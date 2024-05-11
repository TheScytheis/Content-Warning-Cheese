using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Steamworks;
using Photon.Realtime;
using UnityEngine.Rendering.Universal;

namespace TestUnityPlugin
{
    public class LobbyManager : MonoBehaviour
    {
        private Callback<LobbyMatchList_t> lobbyMatchListCallback;
        public List<CSteamID> lobbyList = new List<CSteamID>();
        //Sort by region
        public List<CSteamID> usLobbies = new List<CSteamID>();
        public List<CSteamID> uswLobbies = new List<CSteamID>();
        public List<CSteamID> asiaLobbies = new List<CSteamID>();
        public List<CSteamID> saLobbies = new List<CSteamID>();
        public List<CSteamID> euLobbies = new List<CSteamID>();

        public void Start()
        {
            // steam init
            SteamAPI.Init();

            // callback to run the list handle when ready
            lobbyMatchListCallback = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);

            // refresh lobbies
            SteamMatchmaking.RequestLobbyList();
        }

        private void OnLobbyMatchList(LobbyMatchList_t callback)
        {
            lobbyList.Clear();
            usLobbies.Clear();
            uswLobbies.Clear();
            asiaLobbies.Clear();
            saLobbies.Clear();
            euLobbies.Clear();

            // adding found lobbies into list
            for (int i = 0; i < callback.m_nLobbiesMatching; i++)
            {
                var room = SteamMatchmaking.GetLobbyByIndex(i);
                lobbyList.Add(room);
                addRoomToRegionList(room);
            }
        }

        private void addRoomToRegionList(CSteamID room)
        {
            var region = SteamMatchmaking.GetLobbyData(room, "PhotonRegion");
            if (region == "us")
            {
                usLobbies.Add(room);
            }
            if(region == "usw"){
                uswLobbies.Add(room);
            }
            if (region == "asia")
            {
                asiaLobbies.Add(room);
            }
            if (region == "sa")
            {
                saLobbies.Add(room);
            }
            if (region == "eu")
            {
                euLobbies.Add(room);
            }
        }

        private void OnDestroy()
        {
            // close steamapi (never used, just for security measures)
            SteamAPI.Shutdown();
        }
    }
}
