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
        private readonly Channel<double> _channel;
        private readonly int _delayMilliseconds;
        private long _expectedCounter = -1;

        private readonly Queue<(long timestamp, long count)> _lossWindow = new();
        private long _lossLastSecond = 0;

        public long LostPacketsPerSecond => Interlocked.Read(ref _lossLastSecond);

        public UdpReceiver(Configuration config, Channel<double> channel)
        {
            _delayMilliseconds = config.DelayMilliseconds;
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
            long lastDelayTick = Environment.TickCount64;
            while (!token.IsCancellationRequested)
            {
                if (_delayMilliseconds > 0 && Environment.TickCount64 - lastDelayTick >= 1000)
                {
                    await Task.Delay(_delayMilliseconds, token);
                    lastDelayTick = Environment.TickCount64;
                }

                UdpReceiveResult result;
                try
                {
                    result = await _udpClient.ReceiveAsync(token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine("[Ошибка приёма]: " + ex.Message);
                    continue;
                }

                if (result.Buffer.Length != 16) continue; // 8 байт double + 8 байт long counter
                double value = BitConverter.ToDouble(result.Buffer, 0);
                long packetIndex = BitConverter.ToInt64(result.Buffer, 8);

                if (_expectedCounter == -1)
                    _expectedCounter = packetIndex;

                long lost = Math.Max(0, packetIndex - _expectedCounter);
                if (lost > 0)
                {
                    long now = Environment.TickCount64;
                    _lossWindow.Enqueue((now, lost));
                }

                _expectedCounter = packetIndex + 1;
                await _channel.Writer.WriteAsync(value, token);

                long cutoff = Environment.TickCount64 - 1000;
                while (_lossWindow.Count > 0 && _lossWindow.Peek().timestamp < cutoff)
                    _lossWindow.Dequeue();

                Interlocked.Exchange(ref _lossLastSecond, _lossWindow.Sum(x => x.count));
            }
        }
    }
}
