using System;
using Netbattle_Registry.Common;
using Netbattle_Registry.Network;

namespace Netbattle_Registry {
   public class ServerDisplayTask : TaskItem {
       public ServerDisplayTask() {
           Interval = TimeSpan.FromSeconds(5);
        }

        public override void Setup() {
        }

        public override void Main() {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("========== Netbattle 9.7 Registry === " + NetworkServer.RoNbServers.Length + " Servers online.");
            Console.WriteLine("");
            var i = 0;

            foreach (NbServerClient serverClient in NetworkServer.RoNbServers) {
                if (i > 10)
                    break;
                if (serverClient.Sent) {
                    Console.ForegroundColor = serverClient.Registered ? ConsoleColor.Green : ConsoleColor.Cyan;

                    Console.WriteLine(
                        $"{serverClient.ServerInfo.Name.PadRight(20)}{serverClient.ServerInfo.Owner.PadRight(20)}{serverClient.ServerInfo.OnlinePlayers}/{serverClient.ServerInfo.MaxPlayers}");
                }

                i++;
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public override void Teardown() {
        }
    }
}
