using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace Client
{
    public class Configuration
    {
        public Configuration() { }
        public string IP { get; set; } //239.0.0.222
        public int RemotePort { get; set; } // порт для отправки данных 2222
        public int LocalPort { get; set; } // локальный порт для прослушивания входящих подключений 2222
        public int Delay { get; set; }
    }
    class Client
    {
        private double oldAverage = 0, newAverage = 0, oldSumOfSquaresOfDifferences = 0, newSumOfSquaresOfDifferences = 0;
        private UdpClient client;
        private IPEndPoint localEp = null;
        private IPAddress multicastaddress;
        private List<double> data = new List<double>();
        private ConcurrentDictionary<double, int> dictionaryForMode = new ConcurrentDictionary<double, int>();
        /// <summary>
        /// Экземпляр настроек подключения
        /// </summary>
        public Configuration Configuration { get; set; }
        /// <summary>
        /// Кол-во элементов
        /// </summary>
        public int Count { get; private set; } = 0;
        /// <summary>
        /// Среднее значение
        /// </summary>
        public double Average => Count > 0 ? newAverage : 0;
        /// <summary>
        /// Стандартное отклонение
        /// </summary>
        public double StandardDeviation => Math.Sqrt((Count > 1) ? newSumOfSquaresOfDifferences / (Count - 1) : 0.0);
        /// <summary>
        /// Медиана
        /// </summary>
        public double Median
        {
            get
            {
                QuickSort(data, 0, data.Count - 1);
                if (Count % 2 == 1) return data[Count / 2];
                else return (data[Count / 2] + data[Count / 2 - 1]) / 2;
                //return data[Count > 1 ? (Count > 3 ? Count / 2 : 1) : 0];
            }
        }
        /// <summary>
        /// Мода
        /// </summary>
        public double Mode
        {
            get
            {
                var max = dictionaryForMode.Values.Max();
                return dictionaryForMode.FirstOrDefault(x => x.Value == max).Key;
            }
        }
        /// <summary>
        /// Метод для поиска индекса опорного элемента
        /// </summary>
        /// <param name="m">Массив элементов</param>
        /// <param name="a">Начало подмножества</param>
        /// <param name="b">Конец подмножества</param>
        private int Partition(List<double> m, int a, int b)
        {
            int i = a;
            for (int j = a; j <= b; j++)         // просматриваем с a по b
            {
                if (m[j].CompareTo(m[b]) <= 0)  // если элемент m[j] не превосходит m[b],
                {
                    double t = m[i];                  // меняем местами m[j] и m[a], m[a+1], m[a+2] и так далее...
                    m[i] = m[j];                 // то есть переносим элементы меньшие m[b] в начало,
                    m[j] = t;                    // а затем и сам m[b] «сверху»
                    i++;                         // таким образом последний обмен: m[b] и m[i], после чего i++
                }
            }
            return i - 1;                        // в индексе i хранится <новая позиция элемента m[b]> + 1
        }
        /// <summary>
        /// Быстрая сортировка массива
        /// </summary>
        /// <param name="m">Массив элементов</param>
        /// <param name="a">Начало подмножества</param>
        /// <param name="b">Конец подмножества</param>
        public void QuickSort(List<double> m, int a, int b) //a - начало подмножества, b - конец
        {                                        // для первого вызова: a = 0, b = <элементов в массиве> - 1
            if (a >= b) return;
            int c = Partition(m, a, b);
            QuickSort(m, a, c - 1);
            QuickSort(m, c + 1, b);
        }
        public Client()
        {
            using (FileStream fs = new FileStream("ConfigClient.xml", FileMode.OpenOrCreate))
            {
                Configuration = (Configuration)new XmlSerializer(typeof(Configuration)).Deserialize(fs);

                client = new UdpClient();
                client.ExclusiveAddressUse = false;
                localEp = new IPEndPoint(IPAddress.Any, Configuration.LocalPort);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.ExclusiveAddressUse = false;
                client.Client.Bind(localEp);
                multicastaddress = IPAddress.Parse(Configuration.IP);
                client.JoinMulticastGroup(multicastaddress);
                Console.WriteLine("Настройки загружены!");
            }
        }
        public void GetData()
        {
            while (Console.ReadKey(true).Key == ConsoleKey.Enter && Count > 0)
                Console.WriteLine($"\rВсего: {Count}, Среднее: {Average:N3}, Стандартное отклонение: {StandardDeviation:N3}, Медиана: {Median:N3}, Мода: {Mode:N2}");
        }
        public void ReceiveMessage()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(Configuration.Delay);
                    byte[] buffer = client.Receive(ref localEp);
                    double value = double.Parse(Encoding.Unicode.GetString(buffer));
                    data.Add(value);
                    if (dictionaryForMode.ContainsKey(value))
                        dictionaryForMode[value]++;
                    else
                        dictionaryForMode.TryAdd(value, 1);
                    Count++;
                    //вычисление среднего и отклонения
                    if (Count == 1)
                    {
                        oldAverage = newAverage = value;
                        oldSumOfSquaresOfDifferences = 0;
                    }
                    else
                    {
                        newAverage = oldAverage + (value - oldAverage) / Count;
                        newSumOfSquaresOfDifferences = oldSumOfSquaresOfDifferences + (value - oldAverage) * (value - newAverage);
                        //устанавливаем значения для следующих итераций
                        oldAverage = newAverage;
                        oldSumOfSquaresOfDifferences = newSumOfSquaresOfDifferences;
                    }
                    Console.Write($"\r{Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            new Thread(() => client.ReceiveMessage()) { IsBackground = true }.Start();
            new Thread(() => client.GetData()).Start();
        }
    }
}
