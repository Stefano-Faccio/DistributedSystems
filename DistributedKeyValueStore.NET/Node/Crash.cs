using Akka.Actor;
using static DistributedKeyValueStore.NET.Constants;

namespace DistributedKeyValueStore.NET
{
    internal partial class Node : UntypedActor, IWithTimers
    {
        protected void onCrash(CrashMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} has crashed");

            active = false;
        }

        protected void onRecovery(RecoverMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} is recoverying...");
        }
    }
}
