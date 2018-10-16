using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Netbattle_Registry.Common;
using TaskScheduler = Netbattle_Registry.Common.TaskScheduler;

namespace Netbattle_Registry.Network {
    /// <summary>
    /// Network client specific to netbattle clients looking for a server listing.
    /// </summary>
    public class NetbattleClient : BaseNetworkClient {
        public NetbattleClient(TcpClient client) : base(client) {
            if (Configuration.Settings.BannedIps.Contains(Ip)) {
                Shutdown();
                return;
            }

            NetworkServer.RegisterClient(this);
            SendServers();
        }

        protected override void PopulatePackets() {
            Packets = new Dictionary<string, IRegPacket> {
                {"PONG", new NoArgPacket { Cmd = Constants.PongCommand} },
            };
        }

        /// <summary>
        /// Sends a list of all currently registered servers to the client.
        /// </summary>
        public void SendServers() {
            List<NbServerClient> orderedServers = NetworkServer.RoNbServers.OrderBy(a => a.ServerInfo.OnlinePlayers).ToList();
            var index = 1;

            foreach (NbServerClient orderedServer in orderedServers) {
                orderedServer.ServerInfo.ServerNumber = index;
                var srvPacket = new ServerRegPacket {
                    ServerData = orderedServer.ServerInfo
                };

                SendPacket(srvPacket);
                index++;
            }
        }

        public override void Shutdown() {
            TaskScheduler.UnregisterTask(TaskId);
            NetworkServer.UnregisterClient(this);

            if (Socket.IsConnected)
                Socket.Disconnect("");
        }
    }
}
