using UnityEngine;

public class BubblePlayer
{
    public string FirebaseId { get; set; }
    public string Name { get; set; }
    public long JoinTime { get; set; }
}

public class BubblePlayerReadyState
{
    public string FirebaseId { get; set; }
    public bool Ready { get; set; }
}

public class BubbleBattlePair
{
    public string FirebaseIdPlayer { get; set; }
    public string FirebaseIdOpponent { get; set; }
    public BubbleBattleAction Action { get; set; }
}

public enum BubbleBattleAction
{
    Error,
    NoOpponent,
    NoAction,
    Merge,
    Pop,
    Float
};

public static class BabbleBattleScoreCalculator
{
    public static int CalculateScore(BubbleBattleAction myAction, BubbleBattleAction opponentAction)
    {
        if (myAction == BubbleBattleAction.Merge && opponentAction == BubbleBattleAction.Merge)
        {
            return 2;
        }
        
        throw new System.NotImplementedException("Score calculation not implemented for action: " + myAction + " and opponent action: " + opponentAction + "");
    }
}