using System;

namespace GetNumbersFromServer
{
    [Serializable]
    class ServerResponse
    {
        public long Result { get; set; }
        public int Number { get; set; }

        public ServerResponse(int number, long result)
        {
            Result = result;
            Number = number;
        }
    }
}
