namespace server;

public class Client
{
    public string guid { get; set; }
    public DateTime lastBeat { get; set; }
    

    public Client(string guid, DateTime lastBeat)
    {
        this.guid = guid;
        this.lastBeat = lastBeat;
    }
}