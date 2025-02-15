using System.Collections.Generic;
using Firebase.Database;
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
            return 1;
        }
        else if (myAction == BubbleBattleAction.Merge && opponentAction == BubbleBattleAction.Pop)
        {
            return -1;
        }
        else if (myAction == BubbleBattleAction.Merge && opponentAction == BubbleBattleAction.Float)
        {
            return 0;
        }
        else if (myAction == BubbleBattleAction.Pop && opponentAction == BubbleBattleAction.Merge)
        {
            return 4;
        }
        else if (myAction == BubbleBattleAction.Pop && opponentAction == BubbleBattleAction.Pop)
        {
            return -2;
        }
        else if (myAction == BubbleBattleAction.Pop && opponentAction == BubbleBattleAction.Float)
        {
            return -2;
        }
        else if (myAction == BubbleBattleAction.Float && opponentAction == BubbleBattleAction.Merge)
        {
            return -1;
        }
        else if (myAction == BubbleBattleAction.Float && opponentAction == BubbleBattleAction.Pop)
        {
            return 2;
        }
        else if (myAction == BubbleBattleAction.Float && opponentAction == BubbleBattleAction.Float)
        {
            return -1;
        } 
        else if (myAction == BubbleBattleAction.NoAction) 
        {
            return -2;
        } 
        else if (opponentAction == BubbleBattleAction.NoAction)
        {
            return +2;
        }

        throw new System.NotImplementedException("Score calculation not implemented for action: " + myAction + " and opponent action: " + opponentAction + "");
    }
}