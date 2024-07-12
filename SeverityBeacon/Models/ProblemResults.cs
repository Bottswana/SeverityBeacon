namespace SeverityBeacon.Models;

public class ProblemResults
{
    public List<ProblemResult> Result { get; set; } = [];
}

public class ProblemResult
{
    public string Eventid { get; set; }
    public string Source { get; set; }
    public string Objectid { get; set; }
    public string Clock { get; set; }
    public string Ns { get; set; }
    public string REventid { get; set; }
    public string RClock { get; set; }
    public string RNs { get; set; }
    public string Correlationid { get; set; }
    public string Userid { get; set; }
    public string Name { get; set; }
    public string Acknowledged { get; set; }
    public string Severity { get; set; }
    public string CauseEventid { get; set; }
    public string Opdata { get; set; }
    public string Suppressed { get; set; }
}

