using Discord;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zorro.Core;
using Zorro.Settings;

namespace TestUnityPlugin
{
    public enum LobbyCategory
    {
        us,
        usw,
        asia,
        sa,
        eu,
        all
    }
    internal class LobbyMenu : Singleton<LobbyMenu>
    {
        public CW_TAB firstTab;

        public CW_TABS categoryTabs;

        public Transform m_lobbiesContainer;

        private GameObject layoutGroupGO;

        private GameObject svGO;


        public static LobbyCategory currentCategory = LobbyCategory.all;

        //Buttons for each category
        public static Button usBtn;

        public static Button uswBtn;

        public static Button asiaBtn;

        public static Button euBtn;

        public static Button saBtn;

        public static Button allBtn;

        private ScrollRect scrollRect;

        private void Start()
        {
            svGO = CreateScrollableVerticalLayoutGroup();
        }

        private void OnEnable()
        {
            //categoryTabs.Select(firstTab);
            Show(currentCategory);
        }
        private Dictionary<string, Button> m_buttons = new Dictionary<string, Button>();
        private Dictionary<string, string> m_hosts = new Dictionary<string, string>();
        private void Show(LobbyCategory category)
        {
            var newLobbyList = new HashSet<string>();

            if (HelloWorld.lobbyManager.lobbyList.Count > 0)
            {
                foreach (var room in HelloWorld.lobbyManager.lobbyList)
                {
                    if (room.IsValid())
                    {
                        newLobbyList.Add(room.ToString());
                        if (!m_buttons.ContainsKey(room.ToString()))
                        {
                            // Create a new button for new lobbies
                            SteamMatchmaking.JoinLobby(room);
                            var reflector = new ReflectionUtil<SteamLobbyHandler>(MainMenuHandler.SteamLobbyHandler);
                            var m_isJoining = (bool)reflector.GetValue("m_isJoining");
                            if (m_isJoining)
                                continue;
                            var pLimit = SteamMatchmaking.GetLobbyMemberLimit(room);
                            var m_players = SteamMatchmaking.GetNumLobbyMembers(room);
                            var m_region = SteamMatchmaking.GetLobbyData(room, "PhotonRegion");
                            var m_hostID = SteamMatchmaking.GetLobbyOwner(room);
                            var m_host = SteamFriends.GetFriendPersonaName(m_hostID);
                            m_host = m_host.Substring(0, Math.Min(8, m_host.Length));

                            if (currentCategory.ToString() != "all" && m_region != currentCategory.ToString())
                                continue;
                            var title = $"Host: {m_host} | {m_region.ToUpper()} | Players: {m_players}/{pLimit}";
                            if (m_host == "") continue;
                            var component = MainMenu.CreateBtnCopy($"{room.ToString()}", title, 100, layoutGroupGO.transform).GetComponent<Button>();
                            component.onClick.AddListener(() => OnServerButtonClicked(room));
                            m_buttons.Add(room.ToString(), component);
                            m_hosts.Add(room.ToString(), m_host);
                            SteamMatchmaking.LeaveLobby(room);
                        }else
                        {
                            var pLimit = SteamMatchmaking.GetLobbyMemberLimit(room);
                            var m_players = SteamMatchmaking.GetNumLobbyMembers(room);
                            var m_region = SteamMatchmaking.GetLobbyData(room, "PhotonRegion");
                            var m_host = m_hosts[room.ToString()];
                            var title = $"Host: {m_host} | {m_region.ToUpper()} | Players: {m_players}/{pLimit}";
                            m_buttons[room.ToString()].GetComponentInChildren<TextMeshProUGUI>().text = title;
                        }
                    }
                }
            }

            /*
             So vai criar e add botão se m_host for != ""
             
             */

            // Remove buttons for lobbies that no longer exist
            var keysToRemove = new List<string>();
            foreach (var existingKey in m_buttons.Keys)
            {
                if (!newLobbyList.Contains(existingKey))
                {
                    Destroy(m_buttons[existingKey].gameObject);
                    keysToRemove.Add(existingKey);
                }
            }

            foreach (var key in keysToRemove)
            {
                m_buttons.Remove(key);
                m_hosts.Remove(key);
            }
        }

        private void OnServerButtonClicked(CSteamID room)
        {
            Debug.LogError("Attempting to join lobby through LOBBY VIEWER!");
            MethodInfo methodInfo = typeof(SteamLobbyHandler).GetMethod("JoinLobby", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(MainMenuHandler.SteamLobbyHandler, new object[] { room });
                Debug.LogError("Method passed through LOBBY VIEWER!");

            }
        }

        public void SelectCategory(LobbyCategory lobbyCategory)
        {
            currentCategory = lobbyCategory;
        }
        private float timer = 0;
        private float interval = .5f;
        void Update()
        {
            SetupBtnNames();
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                Show(currentCategory);
                timer = 0;
            }
        }

        private void SetupBtnNames()
        {
            usBtn.GetComponentInChildren<TextMeshProUGUI>().text = "US " + " (" + HelloWorld.lobbyManager.usLobbies.Count + ")";
            uswBtn.GetComponentInChildren<TextMeshProUGUI>().text = "USW " + " (" + HelloWorld.lobbyManager.uswLobbies.Count + ")";
            euBtn.GetComponentInChildren<TextMeshProUGUI>().text = "EU " + " (" + HelloWorld.lobbyManager.euLobbies.Count + ")";
            saBtn.GetComponentInChildren<TextMeshProUGUI>().text = "SA " + " (" + HelloWorld.lobbyManager.saLobbies.Count + ")";
            asiaBtn.GetComponentInChildren<TextMeshProUGUI>().text = "ASIA" + " (" + HelloWorld.lobbyManager.asiaLobbies.Count + ")";
            allBtn.GetComponentInChildren<TextMeshProUGUI>().text = "ALL" + " (" + HelloWorld.lobbyManager.lobbyList.Count + ")";
        }

        public void OnScroll(PointerEventData data)
        {
            if (scrollRect != null)
            {
                scrollRect.OnScroll(data);
            }
        }

        private GameObject CreateScrollableVerticalLayoutGroup()
        {
            // Create the scroll view object and set up its RectTransform
            GameObject scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(this.transform, false);
            RectTransform scrollViewRect = scrollViewGO.AddComponent<RectTransform>();
            scrollViewRect.sizeDelta = new Vector2(800, 600); // Adjust size as needed
            scrollViewRect.anchorMin = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchorMax = new Vector2(0.5f, 0.5f);
            scrollViewRect.pivot = new Vector2(0.5f, 0.5f);
            scrollViewRect.anchoredPosition = Vector2.zero;

            //Add a Transp. BG with raycasting for scrolling
            Image contentImage = scrollViewGO.AddComponent<Image>();
            contentImage.color = Color.clear; // Transparent
            contentImage.raycastTarget = true;

            //Add a Graphic Raycaster
            scrollViewGO.AddComponent<GraphicRaycaster>();

            // Add ScrollRect component
            scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; // Only vertical scrolling
            scrollRect.scrollSensitivity = 45;

            // Create the content panel to hold the vertical layout group
            layoutGroupGO = new GameObject("ContentPanel");
            layoutGroupGO.transform.SetParent(scrollViewGO.transform, false);
            RectTransform contentRect = layoutGroupGO.AddComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);

            // Set up the VerticalLayoutGroup in the content panel
            VerticalLayoutGroup layoutGroup = layoutGroupGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.spacing = 70;

            // Add Content Size Fitter to adjust content size dynamically
            ContentSizeFitter sizeFitter = layoutGroupGO.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Set the content panel as the content of the scroll view
            scrollRect.content = contentRect;

            scrollViewGO.transform.localPosition = new Vector3(scrollViewGO.transform.localPosition.x, scrollViewGO.transform.localPosition.y + 100f, scrollViewGO.transform.localPosition.z);
            return scrollViewGO;
        }
    }
}
