using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Netbattle_Registry.Common;
using Sockets;

namespace Netbattle_Registry.Network {
    /// <summary>
    /// Class that handles listening for new clients, and running the client parse loops.
    /// </summary>
    public class NetworkServer : TaskItem {
        private readonly ServerSocket _nbClientListener;
        private readonly ServerSocket _nbServerListener;

        private static List<NetbattleClient> _nbClients;
        private static List<NbServerClient> _nbServers;

        private static NetbattleClient[] _roNbClients = new NetbattleClient[0];
        public static NbServerClient[] RoNbServers = new NbServerClient[0];

        private Thread _nbServerThread;
        private Thread _nbClientThread;

        public bool Running { get; set; }

        public NetworkServer() {
            Interval = TimeSpan.FromSeconds(5);

            _nbClients = new List<NetbattleClient>();
            _nbServers = new List<NbServerClient>();

            _nbClientListener = new ServerSocket(30002);
            _nbClientListener.IncomingClient += IncomingNbClient;

            _nbServerListener = new ServerSocket(30001);
            _nbServerListener.IncomingClient += IncomingNbServer;
        }

        public void Start() {
            _nbServerListener.Listen();
            _nbClientListener.Listen();
            Running = true;

            _nbServerThread = new Thread(HandleServerData);
            _nbClientThread = new Thread(HandleClientData);
            _nbServerThread.Start();
            _nbClientThread.Start();

            TaskScheduler.RegisterTask("Network Server", this);
            Logger.Log(LogType.Info, "Netbattle Registry Listening.");
        }

        public static void SendToAllNbClients(IRegPacket packet) {
            foreach (NetbattleClient nbClient in _roNbClients) {
                nbClient.SendPacket(packet);
            }
        }

        public static void SendToAllNbServers(IRegPacket packet) {
            foreach (NbServerClient serverClient in RoNbServers) {
                serverClient.SendPacket(packet);
            }
        }

        public static void RegisterClient(NbServerClient c) {
            lock (_nbServers) {
                _nbServers.Add(c);
                RoNbServers = _nbServers.ToArray();
            }
        }

        public static void RegisterClient(NetbattleClient c) {
            lock (_nbClients) {
                _nbClients.Add(c);
                _roNbClients = _nbClients.ToArray();
            }
        }

        public static void UnregisterClient(NetbattleClient c) {
            lock (_nbClients) {
                _nbClients.Remove(c);
                _roNbClients = _nbClients.ToArray();
            }
        }

        public static void UnregisterClient(NbServerClient c) {
            lock (_nbServers) {
                _nbServers.Remove(c);
                RoNbServers = RoNbServers.ToArray();
            }
        }

        private void IncomingNbServer(Sockets.EventArgs.IncomingEventArgs args) {
            // ReSharper disable once ObjectCreationAsStatement
            new NbServerClient(args.IncomingClient);
        }

        private void IncomingNbClient(Sockets.EventArgs.IncomingEventArgs args) {
            // ReSharper disable once ObjectCreationAsStatement
            new NetbattleClient(args.IncomingClient);
        }

        private void HandleClientData() {
            while (Running) {
                foreach (NetbattleClient roNbClient in _roNbClients) {
                    if (roNbClient.DataAvailable) {
                        roNbClient.SendQueued();
                    }

                    roNbClient.Handle();
                }
                Thread.Sleep(1);
            }
        }

        private void HandleServerData() {
            while (Running) {
                foreach (NbServerClient roNbServer in RoNbServers) {
                    if (roNbServer.DataAvailable) {
                        roNbServer.SendQueued();
                    }

                    roNbServer.Handle();
                }
                Thread.Sleep(1);
            }
        }

        public override void Main() {
        }

        public override void Setup() {
        }

        public override void Teardown() {
            _nbClientThread.Abort();
            _nbServerThread.Abort();

            _nbClientListener.Stop();
            _nbServerListener.Stop();

            foreach (NetbattleClient netbattleClient in _roNbClients) {
                netbattleClient.Shutdown();
            }

            foreach (NbServerClient serverClient in RoNbServers) {
                serverClient.Shutdown();
            }
        }
    }
}
