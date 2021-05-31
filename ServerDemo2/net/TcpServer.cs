using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Messages.Battle;
using Messages.Login;
using Messages.Match;
using Messages.Type;

class TcpServer : Singleton<TcpServer>
{
    private Socket serverSocket;
    private Dictionary<string, Socket> clientSockets;

    private bool isRunning = false;

    protected override void Initialize()
    {
        clientSockets = new Dictionary<string, Socket>();
    }

    public void Start()
    {
        try
        {
            // 1.建立socket（地址类型，socket类型，协议类型）
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 2.绑定地址和端口
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(NetConfig.SERVER_IP), NetConfig.SERVER_PORT));
            // 3.设置最大监听数
            serverSocket.Listen(NetConfig.MAX_CLIENT);
            isRunning = true;
            Console.WriteLine("服务器启动...");
            new Thread(ListenClientConnect).Start(); // 另开线程监听客户端连接，通过isRunning来关闭线程
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void Close()
    {
        if (!isRunning)
        {
            return;
        }
        isRunning = false;
        try
        {
            foreach (var clientSocket in clientSockets)
            {
                clientSocket.Value.Close();
            }
            clientSockets.Clear();
            serverSocket.Close();
            serverSocket = null;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    // 服务器监听客户端连接
    private void ListenClientConnect()
    {
        while (isRunning)
        {
            try
            {
                Socket clientSocket = serverSocket.Accept();
                string clientIpWithPort = clientSocket.RemoteEndPoint.ToString();
                Console.WriteLine("收到来自" + clientIpWithPort + "的连接");
                clientSockets[clientIpWithPort] = clientSocket;
                new Thread(ReceiveMessage).Start(clientSocket); // 另开线程接收消息
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    // 客户端接收消息
    private void ReceiveMessage(object obj)
    {
        Socket clientSocket = (Socket)obj;
        byte[] data = new byte[1024];
        Console.WriteLine("开始循环接收数据:");
        while (isRunning)
        {
            try
            {
                // 接收客户度发送的数据，并得到数据大小
                int size = clientSocket.Receive(data);
                Console.WriteLine("收到了长度为" + size + "的数据");
                RequestType requestType = (RequestType)data[PackageConstant.TypeOffset];
                Console.WriteLine("数据类型为" + requestType.ToString());
                Int16 dataLength = BitConverter.ToInt16(data, PackageConstant.LengthOffset);
                int bodyLength = dataLength - PackageConstant.HeadLength;
                byte[] body = new byte[bodyLength];
                Array.Copy(data, PackageConstant.HeadLength, body, 0, bodyLength);

                AnalyzeMessage(requestType, body, clientSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                break;
            }
        }
    }

    private void AnalyzeMessage(RequestType requestType, byte[] body, Socket clientSocket)
    {
        switch (requestType)
        {
            case RequestType.LoginRequest:
                {
                    LoginRequest request = ProtobufHelper.Deserialize<LoginRequest>(body);
                    string clientIpWithPort = clientSocket.RemoteEndPoint.ToString();
                    int userId = UserManager.Instance.Login(clientIpWithPort, request.Token, request.Username, request.Password);
                    LoginResponse response = new LoginResponse();
                    response.UserId = userId;
                    response.Result = true;
                    clientSocket.Send(ProtobufHelper.PackMessage<LoginResponse>(response, ResponseType.LoginResponse));
                }
                break;
            case RequestType.MatchRequest:
                {
                    MatchRequest request = ProtobufHelper.Deserialize<MatchRequest>(body);
                    string clientIpWithPort = clientSocket.RemoteEndPoint.ToString();
                    MatchManager.Instance.JoinMatchQueue(clientIpWithPort, request.UserId, request.RoleId);
                    MatchResponse response = new MatchResponse();
                    clientSocket.Send(ProtobufHelper.PackMessage<MatchResponse>(response, ResponseType.MatchResponse));
                }
                break;
            case RequestType.CancelMatchRequest:
                {
                    CancelMatchRequest request = ProtobufHelper.Deserialize<CancelMatchRequest>(body);
                    MatchManager.Instance.CancleMatch(request.UserId);

                    CancelMatchResponse response = new CancelMatchResponse();
                    clientSocket.Send(ProtobufHelper.PackMessage<CancelMatchResponse>(response, ResponseType.CancelMatchResponse));
                }
                break;
            case RequestType.BattleReadyRequest:
                {
                    BattleReadyRequest request = ProtobufHelper.Deserialize<BattleReadyRequest>(body);
                    BattleManager.Instance.GetBattle(request.BattleId).SetUserReady(request.BattleUserId);
                }
                break;
            case RequestType.OperationRequest:
                {
                    OperationRequest request = ProtobufHelper.Deserialize<OperationRequest>(body);
                    BattleManager.Instance.GetBattle(request.BattleId).UpdateCurOperations(request.Operation);
                }
                break;
            case RequestType.DeltaFramesRequest:
                {
                    DeltaFramesRequest request = ProtobufHelper.Deserialize<DeltaFramesRequest>(body);
                    DeltaFramesResponse response = new DeltaFramesResponse();

                    for (int i = 0; i < request.Frames.Count<int>(); i++)
                    {
                        int frameId = request.Frames[i]; // 缺少第几帧
                        OperationsResponse operations = new OperationsResponse();
                        operations.FrameId = frameId;
                        operations.Operations.AddRange(BattleManager.Instance.GetBattle(request.BattleId).allOperations[frameId]);
                        response.DeltaFrames.Add(operations);
                    }
                    clientSocket.Send(ProtobufHelper.PackMessage<DeltaFramesResponse>(response, ResponseType.DeltaFramesResponse));
                }
                break;

        }
    }

    public Socket GetClientSocket(string clientIpWithPort)
    {
        return clientSockets[clientIpWithPort];
    }
}
