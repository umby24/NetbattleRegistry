using System;
using System.Collections.Generic;
using System.Linq;

namespace Netbattle_Registry.Common {
    public static class ServerDatabase {

        public static List<ServerRegistration> Get(string name) {
            return Configuration.Settings.Registrations.Where(a => String.Equals(a.Name, name, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        public static void Update(ServerRegistration registration) {
            if (!Configuration.Settings.Registrations.Any(a =>
                String.Equals(a.Name, registration.Name, StringComparison.CurrentCultureIgnoreCase)))
                return;
            
            int index = Configuration.Settings.Registrations.IndexOf(Configuration.Settings.Registrations.FirstOrDefault(a =>
                String.Equals(a.Name, registration.Name, StringComparison.CurrentCultureIgnoreCase)));

            Configuration.Settings.Registrations[index] = registration;
            Configuration.Settings.Save();
        
        }
    }
}
