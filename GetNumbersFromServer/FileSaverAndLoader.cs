using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace GetNumbersFromServer
{
    class FileSaverAndLoader
    {
        private static string eventsBackupFilePath =@"D:\ServerResponses.txt";

        /// <summary>
        /// Save received numbers to file.
        /// </summary>
        public static void SaveNumbersToFile(Dictionary<int, long> responses)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(eventsBackupFilePath);
            bf.Serialize(file, responses);
            file.Close();
        }

        /// <summary>
        ///  Load received numbers from file.
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

            return recivedNumbers;
        }
    }
}
