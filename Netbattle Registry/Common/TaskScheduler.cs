using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Netbattle_Registry.Common {
    public static class TaskScheduler {
        private static readonly ConcurrentDictionary<string, TaskItem> Tasks = new ConcurrentDictionary<string, TaskItem>();

        public static string RegisterTask(string name, TaskItem item) {
            if (Tasks.TryGetValue(name, out TaskItem _)) {
                Logger.Log(LogType.Warning, $"Attempted to register already present task: {name}");
                name = name + new Random().Next(25, int.MaxValue);
                return RegisterTask(name, item);
            }

            Tasks.TryAdd(name, item);
            Logger.Log(LogType.Info, $"Registered task {name}");
            return name;
        }

        public static void UnregisterTask(string name) {
            if (!Tasks.TryGetValue(name, out TaskItem _)) {
                Logger.Log(LogType.Warning, $"Attempted to unregister non-existing task: {name}");
                return;
            }

            Tasks.TryRemove(name, out TaskItem _);
        }

        public static void RunSetupTasks() {
            foreach (KeyValuePair<string, TaskItem> taskItem in Tasks) {
                try {
                    taskItem.Value.Setup();
                } catch (Exception e) {
                    Logger.Log(LogType.Error, $"Error occurred starting {taskItem.Key}: {e.Message}");
                    Logger.Log(LogType.Debug, $"Stacktrace: {e.StackTrace}");

                    if (e.InnerException == null)
                        continue;

                    Logger.Log(LogType.Error, $"Inner Error: {e.InnerException.Message}");
                    Logger.Log(LogType.Debug, $"Inner Stack: {e.InnerException.StackTrace}");
                }
            }
        }

        public static void RunMainTasks() {
            foreach (KeyValuePair<string, TaskItem> taskItem in Tasks) {
                if ((DateTime.UtcNow - taskItem.Value.LastRun) < taskItem.Value.Interval)
                    continue;

                try {
                    taskItem.Value.Main();
                    taskItem.Value.LastRun = DateTime.UtcNow;
                } catch (Exception e) {
                    Logger.Log(LogType.Error, $"Error occurred running {taskItem.Key}: {e.Message}");
                    Logger.Log(LogType.Debug, $"Stacktrace: {e.StackTrace}");
                    taskItem.Value.LastRun = DateTime.UtcNow;
                }
            }
        }

        public static void RunTeardownTasks() {
            foreach (KeyValuePair<string, TaskItem> taskItem in Tasks) {
                try {
                    taskItem.Value.Teardown();
                } catch (Exception e) {
                    Logger.Log(LogType.Error, $"Error occurred tearing down {taskItem.Key}: {e.Message}");
                    Logger.Log(LogType.Debug, $"Stacktrace: {e.StackTrace}");
                }
            }
        }
    }
}
