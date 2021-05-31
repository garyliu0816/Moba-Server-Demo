using System.Collections.Generic;

class BattleManager : Singleton<BattleManager>
{
    private int lastBattleId;
    private Dictionary<int, BattleController> battles;

    protected override void Initialize()
    {
        lastBattleId = 0;
        battles = new Dictionary<int, BattleController>();
    }

    public override void Destroy()
    {
        foreach (var item in battles)
        {
            item.Value.Destroy();
        }
        base.Destroy();
    }

    public void CreateBattle(List<MatchUserInfo> users)
    {
        BattleController battle = new BattleController();
        battle.Create(++lastBattleId, users);
        battles[lastBattleId] = battle;
    }

    public void FinishBattle(int battleId)
    {
        battles[battleId].Destroy();
        battles.Remove(battleId);
    }

    public BattleController GetBattle(int battleId)
    {
        return battles[battleId];
    }
}