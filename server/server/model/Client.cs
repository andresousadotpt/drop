namespace server;

public class Client
{
    public string uuid { get; set; }
    public DateTime lastBeat { get; set; }
    

    public Client(string uuid, DateTime lastBeat)
    {
        this.uuid = uuid;
        this.lastBeat = lastBeat;
    }
}