using System;
using System.Collections.Generic;
using System.IO;
using Netbattle_Registry.Common;
using Newtonsoft.Json;

namespace Netbattle_Registry {
    public class Configuration {
        public static Configuration Settings;
        public List<string> BannedIps { get; set; }
        public List<string> BannedSids { get; set; }
        public List<ServerRegistration> Registrations { get; set; }
        public bool ServerView { get; set; }

        public Configuration() {
            BannedIps = new List<string>();
            BannedSids = new List<string>();
            Registrations = new List<ServerRegistration>();
            ServerView = true;

            if (Settings == null)
                Settings = this;
        }

        public void Save() {
            try {
                File.WriteAllText("settings.json", JsonConvert.SerializeObject(Settings, Formatting.Indented));
            }
            catch (Exception e) {
                Logger.Log(LogType.Error, "Failed to save Settings.Json file!");
                Logger.Log(LogType.Debug, e.Message);
                Logger.Log(LogType.Debug, e.StackTrace);
            }
        }

        public void Load() {
            if (!File.Exists("settings.json")) {
                Save();
                return;
            }

            try {
                Settings = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("settings.json"));
            }
            catch (Exception e) {
                Logger.Log(LogType.Error, "Failed to load Settings.Json file!");
                Logger.Log(LogType.Debug, e.Message);
                Logger.Log(LogType.Debug, e.StackTrace);
            }
        }
    }
}
