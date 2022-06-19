using System;

namespace GetNumbersFromServer
{
    [Serializable]
    class ServerResponce
    {
        public long Result { get; set; }
        public int Number { get; set; }

        public ServerResponce(int number, long result)
        {
            Result = result;
            Number = number;
        }
    }
}
