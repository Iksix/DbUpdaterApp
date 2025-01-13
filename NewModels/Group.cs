namespace DbUpdater;

public class Group {
    public int Id {get; set;}
    public string Name {get; set;}
    public string Flags {get; set;}
    public int Immunity {get; set;}
    public string? Comment {get; set;}
    public Group(int id, string name, string flags, int immunity, string? comment)
    {
        Id = id;
        Name = name;
        Flags = flags;
        Immunity = immunity;
        Comment = comment;
    }
}