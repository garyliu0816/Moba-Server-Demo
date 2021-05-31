using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using Messages.Battle;
using Messages.Type;

class BattleController
{
    private int battleId;
    private int curFrameId;
    private bool isRunning;
    private bool isUserSync;
    private Dictionary<int, int> battleUserIds; // 每场战斗重新为玩家分配id;
    private Dictionary<int, bool> readyUsers;
    private Dictionary<int, string> userAddresses;
    private Operation[] curOperations; // 当前帧所有玩家的操作
    private bool[] isPlayerDead; // 玩家是否死亡
    private int[] playerFrameIds;

    public Dictionary<int, Operation[]> allOperations; // 每帧所有玩家的操作

    public BattleController()
    {
        battleId = 0;
        battleUserIds = new Dictionary<int, int>();
        readyUsers = new Dictionary<int, bool>();
        userAddresses = new Dictionary<int, string>();
        allOperations = new Dictionary<int, Operation[]>();
    }

    public void Destroy()
    {
        isRunning = false;
    }

    public void Create(int _battleId, List<MatchUserInfo> users)
    {
        int randomSeed = new Random().Next(0, 100);
        ThreadPool.QueueUserWorkItem((obj) =>
            {
                battleId = _battleId;
                int battleUserId = 0; // 初始化为0

                EnterBattleResponse response = new EnterBattleResponse();
                response.RandomSeed = randomSeed;
                response.BattleId = battleId;
                for (int i = 0; i < users.Count; i++)
                {
                    int userId = users[i].userId;
                    battleUserIds[userId] = ++battleUserId; // 先自增后赋值
                    readyUsers[battleUserId] = false;
                    userAddresses[battleUserId] = users[i].clientIpWithPort;
                    BattleUserInfo battleUserInfo = new BattleUserInfo();
                    battleUserInfo.UserId = userId;
                    battleUserInfo.BattleUserId = battleUserId;
                    battleUserInfo.RoleId = users[i].roleId;

                    response.BattleUserInfos.Add(battleUserInfo);
                }
                for (int i = 0; i < userAddresses.Count; i++) // 遍历参与战斗的玩家，发送开始战斗的消息
                {
                    string clientIpWithPort = userAddresses[i + 1];
                    Socket clientSocket = TcpServer.Instance.GetClientSocket(clientIpWithPort);
                    clientSocket.Send(ProtobufHelper.PackMessage(response, ResponseType.EnterBattleResponse));
                }
            }, null);
    }

    public void SetUserReady(int battleUserId)
    {
        readyUsers[battleUserId] = true;
        if (CheckBattleReady(readyUsers))
        {
            BeginBattle();
        }
    }

    private bool CheckBattleReady(Dictionary<int, bool> readyUsers)
    {
        foreach (var item in readyUsers.Values)
        {
            if (item == false)
            {
                return false;
            }
        }
        return true;
    }

    private void BeginBattle()
    {
        // 初始化数据
        isRunning = true;
        curFrameId = 0;

        int playerNum = battleUserIds.Keys.Count;
        curOperations = new Operation[playerNum];
        playerFrameIds = new int[playerNum];
        isPlayerDead = new bool[playerNum];

        for (int i = 0; i < playerNum; i++)
        {
            curOperations[i] = null;
            playerFrameIds[i] = 0;
            isPlayerDead[i] = false;
        }
        // 开始发送帧数据
        new Thread(SendFrameData).Start();
    }

    private void SendFrameData()
    {
        isUserSync = false;

        // 开始同步用户第一帧
        while (!isUserSync)
        {
            byte[] data = ProtobufHelper.PackMessage<BattleStartResponse>(new BattleStartResponse(), ResponseType.BattleStartResponse);
            foreach (var clientIpWithPort in userAddresses.Values)
            {
                Socket clientSocket = TcpServer.Instance.GetClientSocket(clientIpWithPort);
                clientSocket.Send(data);
            }

            bool receivedAllData = true;
            for (int i = 0; i < curOperations.Length; i++)
            {
                if (curOperations[i] == null)
                {
                    receivedAllData = false;
                    break;
                }
            }

            if (receivedAllData)
            {
                Console.WriteLine("收到全部玩家的第一次数据");
                curFrameId = 1; // 初始化为第一帧
                isUserSync = true; // 玩家同步成功
            }

            Thread.Sleep(500);
        }

        // 开始帧同步
        while (isRunning)
        {
            allOperations[curFrameId] = curOperations; // 服务器本地记录每帧操作

            OperationsResponse response = new OperationsResponse(); // 下发给客户端的操作指令
            response.Operations.AddRange(curOperations);
            response.FrameId = curFrameId;

            curFrameId++;

            byte[] data = ProtobufHelper.PackMessage<OperationsResponse>(response, ResponseType.OperationsResponse);
            // 遍历所有用户
            foreach (var clientIpWithPort in userAddresses.Values)
            {
                Socket clientSocket = TcpServer.Instance.GetClientSocket(clientIpWithPort);
                clientSocket.Send(data);
            }
            // 帧同步间隔
            Thread.Sleep(NetConfig.FRAME_TIME);
        }
    }

    public void UpdateCurOperations(Operation operation)
    {
        int battleUserId = operation.BattleUserId - 1; // battleUserId是从1开始的
        Console.WriteLine("收到来自用户:" + battleUserId + "的操作");
        int frameId = operation.FrameId;
        Console.WriteLine("操作所在的帧数为" + frameId);
        if (frameId > playerFrameIds[battleUserId])
        {
            curOperations[battleUserId] = operation;
            playerFrameIds[battleUserId] = frameId;
        }
    }
}