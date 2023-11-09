using MathNet.Numerics.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal static class Constants
    {
        //MersenneTwister per i numeri casuali
        public static MersenneTwister MersenneTwister = new MersenneTwister(Guid.NewGuid().GetHashCode());

        public static readonly int N = 3;
        public static readonly int READ_QUORUM = 2;
        public static readonly int WRITE_QUORUM = 2;
        public static readonly int TIMEOUT_TIME = 100; //In millisec
        public static readonly int INIT_QUORUM = 2;

        public static readonly bool generalDebug = true;
        public static readonly bool deepDebug = false;
        public static readonly bool receiveDebug = false;
        public static readonly bool sendDebug = true;
    }
}
