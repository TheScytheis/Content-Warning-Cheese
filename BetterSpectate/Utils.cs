using System;
using Photon.Realtime;
using UnityEngine;

namespace TestUnityPlugin.BetterSpectate
{
    internal class Utils
    {
        public static Sprite GetAvatar(Player plr)
        {
            Sprite result;
            SteamAvatarHandler.TryGetAvatarForPlayer(plr.refs.view.Owner, out result);
            return result;
        }
    }
}
