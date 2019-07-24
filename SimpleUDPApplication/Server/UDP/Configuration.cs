namespace UdpClientApp
{
    public class Configuration
    {
        public Configuration() { }
        public double RangeMin { get; set; }
        public double RangeMax { get; set; }
        public string IP { get; set; } //235.5.5.11
        public int RemotePort { get; set; } // порт для отправки данных 8001
        public int LocalPort { get; set; } // локальный порт для прослушивания входящих подключений 8001
    }
}