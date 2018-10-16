using System;
using System.Threading;
using Netbattle_Registry.Common;
using Netbattle_Registry.Network;
using TaskScheduler = Netbattle_Registry.Common.TaskScheduler;

namespace Netbattle_Registry {
    class Program {
        private static NetworkServer _registryServer;

        static void Main(string[] args) {
            TaskScheduler.RunSetupTasks();
            
            var config = new Configuration();
            config.Load();

            _registryServer = new NetworkServer();
            _registryServer.Start();

            if (Configuration.Settings.ServerView) {
                Logger.ConsoleOutEnabled = false;
                TaskScheduler.RegisterTask("Server Display", new ServerDisplayTask());
            }

            var mainThread = new Thread(TaskHandlerThread);
            mainThread.Start();

            while (_registryServer.Running) {
                string myInput = Console.ReadLine();

                if (myInput == null)
                    continue;

                myInput = myInput.Trim();

                if (myInput == "quit") {
                    break;
                }

                var mass = new MassMessage { Message = myInput };
                NetworkServer.SendToAllNbServers(mass);
            }

            _registryServer.Running = false;
            TaskScheduler.RunTeardownTasks();
        }

        static void TaskHandlerThread() {
            while (_registryServer.Running) {
                TaskScheduler.RunMainTasks();
                Thread.Sleep(1);
            }
        }
        
    }
}
