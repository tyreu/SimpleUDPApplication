using System.Text;

namespace UdpServer
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            using var server = new Server("Config.xml");
            await server.StartAsync(cts.Token);
        }
    }
}