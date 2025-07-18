namespace DbUpdater;
public class OldGroup
{
    public int Id { get; set; }
    public string Flags { get; set; }
    public string Name { get; set; }
    public int Immunity { get; set; }

    public OldGroup(string name, string flags, int immunity, int id)
    {
        Flags = flags;
        Name = name;
        Immunity = immunity;
        Id = id;
    }
}