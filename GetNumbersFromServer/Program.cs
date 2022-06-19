using System;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class Program
    {
        private const int port = 2013;
        private const string server = "88.212.241.115";
        private const string velocomeMessage = "Greetings\n";

        static async Task Main(string[] args)
        {

            var numbersLoader = new NumbersLoader(server, port);
            numbersLoader.GetMessage(velocomeMessage);

            var result = await numbersLoader.StartTasksAsync();

            numbersLoader.GetMessage($"Check {result}\n");

            Console.ReadLine();
        }
    }
}
