using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

namespace UdpClientApp
{
    class Server
    {
        private double bytesSum = 0;

        private UdpClient udpclient;
        private IPEndPoint endPoint;
        private IPAddress multicastaddress;
        private XmlSerializer formatter = new XmlSerializer(typeof(Configuration));
        private Random random = new Random();

        public Configuration Configuration { get; private set; }
        public Server()
        {
            using (FileStream fs = new FileStream("Config.xml", FileMode.OpenOrCreate))
            {
                Configuration = (Configuration)formatter.Deserialize(fs);
                udpclient = new UdpClient();
                multicastaddress = IPAddress.Parse(Configuration.IP);
                udpclient.JoinMulticastGroup(multicastaddress);
                endPoint = new IPEndPoint(multicastaddress, Configuration.LocalPort);
                Console.WriteLine("Настройки загружены!");
            }
        }
        public double Generate()
        {
            return random.NextDouble() * (Configuration.RangeMax - Configuration.RangeMin) + Configuration.RangeMin;
        }
        public void SendMessage(string info)
        {
            try
            {
                byte[] data = Encoding.Unicode.GetBytes(info);// сообщение для отправки
                int returnBytes = udpclient.Send(data, data.Length, endPoint); // отправка
                Console.Write($"\rОтправлено {bytesSum+=returnBytes} байт");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Server server = new Server();
                while (true)
                {
                    var info = server.Generate();
                    server.SendMessage($"{info}");//отправляем сообщение
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}