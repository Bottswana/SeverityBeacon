using System.IO.Ports;

namespace SeverityBeacon
{
    public class TheBeacon
    {
        private CancellationTokenSource? _CancellationToken;
        private readonly int _ClearBeaconAfter;
        private readonly string _DefaultState;
        private readonly SerialPort _Port;
        private int _BlankAfter = 0;
        private bool _Flashing;
        
        public TheBeacon(string ComPort, string _defaultState, int clearBeaconAfter)
        {
            _Port = new SerialPort(ComPort, 9600);
            _ClearBeaconAfter = clearBeaconAfter;
            _DefaultState = _defaultState;
            _Port.Open();
        }

        /// <summary>
        /// Return a list of available serial devices
        /// </summary>
        /// <returns></returns>
        public static List<string> GetBeaconDevices()
        {
            return SerialPort.GetPortNames().ToList();
        }

        /// <summary>
        /// Update the severity
        /// </summary>
        /// <param name="Option">The severity to display</param>
        public void SendBeaconIssue(SeverityOption? Option)
        {
            try
            {
                _Flashing = false;
                _CancellationToken?.Cancel();
                if( !_Port.IsOpen ) _Port.Open();
                if( Option == null && (_BlankAfter != _ClearBeaconAfter) )
                {
                    // State has recently changed to "all clear"
                    Console.WriteLine($"WR 00 {_HexToByte(_DefaultState)}");
                    _Port.WriteLine($"WR 00 {_HexToByte(_DefaultState)}");
                    _BlankAfter++;
                    return;
                }
                else if( Option == null && (_BlankAfter == _ClearBeaconAfter) )
                {
                    // State has been all clear exceeding the intervals, clear the beacon
                    Console.WriteLine("WR 00 01 01 01");
                    _Port.WriteLine("WR 00 01 01 01");
                    return;
                }
                
                // Set state 1
                _BlankAfter = 0;
                Console.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState1)}");
                _Port.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState1)}");
                
                // Check if we have a second state for flashing
                if( !string.IsNullOrEmpty(Option.BeaconHexColourState2) && Option.BeaconChangeStateInterval != null )
                {
                    Task.Run(async () => await _RunBackgroundThread(Option));
                }
            }
            catch( Exception Ex )
            {
                Console.WriteLine($"Exception on BackgroundTask: {Ex.Message}");
                _Port.Close();
            }
        }
        
        /// <summary>
        /// Run the flashing task
        /// </summary>
        private async Task _RunBackgroundThread(SeverityOption Option)
        {
            try
            {
                _Flashing = true;
                if( !_Port.IsOpen ) _Port.Open();
                _CancellationToken = new CancellationTokenSource();
                while( _Flashing && Option.BeaconChangeStateInterval != null )
                {
                    Console.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState1)}");
                    _Port.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState1)}");
                    await Task.Delay(TimeSpan.FromMilliseconds((int)Option.BeaconChangeStateInterval), _CancellationToken.Token);
                    
                    Console.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState2!)}");
                    _Port.WriteLine($"WR 00 {_HexToByte(Option.BeaconHexColourState2!)}");
                    await Task.Delay(TimeSpan.FromMilliseconds((int)Option.BeaconChangeStateInterval), _CancellationToken.Token);
                }
                
                _CancellationToken = null;
            }
            catch( TaskCanceledException ) {}
            catch( Exception Ex )
            {
                Console.WriteLine($"Exception on BackgroundTask: {Ex.Message}");
                _Port.Close();
            }
        }
        
        /// <summary>
        /// LED requires spacing between hex bytes, so convert #000000 to 00 00 00
        /// </summary>
        /// <param name="HexCode">Hex Code in #xxxxxx format</param>
        /// <returns>RGB Hex bytes seperated by spaces</returns>
        /// <exception cref="Exception">Invalid Hex Code</exception>
        private string _HexToByte(string HexCode)
        {
            if( HexCode.Length != 7 ) throw new Exception("Invalid hex code!");
            return $"{HexCode[1]}{HexCode[2]} {HexCode[3]}{HexCode[4]} {HexCode[5]}{HexCode[6]}";
        }
    }
}