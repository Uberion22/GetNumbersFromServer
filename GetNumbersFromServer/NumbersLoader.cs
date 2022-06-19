using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class NumbersLoader
    {
        private int currentStep;
        private int maxDelay = 25000;
        private int maxNumber = 2018;
        private int port;
        private IPAddress ip;
        public static int currentTaskComplited;
        public int nextIndex = 30;
        public int maxTasks = 30;
        public Dictionary<int, long> recivedNumbers = new Dictionary<int, long>();
        private object locker = new object();

        public NumbersLoader(string ipString, int port)
        {
            this.port = port;
            ip = IPAddress.Parse(ipString);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public async Task<float> StartTasksAsync()
        {
            nextIndex = maxTasks;
            recivedNumbers = FileSaverAndLoader.LoadNumbersFromFile();

            List<Task<ServerResponce>> tasks = new List<Task<ServerResponce>>();
            var notReceived = recivedNumbers.Where(r => r.Value <= -1).Select(k => k.Key).ToList();
            Console.WriteLine($"Проход по массиву {++currentStep}. Не получено {notReceived.Count} чисел");
            for (int i = 0; i < maxTasks && i < notReceived.Count; i++)
            {
                var index = notReceived[i];
                tasks.Add(Task.Run(() => GetNumbersFromStream(ip, port, index)));
            }
            
            while (nextIndex <= maxNumber || tasks.Any()) 
            {
                if(!tasks.Any()) break;

                var finishedIndex = await Task.WhenAny(tasks);
                var k = recivedNumbers[finishedIndex.Result.Number] = finishedIndex.Result.Result;
                tasks.Remove(finishedIndex);

                lock (locker)
                {
                    FileSaverAndLoader.SaveNumbersToFile(recivedNumbers);
                    if (tasks.Count < maxTasks && nextIndex < notReceived.Count)
                    {
                        var currentIndex = nextIndex;
                        tasks.Add(Task.Run(() => GetNumbersFromStream(ip, port, notReceived[currentIndex])));
                        nextIndex++;
                    }
                }

            }
            Console.WriteLine("Ждем 5 сек и запускаем новый проход");
            Thread.Sleep(5);

            return recivedNumbers.Any(r => r.Value < 0) ? await Task.Run(StartTasksAsync) : GetResult();
        }

        private float GetResult()
        {
            List<long> list = new List<long>();
            foreach (var value in recivedNumbers.Values) list.Add(value);
            list.Sort();
            var result = (list[1007] + list[1008]) / 2;
            Console.WriteLine($"Все задачи завершены, ответ {result}");
            return result;
        }

        public ServerResponce GetNumbersFromStream(IPAddress ip, int usedPort, int index = 1)
        {
            TcpClient client = new TcpClient();
            NetworkStream stream;
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                var firstFound = false;
                var mes = $"{index}\n";
                var numberInByte = Encoding.UTF8.GetBytes(mes);
                bool endOfTime;
                var badSymbolsCount = 0;
                bool last;
                client.Connect(ip, usedPort);
                byte[] data = new byte[4];
                StringBuilder response = new StringBuilder();
                stream = client.GetStream();
                stream.Write(numberInByte);
                stopWatch.Restart();
                do
                {
                    stream.Flush();
                    int bytes = stream.Read(data, 0, data.Length);
                    var str = Encoding.GetEncoding("koi8r").GetString(data, 0, bytes);

                    if (str.Any(char.IsDigit))
                    {
                        firstFound = true;
                        response.Append(str);
                    }
                    else if (firstFound && badSymbolsCount < 3)
                    {
                        badSymbolsCount++;
                    }
                    endOfTime = stopWatch.ElapsedMilliseconds > maxDelay;
                    
                    last = str.Contains("\n") || badSymbolsCount >= 3;
                }
                while (!last && stream.Socket.Connected && !endOfTime) ; // пока данные есть в потоке

                var resString = !endOfTime
                    ? $"Завершено {currentTaskComplited++}, номер: {index}, результат:{response}"
                    : $"Cтопвоч {currentTaskComplited++}, номер: {index}, результат:{response}, стопвоч: {stopWatch.ElapsedMilliseconds}";
                Console.WriteLine(resString);
                Console.ForegroundColor = ConsoleColor.Blue;
                if (!endOfTime)
                {
                    Console.WriteLine(response);
                }
                Console.ResetColor();

                // Закрываем потоки
                stream.Close();
                client.Close();
                var result = endOfTime ? Int64.Parse(resString) : -1;
                return new ServerResponce(index, result);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

            return new ServerResponce(index, -1);
        }

        public  void GetMessage(string mes)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                StringBuilder response = new StringBuilder();
                NetworkStream stream = client.GetStream();
                byte[] data = new byte[8];
                var data2 = Encoding.UTF8.GetBytes(mes);
                var end = false;
                stream.Write(data2);
                do
                {
                    stream.Flush();
                    int bytes = stream.Read(data, 0, data.Length);
                    response.Append(Encoding.GetEncoding("koi8r").GetString(data, 0, bytes));
                    end = response.ToString().Contains("\n");
                } while (stream.DataAvailable || ! end); // пока данные есть в потоке
                Console.WriteLine(response.ToString());
                stream.Close();
                client.Close();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

            Console.ReadLine();
        }
    }
}
