using System.Text;
using System.Threading.Channels;
using System.Xml.Serialization;

namespace Client
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var config = LoadOrCreateConfig("ConfigClient.xml");
            var calculator = new StatsCalculator();
            var channel = Channel.CreateUnbounded<double>(new UnboundedChannelOptions { SingleReader = true });
            using var cts = new CancellationTokenSource();

            var receiver = new UdpReceiver(config, channel);
            var receiveTask = receiver.ReceiveAsync(cts.Token);

            var processTask = Task.Run(async () =>
            {
                await foreach (var value in channel.Reader.ReadAllAsync(cts.Token))
                    calculator.Add(value);
            }, cts.Token);

            var printTask = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Enter)
                    {
                        var (count, avg, stddev, median, mode) = calculator.GetStats();
                        Console.WriteLine($"\nВсего: {count:N0}, Среднее: {avg:N3}, Ст.откл.: {stddev:N3}, Медиана: {median:N3}, Мода: {mode:N3}, Потери/сек: {receiver.LostPacketsPerSecond:N0}");
                    }
                }
            }, cts.Token);

            await Task.WhenAny(receiveTask, processTask, printTask);
        }

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
    }
}
