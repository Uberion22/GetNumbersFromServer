using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class NumbersLoader
    {
        public Dictionary<int, long> ReceivedNumbers { get; private set; }
        private int currentStep;
        private int maxDelay = 25000;
        private int maxNumber = 2018;
        private readonly int port;
        private readonly IPAddress ip;
        private int currentTaskComplited;
        private int nextIndex = 30;
        private int maxTasks = 30;
        private object locker = new object();
        private object keyReceivingLocker = new object();
        private const int notReceieved = -1;
        private string key = "";
        private const string newKeyMes = "Register\n";
        private const string keyHasExpiredText = "Key has expired";
        private readonly Encoding usedEncoding;
        private int encdoeSymbolsCount = 3;
        private int badSymvolsCount = 4;
 
        public NumbersLoader(string ipString, int port)
        {
            this.port = port;
            ip = IPAddress.Parse(ipString);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            usedEncoding = Encoding.GetEncoding("koi8r");
            ReceivedNumbers = new Dictionary<int, long>();
        }

        /// <summary>
        /// Run the task of getting 2018 numbers
        /// </summary>
        /// <returns>Result</returns>
        public async Task<float> StartGettingNumbersTasksAsync()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(SaveToFile);
            nextIndex = maxTasks;
            if (!ReceivedNumbers.Any())
            {
                ReceivedNumbers = FileSaverAndLoader.LoadNumbersFromFile();
            }
            SendMessage(newKeyMes);
            List<Task<ServerResponse>> tasks = new List<Task<ServerResponse>>();
            var notReceived = ReceivedNumbers.Where(r => r.Value <= notReceieved).Select(k => k.Key).ToList();
            Console.WriteLine($"\nTraversing an array of keys {++currentStep}. Not received {notReceived.Count}");
            for (int i = 0; i < maxTasks && i < notReceived.Count; i++)
            {
                var index = notReceived[i];
                tasks.Add(Task.Run(() => GetNumberFromStream(index)));
            }
            
            while (nextIndex <= maxNumber || tasks.Any()) 
            {
                if(!tasks.Any()) break;

                var finishedIndex = await Task.WhenAny(tasks);
                ReceivedNumbers[finishedIndex.Result.Number] = finishedIndex.Result.Result;
                tasks.Remove(finishedIndex);
                lock (locker)
                {
                    //FileSaverAndLoader.SaveNumbersToFile(receivedNumbers);
                    if (tasks.Count < maxTasks && nextIndex < notReceived.Count)
                    {
                        var currentIndex = nextIndex;
                        tasks.Add(Task.Run(() => GetNumberFromStream(notReceived[currentIndex])));
                        nextIndex++;
                    }
                }
            }

            return ReceivedNumbers.Any(r => r.Value < 0) ? await Task.Run(StartGettingNumbersTasksAsync) : GetResult();
        }

        /// <summary>
        /// Get the median of an array(dictionary values).
        /// </summary>
        /// <returns>median</returns>
        private float GetResult()
        {
            List<long> list = new List<long>();
            foreach (var value in ReceivedNumbers.Values) list.Add(value);
            // Used easy way with standart sorting;
            list.Sort();
            var result = (list[1008] + list[1009]) / 2.0f;
            Console.WriteLine($"All tasks completed, result: {result}");
            
            return result;
        }

        /// <summary>
        /// Send message to server.
        /// </summary>
        /// <param name="message"> Message</param>
        public  void SendMessage(string message)
        {
            try
            {
                using TcpClient client = new TcpClient();
                client.Connect(ip, port);
                using NetworkStream stream = client.GetStream();
                using BinaryWriter writer = new BinaryWriter(stream);
                using BinaryReader reader = new BinaryReader(stream);
                writer.Write(usedEncoding.GetBytes(message));
                writer.Flush();
                var responseInBytes = reader.ReadBytes(10000);
                var response = usedEncoding.GetString(responseInBytes);
                if (message == newKeyMes)
                {
                    Console.Write($"New key received:   ");
                    key = GetKeyFromResponseString(response);
                }
                Console.WriteLine(response);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
        }

        /// <summary>
        /// Get a number from a message. If no response is received within the time limit,
        /// the connection is closed without waiting for completion.
        /// </summary>
        /// <param name="index">Requested number</param>
        /// <returns>Requested number, or -1 on failure</returns>
        public ServerResponse GetNumberFromStream(int index)
        {
            using TcpClient client = new TcpClient();
            try
            {
                var lastKey = key.Clone().ToString();
                client.Connect(ip, port);
                string message = $"{key}|{index}\n";
                using NetworkStream stream = client.GetStream();
                using BinaryWriter writer = new BinaryWriter(stream);
                writer.Write(usedEncoding.GetBytes(message));
                writer.Flush();
                BinaryReader reader = new BinaryReader(stream);
                stream.ReadTimeout = maxDelay;
                var res = usedEncoding.GetString(reader.ReadBytes(10000));
                Console.WriteLine($"Received string: {res}");
                CheckKeyHasExpiredMessage(res, lastKey);
                var currentResult = GetNumberFromString(res, index);

                return new ServerResponse(index, currentResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new ServerResponse(index, notReceieved);
        }

        private long GetNumberFromString(string str, int index)
        {
            long currentNumber = notReceieved;
            if (!char.IsDigit(str.LastOrDefault()) && str.Any(char.IsDigit))
            {
                long.TryParse(string.Join("", str.Where(char.IsDigit)), out currentNumber);
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"Completed {currentTaskComplited++}, number: {index}, result:{currentNumber}");
                Console.ResetColor();
            }

            return currentNumber;
        }

        private void CheckKeyHasExpiredMessage(string result, string lastKey)
        {
            if(!result.Contains(keyHasExpiredText)) return;
            
            lock (keyReceivingLocker)
            {
                if(lastKey != key) return;

                SendMessage(newKeyMes);
            }
        }

        private string GetKeyFromResponseString(string str)
        {
            return str.Substring(encdoeSymbolsCount, str.Length - (badSymvolsCount + encdoeSymbolsCount));
        }

        public void SaveToFile(object sender, EventArgs e)
        {
            FileSaverAndLoader.SaveNumbersToFile(ReceivedNumbers);
        }
    }
}
