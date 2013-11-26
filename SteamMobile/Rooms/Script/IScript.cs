using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SteamMobile.Rooms.Script
{
    public interface IScript
    {
        void Initialize(ScriptHost host);
        void Update(float deltaTime);
    }
}
