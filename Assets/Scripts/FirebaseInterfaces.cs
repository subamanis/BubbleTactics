using UnityEngine;

public class BubblePlayer
{
    public string FirebaseId {get; set;}
    public string Name {get; set;}
    public long JoinTime {get; set;}
}

public class BubblePlayerReadyState
{
    public string FirebaseId {get; set;}
    public bool Ready {get; set;}
}