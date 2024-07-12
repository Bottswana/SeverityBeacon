namespace SeverityBeacon;

public class HostGroupResults
{
    public List<HostGroupResult> Result { get; set; } = [];
}

public class HostGroupResult
{
    public string Groupid { get; set; }
    public string Name { get; set; }
    public string Flags { get; set; }
    public string Uuid { get; set; }
}

