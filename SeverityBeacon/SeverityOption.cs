namespace SeverityBeacon;

public record SeverityOption(
    string BeaconHexColourState1,
    int ZabbixSeverityValue,
    string? BeaconHexColourState2 = null,
    int? BeaconChangeStateInterval1 = null,
    int? BeaconChangeStateInterval2 = null)
{
    /// <summary>
    /// Colour state 1
    /// </summary>
    public string BeaconHexColourState1 { get; set; } = BeaconHexColourState1;

    /// <summary>
    /// Colour State 2. If set, beacon will change between both states as defined by the interval
    /// </summary>
    public string? BeaconHexColourState2 { get; set; } = BeaconHexColourState2;

    /// <summary>
    /// Time to wait to transition between State 1 and State 2
    /// </summary>
    public int? BeaconChangeStateInterval1 { get; set; } = BeaconChangeStateInterval1;

    /// <summary>
    /// Time to wait to transntion between State 2 and State 1
    /// </summary>
    public int? BeaconChangeStateInterval2 { get; set; } = BeaconChangeStateInterval2;

    /// <summary>
    /// The severity to match from Zabbix
    /// </summary>
    public int ZabbixSeverityValue { get; set; } = ZabbixSeverityValue;
}