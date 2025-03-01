public enum GameState
{
    Idle,
    JoiningRoom,
    WaitingForPlayersGameReady,
    WaitingForRoundStart,
    WaitingForPlayerActions,
    ResolvingActions,
    Over
}

public class Player
{
    public string Id;
    public string Name;
    public int JoinTime;
    public bool IsOwner;

    public Player(string id, string name, int joinTime, bool isOwner)
    {
        Id = id;
        Name = name;
        JoinTime = joinTime;
        IsOwner = isOwner;
    }

    public override string ToString()
    {
        return $"Player(Id: {Id}, Name: {Name}, JoinTime: {JoinTime}, IsOwner: {IsOwner})";
    }
}