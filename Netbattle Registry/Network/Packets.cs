using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netbattle_Registry.Common;

namespace Netbattle_Registry.Network {
    public struct ServerRegPacket : IRegPacket {
        public string Command => "SERV";
        public ServerListing ServerData;

        public void Read(ByteBuffer reader) {

            ServerData = new ServerListing {
                ServerNumber = Convert.ToInt32(reader.ReadString(2), 16),
                Name = reader.ReadString(20).Trim(),
                Owner = reader.ReadString(20).Trim(),
                OnlinePlayers = Convert.ToInt32(reader.ReadString(2), 16),
                MaxPlayers = Convert.ToInt32(reader.ReadString(2), 16),
                Ip = reader.ReadString(8),
                Description = reader.ReadString(reader.Length)
            };
        }

        public void Write(ByteBuffer writer) {
            var sb = new StringBuilder();
            sb.Append(Command);
            sb.Append(":");
            sb.Append(NbMethods.FixedHex(ServerData.ServerNumber, 2));
            sb.Append(ServerData.Name.PadRight(20));
            sb.Append(ServerData.Owner.PadRight(20));
            sb.Append(NbMethods.FixedHex(ServerData.OnlinePlayers, 2));
            sb.Append(NbMethods.FixedHex(ServerData.MaxPlayers, 2));
            sb.Append(ServerData.Ip);
            sb.Append(ServerData.Description);

            writer.WriteAndPrefixString(sb.ToString());
            writer.Purge();
        }

        public void Handle(BaseNetworkClient listForm) {
        }
    }

    public struct PingRegPacket : IRegPacket {
        public string Command => "PING";
        public void Read(ByteBuffer reader) {
        }

        public void Write(ByteBuffer writer) {
            writer.WriteAndPrefixString("PING:");
            writer.Purge();
        }

        public void Handle(BaseNetworkClient listForm) {
            var pct = new NoArgPacket {Cmd = Constants.PongCommand};
            listForm.SendPacket(pct);
        }
    }

    public struct NoArgPacket : IRegPacket {
        public string Cmd { get; set; }

        public void Read(ByteBuffer reader) {
            
        }

        public void Write(ByteBuffer writer) {
            writer.WriteAndPrefixString(Cmd);
            writer.Purge();
        }

        public void Handle(BaseNetworkClient listForm) {
            
        }
    }

    public struct MassMessage : IRegPacket {
        public string Command => "MASS";
        public string Message;

        public void Read(ByteBuffer reader) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Write(ByteBuffer writer) {
            writer.WriteAndPrefixString(Command + ":" + Message);
            writer.Purge();
        }

        public void Handle(BaseNetworkClient listForm) {
            throw new Exception("Packet should not be sent in this direction.");
        }
    }

    public struct RegisterServerPacket : IRegPacket {
        public string Command => "INFO";
        public string ServerName;
        public string ServerAdmin;
        public byte OnlinePlayers, MaxPlayers;
        public byte[] StationId;
        public string Description;
        public string Version;

        public void Read(ByteBuffer reader) {
            ServerName = reader.ReadString(20);
            ServerAdmin = reader.ReadString(20);
            OnlinePlayers = reader.ReadByte();
            MaxPlayers = reader.ReadByte();
            Version = reader.ReadString(8); // -- Index 43..
            StationId = reader.ReadByteArray(13); // -- index 51..
            Description = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            if (string.IsNullOrEmpty(ServerName) || string.IsNullOrEmpty(ServerAdmin)) {
                listForm.Shutdown();
                return;
            }

            var newInfo = new ServerListing {
                Name = ServerName,
                Description = Description,
                Ip = NbMethods.PackIp(listForm.Ip),
                MaxPlayers = MaxPlayers,
                OnlinePlayers = OnlinePlayers,
                Owner = ServerAdmin,
                StationId = NbMethods.DecompressSid(StationId)
            };

            var nbServer = (NbServerClient)listForm;
            nbServer.ServerInfo = newInfo;  // -- Set Info
            nbServer.ServerVersion = Version;

            // -- if version <> 'VERSION' constant, send OLDVR.
            if (Version != Constants.NetbattleVersion) {
                Logger.Log(LogType.Warning, $"Disconnecting server, outdated netbattle version! {Version}");
                nbServer.SendPacket(new NoArgPacket { Cmd = Constants.OutdatedServerCommand });
                return;
            }

            if (!nbServer.SanityCheck()) {
                Logger.Log(LogType.Warning, "Disconnecting server, hacking detected! (Invalid registration data)");
                nbServer.Shutdown();
                return;
            }

            Logger.Log(LogType.Info, $"Registered server {ServerName} running netbattle {Version}");

            // -- If ClientsKnow, kick.
            if (nbServer.Sent) {
                nbServer.Shutdown();
                return;
            }

            List<NbServerClient> matchingServers = NetworkServer.RoNbServers
                .Where(a => a.ServerInfo.Name.ToLower() == nbServer.ServerInfo.Name).ToList();

            // -- If another server has the same name, send NMIU: packet.
            if (matchingServers.Count > 0) {
                nbServer.SendPacket(new NoArgPacket { Cmd = Constants.DuplicateNameCommand });
                return;
            }


            // -- if servername matches another in 'entry', AND the SID is different, send NAMPW. (Attempt at users not stealing server names.. hmm).
            List<ServerRegistration> listings = ServerDatabase.Get(nbServer.ServerInfo.Name);

            if (listings.Count > 0) {
                if (listings[0].Sid == newInfo.StationId) {
                    nbServer.Registered = true;
                }
                else {
                    nbServer.SendPacket(new NoArgPacket { Cmd = Constants.ServerPasswordRequestCommand });
                    return;
                }
            }

            // Constants.ServerPasswordRequest
            // -- Search entry again, if there is an SID match against this server, update the server name for this SID, and set the server as 'Regged'.


            // -- Refresh listing.. (Sorts UI..)
            // -- Send to everyone connected a SERV entry for this server.
            nbServer.BroadcastToClients();

            // -- Set 'ClientsKnow' to true.
            nbServer.Sent = true;
            // -- Send the server, 'OKAY!"

            string cmd = !nbServer.Registered ? Constants.ServerAcknowledgedCommand : Constants.ServerAcknowledgedRegistered;

            var ack = new NoArgPacket {Cmd = cmd};
            listForm.SendPacket(ack);
        }
    }

    public struct NameChangePacket : IRegPacket {
        public string Command => "NAMC";
        public string NewName;

        public void Read(ByteBuffer reader) {
            NewName = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            if (string.IsNullOrEmpty(NewName)) {
                listForm.Shutdown();
                return;
            }

            var server = (NbServerClient)listForm;
            server.ServerInfo.Name = NewName;
            server.BroadcastToClients();
        }
    }

    public struct AdminChangePacket : IRegPacket {
        public string Command => "ADMC";
        public string NewName;

        public void Read(ByteBuffer reader) {
            NewName = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            if (string.IsNullOrEmpty(NewName)) {
                listForm.Shutdown();
                return;
            }
            var server = (NbServerClient)listForm;
            server.ServerInfo.Owner = NewName;
            server.BroadcastToClients();
        }
    }

    public struct DescriptionChange : IRegPacket {
        public string Command => "DESC";
        public string NewName;

        public void Read(ByteBuffer reader) {
            NewName = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            var server = (NbServerClient)listForm;
            server.ServerInfo.Description = NewName;
            server.BroadcastToClients();
        }
    }

    public struct MaxUserChange : IRegPacket {
        public string Command => "MAXC";
        public string NewName;

        public void Read(ByteBuffer reader) {
            NewName = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            var server = (NbServerClient)listForm;
            server.ServerInfo.MaxPlayers = int.Parse(NewName);
            server.BroadcastToClients();
        }
    }

    public struct OnlineUsersChange : IRegPacket {
        public string Command => "USRC";
        public string NewName;

        public void Read(ByteBuffer reader) {
            NewName = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            var server = (NbServerClient)listForm;
            server.ServerInfo.OnlinePlayers = int.Parse(NewName);
            server.BroadcastToClients();
        }
    }

    public struct DisconnectPacket : IRegPacket {
        public void Read(ByteBuffer reader) {

        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient client) {
            client.Shutdown();
        }
    }

    public struct Password : IRegPacket {
        public string Md5 { get; set; }

        public void Read(ByteBuffer reader) {
            Md5 = reader.ReadString(reader.Length);
        }

        public void Write(ByteBuffer writer) {
            throw new Exception("Packet should not be sent in this direction.");
        }

        public void Handle(BaseNetworkClient listForm) {
            var nbServer = (NbServerClient) listForm;

            if (nbServer.Sent && NetworkServer.RoNbServers.Any(a => a.ServerInfo.Name.ToLower() == nbServer.ServerInfo.Name)) {
                nbServer.Shutdown();
                return;
            }

            // -- Search the server DB, for an entry with the matching server name.
            List<ServerRegistration> listings = ServerDatabase.Get(nbServer.ServerInfo.Name);
            // -- If the incoming password is the same as the listed server password, update the SID in the server DB.
            if (listings.Count > 0 && listings[0].Password == Md5) {
                nbServer.Registered = true;

                if (!nbServer.Sent) {
                    nbServer.SendPacket(new NoArgPacket { Cmd = Constants.ServerAcknowledgedRegistered });
                    nbServer.BroadcastToClients();
                    nbServer.Sent = true;
                }

                nbServer.SendPacket(new NoArgPacket { Cmd= Constants.PasswordOkCommand});

                ServerRegistration myListing = listings[0];
                myListing.Sid = nbServer.ServerInfo.StationId;

                ServerDatabase.Update(myListing);
            } else if (listings.Count > 0) {
                nbServer.SendPacket(new NoArgPacket { Cmd = Constants.PasswordWrongCommand });
            }
            else {
                nbServer.SendPacket(new NoArgPacket { Cmd = Constants.ServerRegisteredCommand });
                var newEntry = new ServerRegistration {
                    Name = nbServer.ServerInfo.Name,
                    Password = Md5,
                    Sid = nbServer.ServerInfo.StationId
                };

                nbServer.Registered = true;
                Configuration.Settings.Registrations.Add(newEntry);
                Configuration.Settings.Save();
            }
        }
    }
}
