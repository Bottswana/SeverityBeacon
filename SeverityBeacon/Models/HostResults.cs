namespace SeverityBeacon.Models;

public class HostResults
{
    public List<HostResult> Result { get; set; } = [];
}

public class HostResult
{
    public string Hostid { get; set; }
    public string Status { get; set; }
}

