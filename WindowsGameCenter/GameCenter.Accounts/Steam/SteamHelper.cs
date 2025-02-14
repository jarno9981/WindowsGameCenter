using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCenter.Accounts.Steam
{
    public static class SteamHelper
    {
        public static bool IsLoggedOn()
        {
            return SteamAPI.IsSteamRunning() && SteamUser.BLoggedOn();
        }

        public static CSteamID GetSteamID()
        {
            return SteamUser.GetSteamID();
        }

        public static string GetPersonaName()
        {
            return SteamFriends.GetPersonaName();
        }
    }
}
