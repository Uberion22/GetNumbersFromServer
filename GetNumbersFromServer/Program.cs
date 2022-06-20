using System;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class Program
    {
        private const int port = 2013;
        private const string server = "88.212.241.115";
        private const string greetengsMessage = "Greetings\n";

        static async Task Main(string[] args)
        {
            var numbersLoader = new NumbersLoader(server, port);
            numbersLoader.SendMessage(greetengsMessage);
            //result = 4925680,5
            var result = await numbersLoader.StartGettingNumbersTasksAsync();

            numbersLoader.SendMessage($"Check {result}\n");

            Console.ReadLine();
        }
    }
}
