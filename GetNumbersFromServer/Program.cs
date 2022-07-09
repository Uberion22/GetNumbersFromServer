using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace GetNumbersFromServer
{
    class Program
    {
        private const int Port = 2013;
        private const string Server = "88.212.241.115";
        private const string GreetengsMessage = "Greetings\n";
        private const string CheckAdvanced = "Check_Advanced";
        private const string Check = "Check";
        private const float AdvancedResult = 4922031.5f;
        private const float Result = 925680.5f;

        static async Task Main(string[] args)
        {
            var numbersLoader = new NumbersLoader(Server, Port);
            //numbersLoader.SendMessage(greetengsMessage);
            //numbersLoader.SendMessage(getKeyMessage);
            //var result = await numbersLoader.StartGettingNumbersTasksAsync();
            numbersLoader.SendMessage($"{CheckAdvanced} {AdvancedResult}\n");
            //numbersLoader.SendMessage($"{checkAdvanced} {result}\n");
            Console.ReadLine();
        }
    }
}
