using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace Client
{
    public class UdpReceiver
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _localEp;
        private readonly IPAddress _multicastAddress;
        private readonly int _delayMs;
        private readonly Channel<double> _channel;

        public UdpReceiver(Configuration config, Channel<double> channel)
        {
            _delayMs = config.Delay;
            _channel = channel;
            _localEp = new IPEndPoint(IPAddress.Any, config.LocalPort);
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Client.Bind(_localEp);
            _multicastAddress = IPAddress.Parse(config.IP);
            _udpClient.JoinMulticastGroup(_multicastAddress);
        }

        public async Task ReceiveAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_delayMs > 0)
                    await Task.Delay(_delayMs, token);

                var result = await _udpClient.ReceiveAsync(token);
                if (result.Buffer.Length == sizeof(double))
                {
                    double value = BitConverter.ToDouble(result.Buffer);
                    await _channel.Writer.WriteAsync(value, token);
                }
            }
        }
    }

}
