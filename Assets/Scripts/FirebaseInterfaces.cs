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
    public BabbleBattleAction Action { get; set; }
}

public enum BabbleBattleAction
{
    Error,
    NoOpponent,
    None,
    Merge,
    Pop,
    Float
};