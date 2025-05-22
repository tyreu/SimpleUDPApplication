namespace Client
{
    public class Configuration
    {
        public string IP { get; set; } = "239.0.0.222";
        public int RemotePort { get; set; } = 2222;
        public int LocalPort { get; set; } = 2222;
        public int DelayMilliseconds { get; set; } = 500;
    }
}
