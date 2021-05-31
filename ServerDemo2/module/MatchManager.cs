using System;
using System.Collections.Generic;

struct MatchUserInfo
{
    public string clientIpWithPort;
    public int userId;
    public int roleId;

    public MatchUserInfo(string _clientIpWithPort, int _userId, int _roleId)
    {
        clientIpWithPort = _clientIpWithPort;
        userId = _userId;
        roleId = _roleId;
    }
}

class MatchManager : Singleton<MatchManager>
{
    private List<MatchUserInfo> matchQueue;

    protected override void Initialize()
    {
        matchQueue = new List<MatchUserInfo>();
    }

    public void JoinMatchQueue(string clientIpWithPort, int userId, int roleId)
    {
        matchQueue.Add(new MatchUserInfo(clientIpWithPort, userId, roleId));
        // 判断当前队列人数大于房间开启人数
        if (matchQueue.Count >= NetConfig.BATTLE_USER_NUM)
        {
            Console.WriteLine("队列人数:" + matchQueue.Count + "大于房间需要人数:" + NetConfig.BATTLE_USER_NUM + "，开启一场战斗");
            // 选择要加入到战斗的玩家
            List<MatchUserInfo> users = new List<MatchUserInfo>();
            for (int i = 0; i < NetConfig.BATTLE_USER_NUM; i++)
            {
                users.Add(matchQueue[0]);
                matchQueue.RemoveAt(0);
            }
            BattleManager.Instance.CreateBattle(users);
        }
    }

    public void CancleMatch(int userId)
    {
        for (int i = 0; i < matchQueue.Count; i++)
        {
            if (matchQueue[i].userId == userId)
            {
                matchQueue.RemoveAt(i);
                break;
            }
        }
    }

    public void MatchSure(int matchId, int userId) { }

    public void CancleMatchSure(int matchId, int userId) { }
}