namespace LSL.Models;

public class PlayerInfo
{
    public PlayerInfo(string uuid, string playerName)
    {
        UUID = uuid;
        PlayerName = playerName;
    }

    public string UUID { get; set; }
    public string PlayerName { get; set; }
}