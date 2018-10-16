using System.Collections.Generic;
using System.Net.Sockets;
using Netbattle_Registry.Common;
using TaskScheduler = Netbattle_Registry.Common.TaskScheduler;

namespace Netbattle_Registry.Network {
    /// <summary>
    /// Network client for handling incoming information from a netbattle server, looking to be listed.
    /// </summary>
    public class NbServerClient : BaseNetworkClient {
        public ServerListing ServerInfo;
        /// <summary>
        /// True if this server has been broadcast to all clients.
        /// </summary>
        public bool Sent { get; set; }
        public bool Registered { get; set; }
        public string ServerVersion { get; set; }

        public NbServerClient(TcpClient client) : base(client) {
            if (Configuration.Settings.BannedIps.Contains(Ip)) {
                Shutdown();
                return;
            }

            NetworkServer.RegisterClient(this);
            SendPacket(new NoArgPacket { Cmd = Constants.RequestInfoCommand });
        }

        public void BroadcastToClients() {
            var servInfo = new ServerRegPacket {
                ServerData = ServerInfo
            };

            NetworkServer.SendToAllNbClients(servInfo);
        }

        public bool SanityCheck() {
            if (ServerInfo.OnlinePlayers > ServerInfo.MaxPlayers)
                return false;

            if (string.IsNullOrEmpty(ServerInfo.Name))
                return false;

            if (string.IsNullOrEmpty(ServerInfo.Owner))
                return false;

            return true;
        }

        protected override void PopulatePackets() {
            Packets = new Dictionary<string, IRegPacket> {
                {"INFO", new RegisterServerPacket() },
                {"ADMC", new AdminChangePacket() },
                {"NAMC", new NameChangePacket() },
                {"DESC", new DescriptionChange() },
                {"USRC", new OnlineUsersChange() },
                {"PASS", new Password() },
                {"PONG", new NoArgPacket() },
                {"MAXC", new MaxUserChange() },
                {"EXIT", new DisconnectPacket() }
            };
        }

        public override void Shutdown() {
            TaskScheduler.UnregisterTask(TaskId);
            NetworkServer.UnregisterClient(this);

            if (Socket.IsConnected)
                Socket.Disconnect("");
        }
    }
}
