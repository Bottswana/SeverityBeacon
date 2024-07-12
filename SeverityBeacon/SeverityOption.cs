namespace SeverityBeacon;

public record SeverityOption
{
    public SeverityOption(string beaconHexColour, int zabbixSeverityValue, string? beaconHexColour2 = null, int? beaconChangeStateInterval = null)
    {
        BeaconHexColourState1 = beaconHexColour;
        BeaconHexColourState2 = beaconHexColour2;
        BeaconChangeStateInterval = beaconChangeStateInterval;
        ZabbixSeverityValue = zabbixSeverityValue;
    }

    /// <summary>
    /// Colour state 1
    /// </summary>
    public string BeaconHexColourState1 { get; set; }
    
    /// <summary>
    /// Colour State 2. If set, beacon will change between both states as defined by the interval
    /// </summary>
    public string? BeaconHexColourState2 { get; set; }
    
    /// <summary>
    /// How often to change between state 1 and state 2 in milliseconds
    /// </summary>
    public int? BeaconChangeStateInterval { get; set; }
    
    /// <summary>
    /// The severity to match from Zabbix
    /// </summary>
    public int ZabbixSeverityValue { get; set; }
}