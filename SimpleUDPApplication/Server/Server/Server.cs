using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace UdpServer
{
    public class Server : IDisposable
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly Random _random = new();
        private readonly Configuration _config;
        private long _bytesSent;
        private long _counter;

        public Server(string configPath)
        {
            _config = LoadOrCreateConfig(configPath);
            _udpClient = new UdpClient();
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(_config.IP), _config.LocalPort);
            Console.WriteLine("Настройки загружены.");
        }

        public async Task StartAsync(CancellationToken token)
        {
            var buffer = new byte[16];
            while (!token.IsCancellationRequested)
            {
                double value = GenerateQuote();
                BitConverter.TryWriteBytes(buffer.AsSpan(0, 8), value);
                BitConverter.TryWriteBytes(buffer.AsSpan(8, 8), _counter);

                int sent = await _udpClient.SendAsync(buffer, buffer.Length, _remoteEndPoint);
                Interlocked.Add(ref _bytesSent, sent);
                Console.Write($"\rОтправлено: {_bytesSent} байт");
                _counter++;
            }
        }

        private double GenerateQuote() =>
            _random.NextDouble() * (_config.RangeMax - _config.RangeMin) + _config.RangeMin;

        private static Configuration LoadOrCreateConfig(string path)
        {
            if (!File.Exists(path))
            {
                var defaultConfig = new Configuration();
                using var fs = new FileStream(path, FileMode.Create);
                new XmlSerializer(typeof(Configuration)).Serialize(fs, defaultConfig);
                Console.WriteLine($"Создан конфиг по умолчанию: {path}");
                return defaultConfig;
            }
            using var fsLoad = new FileStream(path, FileMode.Open, FileAccess.Read);
            return (Configuration)new XmlSerializer(typeof(Configuration)).Deserialize(fsLoad);
        }

        public void Dispose() => _udpClient.Dispose();
    }
}