using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class NumbersLoader
    {
        private int currentStep;
        private int maxDelay = 25000;
        private int maxNumber = 2018;
        private readonly int port;
        private readonly IPAddress ip;
        private int currentTaskComplited;
        private int nextIndex = 30;
        private int maxTasks = 30;
        private Dictionary<int, long> recivedNumbers = new Dictionary<int, long>();
        private object locker = new object();
        private  const int notRecieved = -1;

        public NumbersLoader(string ipString, int port)
        {
            this.port = port;
            ip = IPAddress.Parse(ipString);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Run the task of getting 2018 numbers
        /// </summary>
        /// <returns>Result</returns>
        public async Task<float> StartGettingNumbersTasksAsync()
        {
            nextIndex = maxTasks;
            recivedNumbers = FileSaverAndLoader.LoadNumbersFromFile();

            List<Task<ServerResponse>> tasks = new List<Task<ServerResponse>>();
            var notReceived = recivedNumbers.Where(r => r.Value <= notRecieved).Select(k => k.Key).ToList();
            Console.WriteLine($"Traversing an array of keys {++currentStep}. Not received {notReceived.Count}");
            for (int i = 0; i < maxTasks && i < notReceived.Count; i++)
            {
                var index = notReceived[i];
                tasks.Add(Task.Run(() => GetNumberFromStream(index)));
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
                        tasks.Add(Task.Run(() => GetNumberFromStream(notReceived[currentIndex])));
                        nextIndex++;
                    }
                }

            }

            return recivedNumbers.Any(r => r.Value < 0) ? await Task.Run(StartGettingNumbersTasksAsync) : GetResult();
        }

        /// <summary>
        /// Get the median of an array(dictionary values).
        /// </summary>
        /// <returns>median</returns>
        private float GetResult()
        {
            List<long> list = new List<long>();
            foreach (var value in recivedNumbers.Values) list.Add(value);
            // Used easy wai with standart sorting;
            list.Sort();
            var result = (list[1008] + list[1009]) / 2.0f;
            Console.WriteLine($"All tasks completed, result: {result}");
            
            return result;
        }

        /// <summary>
        /// Get a number from a message. If no response is received within the time limit,
        /// the connection is closed without waiting for completion.
        /// </summary>
        /// <param name="index">Requested number</param>
        /// <returns>Requested number, or -1 on failure</returns>
        public ServerResponse GetNumberFromStream(int index = 1)
        {
            try
            {
                TcpClient client = new TcpClient();
                Stopwatch stopWatch = new Stopwatch();
                StringBuilder response = new StringBuilder();
                string message = $"{index}\n";
                byte[] numberInByte = Encoding.UTF8.GetBytes(message);
                byte[] data = new byte[4];
                bool firstFound = false;
                bool timeNotEnded = true;
                bool isLastSymbol;
                int badSymbolsCount = 0;
                
                client.Connect(ip, port);
                var stream = client.GetStream();
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
                    else if (!string.IsNullOrEmpty(str) && firstFound && badSymbolsCount < 3)
                    {
                        badSymbolsCount++;
                    }
                    timeNotEnded = stopWatch.ElapsedMilliseconds < maxDelay;
                    
                    isLastSymbol = str.Contains("\n") || badSymbolsCount >= 3;
                }
                while (!isLastSymbol && timeNotEnded && stream.DataAvailable) ;

                var resString = timeNotEnded
                    ? $"Completed {currentTaskComplited++}, number: {index}, result:{response}"
                    : $"Timed out: {currentTaskComplited++}, number: {index}, result:{response}, time: {stopWatch.ElapsedMilliseconds}";
                Console.WriteLine(resString);
                Console.ForegroundColor = ConsoleColor.Blue;
                if (timeNotEnded)
                {
                    Console.WriteLine(response);
                }
                Console.ResetColor();
                stream.Close();
                client.Close();
                
                var result = timeNotEnded ? Int64.Parse(response.ToString()) : notRecieved;
                
                return new ServerResponse(index, result);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

            return new ServerResponse(index, notRecieved);
        }

        /// <summary>
        /// Send message to server.
        /// </summary>
        /// <param name="message"> Message</param>
        public  void SendMessage(string message)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(ip, port);
                StringBuilder response = new StringBuilder();
                NetworkStream stream = client.GetStream();
                byte[] data = new byte[2];
                byte[] messageInByte = Encoding.UTF8.GetBytes(message);
                stream.Write(messageInByte);
                stream.ReadTimeout = maxDelay;
                bool lastSymbol = false;
                do
                {
                    stream.Flush();
                    int bytes = stream.Read(data, 0, data.Length);
                    
                    response.Append(Encoding.GetEncoding("koi8r").GetString(data, 0, bytes));
                    lastSymbol = response.ToString().Contains("\r\n");
                } while (stream.DataAvailable || !lastSymbol);
                Console.WriteLine(response.ToString());
                stream.Close();
                client.Close();
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }

            Console.ReadLine();
        }
    }
}
