using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace SteamMobile
{
    public abstract class Packet
    {
        public abstract string Type { get; }


        private static readonly Dictionary<string, Type> PacketTypes;

        static Packet()
        {
            PacketTypes = new Dictionary<string, Type>();

            var assembly = Assembly.GetCallingAssembly();
            var types = assembly.GetExportedTypes().Where(type => type.IsSubclassOf(typeof(Packet)));

            foreach (var type in types)
            {
                var instance = (Packet)Activator.CreateInstance(type);
                PacketTypes[instance.Type] = type;
            }
        }

        public static string WriteToMessage(Packet packet)
        {
            return JsonConvert.SerializeObject(packet);
        }

        public static Packet ReadFromMessage(string msg)
        {
            // fuck the police
            var type = PacketTypes[(string)JsonConvert.DeserializeObject<dynamic>(msg).Type];
            return (Packet)JsonConvert.DeserializeObject(msg, type);
        }
    }
}
