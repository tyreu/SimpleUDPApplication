namespace UdpServer
{
    public class Configuration
    {
        public string IP { get; set; } = "239.0.0.222";
        public int LocalPort { get; set; } = 2222;
        public double RangeMin { get; set; } = 100;
        public double RangeMax { get; set; } = 200;
    }
}