﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RohBot
{
    public abstract class Packet
    {
        public abstract string Type { get; }
        public abstract void Handle(Connection connection);

        #region Static
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
            var obj = JObject.Parse(msg);
            var type = PacketTypes[obj["Type"].ToObject<string>()];
            return (Packet)obj.ToObject(type);
        }
        #endregion
    }
}
