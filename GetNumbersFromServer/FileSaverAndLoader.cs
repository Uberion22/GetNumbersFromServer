using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace GetNumbersFromServer
{
    class FileSaverAndLoader
    {
        private static string eventsBackupFilePath = @"D:\ServerResponces.txt";

        /// <summary>
        /// Save undelivered events to file.
        /// </summary>
        public static void SaveNumbersToFile(Dictionary<int, long> responces)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(eventsBackupFilePath);
            bf.Serialize(file, responces);
            file.Close();
        }

        /// <summary>
        ///  Load undelivered events from file.
        /// </summary>
        /// <returns>List of events</returns>
        public static Dictionary<int, long> LoadNumbersFromFile()
        {
            var recivedNumbers = new Dictionary<int, long>();
            if (File.Exists(eventsBackupFilePath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(eventsBackupFilePath, FileMode.Open);
                recivedNumbers = (Dictionary<int, long>)bf.Deserialize(file);
                file.Close();
            }
            else
            {
                for (int i = 1; i <= 2018; i++)
                {
                    recivedNumbers.Add(i, -1);
                }
            }

            recivedNumbers[1125] = -1;
            recivedNumbers[1956] = -1;
            recivedNumbers[1986] = -1;
            //recivedNumbers[746] = -1;
            //recivedNumbers[1036] = -1;
            return recivedNumbers;
        }
    }
}
