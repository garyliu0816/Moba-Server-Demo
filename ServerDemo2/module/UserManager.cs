using System.Collections.Generic;

struct UserInfo
{
    public string clientIpWithPort;
    public bool isLogin;

    public UserInfo(string _clientIpWithPort, bool _isLogin)
    {
        clientIpWithPort = _clientIpWithPort;
        isLogin = _isLogin;
    }
}

class UserManager : Singleton<UserManager>
{
    private int lastUserId;
    private Dictionary<string, int> userIds;        // (clientIpWithPort -> userid) 原本应该使用token，单机下暂时用clientIpWithPort
    private Dictionary<int, UserInfo> userInfos;  // (userid -> clientIpWithPort && isLogin)

    protected override void Initialize()
    {
        lastUserId = 0;
        userIds = new Dictionary<string, int>();
        userInfos = new Dictionary<int, UserInfo>();
    }

    public int Login(string clientIpWithPort, string token, string username, string password)
    {
        int userId;
        // 暂时用clientIpWithPort，有多台主机时换为token
        if (userIds.ContainsKey(clientIpWithPort))
        {
            userId = userIds[clientIpWithPort];
        }
        else
        {
            userId = ++lastUserId;
            userIds[clientIpWithPort] = userId;
        }

        UserInfo userInfo = new UserInfo(clientIpWithPort, true);
        userInfos[userId] = userInfo;

        return userId;
    }

    public void Logout(string clientIpWithPort)
    {
        if (userIds.ContainsKey(clientIpWithPort))
        {
            int userId = userIds[clientIpWithPort];
            userInfos[userId] = new UserInfo(clientIpWithPort, false);
        }
    }

    public void Logout(int userId)
    {
        if (userInfos.ContainsKey(userId))
        {
            string clientIpWithPort = userInfos[userId].clientIpWithPort;
            userInfos[userId] = new UserInfo(clientIpWithPort, false);
        }
    }

    public UserInfo GetUserInfo(int userId)
    {
        return userInfos[userId];
    }
}