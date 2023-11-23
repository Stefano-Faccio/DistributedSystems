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

            crashed = true;
        }

        protected void onRecovery(RecoveryMessage message)
        {
            if (!crashed) return;

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
            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} response GET NODE LIST");

            var new_nodes = message.Nodes;
            this.nodes = new_nodes;
            uint prev_node = this.Id;
            uint next_node = this.Id;
            foreach (var n in this.nodes)
            {
                if (n < this.Id && n > prev_node) prev_node = n;
                if (n > this.Id && n < next_node) next_node = n;
            }

            Context.ActorSelection($"/user/node{prev_node}").Tell(new GetKeysListMessage(this.Id));
            Context.ActorSelection($"/user/node{next_node}").Tell(new GetKeysListMessage(this.Id));

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} asking to {prev_node} and {next_node} for my keys");
        }

        protected void GetKeysListResponseRecovery(GetKeysListResponseMessage message)
        {
            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} response GET KEY LIST");

            foreach (var k in message.KeysList)
            {
                if (!this.data.ContainsKey(k))
                {
                    if (sendDebug)
                        lock (Console.Out)
                            Console.WriteLine($"{Self.Path.Name} asking to {Sender.Path.Name} new key value: {k}");
                    Context.ActorSelection($"/user/node{Sender.Path.Name}").Tell(new GetMessage(this.Id));
                }
            }

            Timers.StartSingleTimer($"Recovery{myMersenneTwister.Next()}", new BackOnlineMessage(this.Id), TimeSpan.FromMilliseconds(TIMEOUT_TIME));
        }

        protected void GetResponseRecovery(GetResponseMessage message)
        {
            if (message.Value == null) return;

            if (sendDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} response GET KEY {message.Key} value {message.Value}");

            data.Add(message.Key, message.Value);
        }

        protected void BackOnline(BackOnlineMessage message)
        {
            if (receiveDebug)
                lock (Console.Out)
                    Console.WriteLine($"{Self.Path.Name} is back online");

            crashed = false;
        }
    }
}
