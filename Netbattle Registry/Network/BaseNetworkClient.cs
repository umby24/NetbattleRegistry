using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Netbattle_Registry.Common;
using Sockets;
using TaskScheduler = Netbattle_Registry.Common.TaskScheduler;

namespace Netbattle_Registry.Network {
    /// <summary>
    /// Base network client for shared methods between other types of network clients.
    /// </summary>
    public abstract class BaseNetworkClient : TaskItem {
        public string Ip { get; set; }
        public bool EncryptionEnabled;

        private readonly ByteBuffer _receiveBuffer;
        public ByteBuffer SendBuffer;
        protected readonly ClientSocket Socket;
        private DateTime _lastActive;
        protected readonly string TaskId;
        private readonly object _locker = new Object();
        protected Dictionary<string, IRegPacket> Packets;

        protected BaseNetworkClient(TcpClient client) {
            DataAvailable = false;
            EncryptionEnabled = true;
            _receiveBuffer = new ByteBuffer();
            SendBuffer = new ByteBuffer();
            Socket = new ClientSocket();
            PopulatePackets();

            Socket.DataReceived += ReceivedSocketData;
            Socket.Disconnected += SocketDisconnected;
            SendBuffer.DataAdded += SendBufferDataAdded;

            Ip = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address.ToString();
            Socket.Accept(client);
            _lastActive = DateTime.UtcNow;

            // -- Setup the timeout task
            Interval = TimeSpan.FromSeconds(1);
            TaskId = Ip + new Random().Next(2035, 193876957);
            TaskId = TaskScheduler.RegisterTask(TaskId, this);
        }

        /// <summary>
        /// Cross-thread safe packet sending
        /// </summary>
        /// <param name="packet"></param>
        public void SendPacket(IRegPacket packet) {
            lock (SendBuffer) {
                packet.Write(SendBuffer);
            }
        }

        private void SendBufferDataAdded() {
            lock (SendBuffer) {
                DataAvailable = true;
            }
        }

        private void SocketDisconnected(Sockets.EventArgs.SocketDisconnectedArgs args) {
            Shutdown();
        }

        private void ReceivedSocketData(Sockets.EventArgs.DataReceivedArgs args) {
            lock (_locker) {
                _receiveBuffer.AddBytes(args.Data);
                _lastActive = DateTime.UtcNow;
            }
        }

        protected abstract void PopulatePackets();

        public abstract void Shutdown();

        public bool DataAvailable { get; set; }

        public void Handle() {
            var maxIterations = 10;

            while (true) {
                if (_receiveBuffer.Length == 0)
                    break;

                if (maxIterations == 0)
                    break;

                var reqLength = (byte)(_receiveBuffer.PeekByte() + 1);

                if (_receiveBuffer.Length - 1 < reqLength)
                    break;

                _receiveBuffer.ReadByte(); // -- Dispose of length code.

                byte[] data = EncryptionEnabled
                    ? XorModule.XorDecrypt(_receiveBuffer.ReadByteArray(reqLength))
                    : _receiveBuffer.ReadByteArray(reqLength);

                var tempBuffer = new ByteBuffer();
                tempBuffer.AddBytes(data);
                string cmd = tempBuffer.ReadString(5);

                IRegPacket packet;
                if (!Packets.TryGetValue(cmd.Substring(0, 4), out packet) && !Packets.TryGetValue(cmd, out packet)) {
                    Logger.Log(LogType.Error, "Invalid packet received!!");
                    return;
                }

                packet.Read(tempBuffer);
                packet.Handle(this);
            }
        }

        public void SendQueued() {
            lock (SendBuffer) {
                byte[] allBytes = SendBuffer.GetAllBytes();
                Socket.Send(allBytes);
                DataAvailable = false;
            }
        }

        public override void Setup() {
        }

        public override void Main() {
            TimeSpan span = (DateTime.UtcNow - _lastActive);

            if (span.TotalSeconds < 30 && span.TotalSeconds > 5) {
                SendPacket(new PingRegPacket());
                return;
            }

            if ((DateTime.UtcNow - _lastActive).TotalSeconds >= 30) {
                Logger.Log(LogType.Info, $"Disconnecting {Ip}: Timed out.");
                Shutdown();
            }
        }

        public override void Teardown() {
            Shutdown();
        }
    }
}
