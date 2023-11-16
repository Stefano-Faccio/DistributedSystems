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

        protected void onRecovery(RecoveryMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} is recoverying...");

            Context.ActorSelection($"/user/node{message.NodeToContactForList}").Tell(new GetNodeListMessage(this.Id));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} request GET NODE LIST to node {message.NodeToContactForList}");
        }

        protected void RecoveryGetNodeListResponse(GetNodeListResponseMessage message)
        {
            var new_nodes = message.Nodes;
        }

        protected void GetResponseRecovery(GetResponseMessage message)
        {

        }
    }
}
