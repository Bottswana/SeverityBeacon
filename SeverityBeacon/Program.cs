using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Cocona;
using Cocona.Builder;
using SeverityBeacon;
using SeverityBeacon.Models;

CoconaAppBuilder builder = CoconaApp.CreateBuilder();
CoconaApp app = builder.Build();
HttpClient httpClient = new HttpClient();

Dictionary<string, SeverityOption> zabbixSeverityOptions = new()
{
    { "disaster", new("#FF0101", 5, "#0101FF", 125) },
    { "high", new ("#FF0101",4, "#010101", 125) },
    { "average", new ("#FFA501",3) },
    { "warning", new ("#FFFF01",2) }
};

var zeroProblemsBeconHex = "#01FF01";
var queryIntervalSeconds = 15;
string filteredHostGroupId = string.Empty;
string? beaconSerialPort2 = null;
var clearBeaconAfter2 = 9;

app.AddCommand(async (
    [Argument(Description = "URL of Zabbix Server")]Uri zabbixUrl,
    [Argument(Description = "Zabbix user API token")]string apiToken,
    [Option(['c'], Description = "Beacon Serial Port")]string? beaconSerialPort,
    [Option(['g'], Description = "Provide a HostGroup_ID to filter severity queries")]string? hostGroup,
    [Option(['s'], Description = """
                                 Severities to filter by with colour or flashing state:
                                 Usage: -s [disaster,#FF0000] (static)
                                        -s [disaster,#FF0000,#00FF00,125] (flashing between 2 colours each 125ms)
                                 Severity Values: disaster, high, average, warning, information, "not classified"
                                 default = -s "[disaster,#FF0101,#0101FF,125]", -s "[high,#FF0101,#010101,125]" -s "[average,#FFA501]" -s "[warning,#FFFF01]"
                                 """)]string[]? severity, 
    [Option(['r'], Description = "Query interval in seconds, default = 15")]int? queryInterval,
    [Option(['z'], Description = "Hex colour of zero problems state, default = #018001")]string? zeroProblemsHex,
    [Option(['x'], Description = "After x number of queries with successive OK results, turn off the beacon, default = 9")]int? clearBeaconAfter
    ) =>
{
    if (severity != null) ParseCliSeverityOptions(severity);
    if (queryInterval != null) queryIntervalSeconds = queryInterval.Value;
    if (zeroProblemsHex != null) zeroProblemsBeconHex = zeroProblemsHex;
    if (beaconSerialPort != null) beaconSerialPort2 = beaconSerialPort;
    if (clearBeaconAfter != null ) clearBeaconAfter2 = (int)clearBeaconAfter;
    if (hostGroup == null) await GetHostGroups(zabbixUrl, apiToken);
    else
    {
        filteredHostGroupId = hostGroup;
    }

    await PollProblems(zabbixUrl, apiToken);

});

app.Run();

async Task PollProblems(Uri zabbixUrl, string apiToken)
{
    // Init beacon
    TheBeacon Beacon;
    if( string.IsNullOrEmpty(beaconSerialPort2) )
    {
        Console.Clear();
        Console.WriteLine("Please select beacon:");
        var AvailableBeacons = TheBeacon.GetBeaconDevices();
        for( var i=0; i < AvailableBeacons.Count; i++ )
        {
            Console.WriteLine($"{i}> {AvailableBeacons[i]}");
        }
        
        var requestedId = Console.ReadLine()?.Trim();
        while( true )
        {
            if( !int.TryParse(requestedId, null, out var result) || result < 0 || result > AvailableBeacons.Count )
            {
                Console.WriteLine("ID entered as incorrect, please try again");
                requestedId = Console.ReadLine();
            }
            else
            {
                Console.WriteLine($"Beacon set as device: {AvailableBeacons[result]}");
                Beacon = new TheBeacon(AvailableBeacons[result], zeroProblemsBeconHex, clearBeaconAfter2);
                break;
            }
        }
    }
    else
    {
        Beacon = new TheBeacon(beaconSerialPort2, zeroProblemsBeconHex, clearBeaconAfter2);
    }
    
    Console.WriteLine("please wait, starting Zabbix poll...");
    await Task.Delay(2000);
    Console.Clear();
    Console.WriteLine("Zabbix poll started");
    while (true)
    {
        Beacon.SendBeaconIssue(await GetProblems(zabbixUrl, apiToken));
        await Task.Delay(TimeSpan.FromSeconds(queryIntervalSeconds));
    }
}

async Task GetHostGroups(Uri zabbixUrl, string apiToken)
{
    Console.WriteLine($"No HostGroup ID provided, obtaining the list from {zabbixUrl}");
    
    var rpcRequest = new JsonRpc();
    rpcRequest.Method = "hostgroup.get";
    
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    var response = await httpClient.PostAsJsonAsync(zabbixUrl+"/api_jsonrpc.php", rpcRequest);
    var hostGroups = await response.Content.ReadFromJsonAsync<HostGroupResults>();
    
    if(hostGroups == null) Console.WriteLine("No HostGroups found");

    StringBuilder hostGroupsMessage = new StringBuilder();
    hostGroups?.Result.ForEach(a => hostGroupsMessage.AppendLine($"- {a.Name} - Group ID: {a.Groupid}"));
    hostGroupsMessage.AppendLine("Please enter a host group id from the list");
    
    Console.WriteLine(hostGroupsMessage);
    var requestedId = Console.ReadLine()?.Trim();
    while (!hostGroups.Result.Any(a => a.Groupid == requestedId))
    {
        Console.WriteLine("ID entered as incorrect, please try again");
        requestedId = Console.ReadLine();
    }

    filteredHostGroupId = requestedId;
    Console.Clear();
    Console.WriteLine($"Filtering by HostGroup \"{hostGroups.Result.First(a => a.Groupid == requestedId).Name}\"");
}

void ParseCliSeverityOptions(IEnumerable<string> severitiyOptions)
{
    var options = new Dictionary<string, SeverityOption>();
    
    foreach (var splitOption in severitiyOptions.Select(option => option.Replace("[", "").Replace("]", "").Split(',')))
    {
        switch (splitOption[0].Trim())
        {
            case "disaster":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 5, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 5));
                break;
            case "high":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 4, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 4));
                break;
            case "average":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 3, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 3));
                break;
            case "warning":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 2, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 2));
                break;
            case "information":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 1, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 1));
                break;
            case "not classified":
                if(splitOption.Length == 4) options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 0, splitOption[2].Trim(), int.Parse(splitOption[3])));
                else options.Add(splitOption[0].Trim(), new SeverityOption(splitOption[1].Trim(), 0));
                break;
        }
    }

    zabbixSeverityOptions = options;
}

async Task<SeverityOption?> GetProblems(Uri zabbixUrl, string apiToken)
{
    Console.WriteLine($"{GetCurrentTime()}: Obtaining latest problems from Zabbix");
    
    // Get the list of active/enabled hosts in the host groups
    var getHosts = await GetEnabledHostsInHostGroups(zabbixUrl, apiToken, new List<string>{filteredHostGroupId});
    if( getHosts == null ) return null;
    Console.WriteLine($"{GetCurrentTime()}: {getHosts.Count()} hosts in host group enabled in Zabbix");
    
    var rpcRequest = new JsonRpc();
    rpcRequest.Method = "problem.get";
    rpcRequest.Params.Add("acknowledged", false);
    rpcRequest.Params.Add("suppressed", false);
    rpcRequest.Params.Add("hostids", getHosts);
    rpcRequest.Params.Add("severities", zabbixSeverityOptions.Select(a => a.Value.ZabbixSeverityValue).ToArray());
    
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    var response = await httpClient.PostAsJsonAsync(zabbixUrl+"/api_jsonrpc.php", rpcRequest);
    var problems = await response.Content.ReadFromJsonAsync<ProblemResults>();
    
    if( problems == null ) Console.WriteLine("Problems couldn't be obtained");
    Console.WriteLine($"{GetCurrentTime()}: {problems!.Result.Count} problems active in Zabbix");
    
    // Return SeverityOption matching the highest found severity
    var maximumSeverity = problems.Result.Max(q => q.Severity);
    return maximumSeverity == null ? null : zabbixSeverityOptions.First(a => a.Value.ZabbixSeverityValue == int.Parse(maximumSeverity)).Value;
}

async Task<IEnumerable<string>?> GetEnabledHostsInHostGroups(Uri zabbixUrl, string apiToken, IEnumerable<string> hostGroupIDs)
{
    Console.WriteLine($"{GetCurrentTime()}: Obtaining host state from Zabbix");
    
    var rpcRequest = new JsonRpc();
    rpcRequest.Method = "host.get";
    rpcRequest.Params.Add("output", new List<string>{"hostid", "status"});
    rpcRequest.Params.Add("filter", new RequestFilter{ status = ["0"] });
    rpcRequest.Params.Add("groupids", hostGroupIDs);
    
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
    var response = await httpClient.PostAsJsonAsync(zabbixUrl+"/api_jsonrpc.php", rpcRequest);
    var hosts = await response.Content.ReadFromJsonAsync<HostResults>();
    if( hosts == null ) return null;
    
    var HostIDs = new List<string>();
    hosts.Result.ForEach(q => HostIDs.Add(q.Hostid));
    return HostIDs;
}

string GetCurrentTime()
{
    return DateTime.Now.ToLongTimeString();
}
