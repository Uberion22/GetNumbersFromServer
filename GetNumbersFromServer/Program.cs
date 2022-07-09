using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class Program
    {
        private const int Port = 2013;
        private const string WelcomeMessage = "Greetings\n";
        private const string CheckAdvanced = "Check_Advanced";
        private const string Check = "Check";
        private const float AdvancedResult = 4922031.5f;
        private const float Result = 4925680.5f;

        static async Task Main(string[] args)
        {
            var server = Console.ReadLine();
            var numbersLoader = new NumbersLoader(server, Port);
            //numbersLoader.SendMessage(WelcomeMessage);
            //numbersLoader.SendMessage(getKeyMessage);
            //var result = await numbersLoader.StartGettingNumbersTasksAsync();
            numbersLoader.SendMessage($"{CheckAdvanced} {AdvancedResult}\n");
            //numbersLoader.SendMessage($"{checkAdvanced} {result}\n");
            Console.ReadLine();
        }
    }
}
