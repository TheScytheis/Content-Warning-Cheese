using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TestUnityPlugin
{
    internal class LobbiesWindow : MonoBehaviour
    {
        /*public static LobbiesWindow Instance { get; private set; }
        public bool DisplayingLobbyWindow = false;
        public Rect lobbyWindowRect = new Rect(0, 0, 600, 650);
        private readonly int title_height = 17;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnGUI()
        {
            Debug.Log("LOBBIEWSSSSS");
            if (!DisplayingLobbyWindow)
                return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            GUI.backgroundColor = Color.black;
            lobbyWindowRect = GUI.Window(24, lobbyWindowRect, LobbyWindowFunc, "The Game Lobbies!");
        }

        private void LobbyWindowFunc(int winId)
        {
            GUI.DragWindow(new Rect(0, 0, lobbyWindowRect.width, title_height));
            int x = 0, y = 0;
            GUILayout.BeginArea(new Rect(x, y + (title_height + 35), lobbyWindowRect.width, lobbyWindowRect.height - y - (title_height + 35)));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("LOBBY SERVERS");
            if (lobbymanager.Count > 0)
            {
                Debug.Log("WE GOT LOBBIES!!!! " + LobbyManager.Instance.lobbyList.Count);
                foreach (var room in LobbyManager.Instance.lobbyList)
                {
                    if (room.IsValid())
                    {
                        var pLimit = SteamMatchmaking.GetLobbyMemberLimit(room);
                        var roomId = SteamMatchmaking.GetLobbyData(room, "Photon");
                        var m_players = SteamMatchmaking.GetNumLobbyMembers(room);
                        var m_region = SteamMatchmaking.GetLobbyData(room, "PhotonRegion");
                        if (GUILayout.Button($"Room ID: {roomId.Remove(5)} | Region: {m_region.ToUpper()} | Players: {m_players}/{pLimit}"))
                        {
                            MethodInfo methodInfo = typeof(SteamLobbyHandler).GetMethod("JoinLobby", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(MainMenuHandler.SteamLobbyHandler, new object[] { room });
                            }
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private float timer = 0f;
        private float interval = 1.0f; // 1 second interval
        void Update()
        {
            // Timer check
            timer += Time.deltaTime;
            if (DisplayingLobbyWindow && timer >= interval)
            {
                Debug.Log("REQUESTING STEAM LOBBY LISTSSSS");
                SteamMatchmaking.RequestLobbyList();
                timer = 0f;
            }
        }*/
    }
}
