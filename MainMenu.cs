using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.PropertyVariants;
using UnityEngine.UI;
using Zorro.Core;

namespace TestUnityPlugin
{
    internal class MainMenu : MonoBehaviour
    {
        public Button joinFriendBtn;
        public Button joinRandomBtn;
        public Button viewLobbiesBtn;

        private Transform page;
        private Canvas canvas;
        private MainMenuMainPage main;
        public static GameObject btnToCopy;

        private void Awake()
        {
            canvas = FindObjectOfType<Canvas>();
            page = GameObject.Find("MainPage").transform;
            main = FindObjectOfType<MainMenuMainPage>();
            btnToCopy = main.quitButton.gameObject;

            CreateButtons();
            joinFriendBtn.onClick.AddListener(new UnityAction(this.OnJoinFriendButtonClicked));
            joinRandomBtn.onClick.AddListener(new UnityAction(this.OnJoinRandomButtonClicked));
            viewLobbiesBtn.onClick.AddListener(new UnityAction(this.OnViewLobbiesButtonClicked));

        }

        private void Start()
        {
            joinFriendBtn.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "Join Friend";
        }

        private void CreateButtons()
        {
            viewLobbiesBtn = CreateBtnCopy("viewLobbiesBtn", "View Lobbies", 450, page).GetComponent<Button>();
            joinFriendBtn = CreateBtnCopy("JoinFriendBtn", "Join Friend", 375, page).GetComponent<Button>();
            joinRandomBtn = CreateBtnCopy("joinRandomBtn", "Join Random Private", 300, page).GetComponent<Button>();
        }

        public static GameObject CreateBtnCopy(string name, string btnText, float y, Transform parent)
        {
            GameObject go = Instantiate(btnToCopy, parent);
            Destroy(go.GetComponentInChildren<GameObjectLocalizer>());

            go.name = name;
            RectTransform rt = go.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(-325, y);
            rt.anchorMax = new Vector2(1, 0);
            rt.anchorMin = new Vector2(1, 0);
            rt.sizeDelta = new Vector2(500f, 50);
            rt.localScale = new Vector3(1, 1, 1);
            rt.rotation = Quaternion.identity;

            go.GetComponentInChildren<TextMeshProUGUI>().text = btnText;

            return go;
        }

        private void OnJoinFriendButtonClicked()
        {
            List<CSteamID> lobbies = new List<CSteamID>();

            for (int i = 0; i < SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate); i++)
            {
                CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                SteamFriends.GetFriendGamePlayed(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagAll), out var info);

                if (info.m_gameID.m_GameID == 2881650 && info.m_steamIDLobby.IsValid())
                    lobbies.Add(info.m_steamIDLobby);
            }

            if (lobbies.Count == 0)
            {
                Modal.ShowError("No Friend Lobbies Found!", "Failed to find any valid joinable friend lobbies. Please try again.");
                return;
            }

            MainMenuHandler.SteamLobbyHandler.Reflect().Invoke("JoinLobby", true, lobbies[Random.Range(0, lobbies.Count)]);
        }

        private void OnJoinRandomButtonClicked() => HelloWorld.Instance.StartCoroutine(HelloWorld.JoinRandomPrivateGame());
        private void OnViewLobbiesButtonClicked()
        {
            //Modal.ShowError("Feature Coming Soon!", "This feature is currently in development and will be available in a future update.");
            MyPageUI.TransitionToPage<MainMenuViewLobbiesPage>();
        }

        public static void JoinRandomPriv()
        {
            HelloWorld.Instance.StartCoroutine(HelloWorld.JoinRandomPrivateGame());
        }

        
    }
}
