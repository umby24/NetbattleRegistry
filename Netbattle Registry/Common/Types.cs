using System;
using Netbattle_Registry.Network;

namespace Netbattle_Registry.Common {
    public interface IRegPacket {
        void Read(ByteBuffer reader);
        void Write(ByteBuffer writer);
        void Handle(BaseNetworkClient client);
    }

    public struct ServerListing {
        public int ServerNumber { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
        public string Ip { get; set; }
        public string StationId { get; set; }
        public int OnlinePlayers { get; set; }
        public int MaxPlayers { get; set; }
    }

    public delegate void EmptyEventArgs();

    public enum LogType {
        Verbose,
        Debug,
        Error,
        Warning,
        Info
    }

    public struct ServerRegistration {
        public string Name { get; set; }
        public string Sid { get; set; }
        public string Password { get; set; }
    }

    public struct LogItem {
        public LogType Type;
        public DateTime Time;
        public string Message;
    }

    public abstract class TaskItem {
        public TimeSpan Interval;
        public DateTime LastRun;
        public abstract void Setup();
        public abstract void Main();
        public abstract void Teardown();
    }
}
