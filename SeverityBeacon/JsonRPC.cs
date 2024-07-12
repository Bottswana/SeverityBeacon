namespace SeverityBeacon;

public class JsonRpc
{
        public string Jsonrpc => "2.0";
        public string Method { get; set; }
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
        public int Id => Convert.ToInt32(DateTimeOffset.Now.ToUnixTimeSeconds());
}