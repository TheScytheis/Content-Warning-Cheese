using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using DefaultNamespace;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Zorro.Core;
using Zorro.UI;

namespace TestUnityPlugin
{
    class Win32
    {
        [DllImport("Shell32.dll")]
        public static extern int ShellExecuteA(IntPtr hwnd, StringBuilder lpszOp, StringBuilder lpszFile, StringBuilder lpszParams, StringBuilder lpszDir, int FsShowCmd);
    }
    [BepInPlugin("xiaodo.plugin.test.HelloWorld", "Hello, World!", "1.0")]
    public class HelloWorld : BaseUnityPlugin
    {
        void Start()
        {
            //Bypass Plugin Check
            Traverse.Create(GameHandler.Instance).Field("m_pluginHash").SetValue(null);
        }
        void OnGUI()
        {
            ESP.StartESP();
            //显示菜单
            if (!DisplayingWindow)
                return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            windowRect = GUI.Window(114519810, windowRect, WindowFunc, "Xiaodou - Content Warning: Free mods, no resale allowed!");
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Insert))
                DisplayingWindow = !DisplayingWindow;

            if (Input.GetKeyDown(KeyCode.F1) && PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                foreach (Player player in GameObject.FindObjectsOfType<Player>())
                {
                    if (player.IsLocal && !player.ai)
                    {
                        //PhotonNetwork.NetworkingClient.ChangeLocalID(PhotonNetwork.MasterClient.ActorNumber);
                        /*
                        byte id = 43;
                        player.refs.view.RPC("RPC_PlayEmote", RpcTarget.All, new object[] { id });
                        Traverse.Create(GameObject.FindObjectOfType<PlayerCustomizer>()).Field("view_g").GetValue<PhotonView>().RPC("RPCM_RequestEnterTerminal", RpcTarget.MasterClient, new object[]
                        {
                            player.refs.view.ViewID
                        });
                        ShadowRealmHandler.instance.TeleportPlayerToRandomRealm(player);
                        */
                    }
                }
            }

            Players.Run();
            Items.Run();
            Monsters.Run();
            Misc.Run();
        }
        public bool DisplayingWindow = false;
        public Rect windowRect = new Rect(0, 0, 400, 550);
        private readonly int title_height = 17;
        private Vector2 scrollPosition = Vector2.zero;
        private void WindowFunc(int winId)
        {
            // Draggable window
            GUI.DragWindow(new Rect(0, 0, windowRect.width, title_height));

            // Toolbar
            GUILayout.BeginArea(new Rect(0, title_height + 5, windowRect.width, 30));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Players"))
                Hack.GUILabel = Hack.Label.Players;
            if (GUILayout.Button("Items"))
                Hack.GUILabel = Hack.Label.Items;
            if (GUILayout.Button("Monsters"))
                Hack.GUILabel = Hack.Label.Monsters;
            if (GUILayout.Button("ESP"))
                Hack.GUILabel = Hack.Label.ESP;
            if (GUILayout.Button("Misc"))
                Hack.GUILabel = Hack.Label.Misc;
            GUILayout.EndHorizontal();
            CenterLabel("This is Holden's beautiful translation");
            GUILayout.EndArea();

            if (Hack.GUILabel == Hack.Label.Players)
            {
                int x = 0, y = 0;
                GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));
                GUILayout.BeginHorizontal();
                Players.InfinityHealth = GUILayout.Toggle(Players.InfinityHealth, "Unlimited Health");
                Players.InfinityStamina = GUILayout.Toggle(Players.InfinityStamina, "Unlimited Stamina");
                Players.InfinityOxy = GUILayout.Toggle(Players.InfinityOxy, "Unlimited Oxygen");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                Players.InfinityJump = GUILayout.Toggle(Players.InfinityJump, "No Jump Interval");
                Players.NeverFalldown = GUILayout.Toggle(Players.NeverFalldown, "Never Fall Down");
                Players.NeverDie = GUILayout.Toggle(Players.NeverDie, "Never Die");
                GUILayout.EndHorizontal();
                CenterLabel($"Running Speed Multiplier: {Math.Round(Players.SprintMultipiler, 2)}x");
                Players.SprintMultipiler = GUILayout.HorizontalSlider(Players.SprintMultipiler, 1f, 20f);
                CenterLabel($"Jump Height Multiplier: {Math.Round(Players.JumpHeightMultipiler, 2)}x");
                Players.JumpHeightMultipiler = GUILayout.HorizontalSlider(Players.JumpHeightMultipiler, 1f, 20f);
                if (Player.localPlayer)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Revive"))
                        Players.Respawn(true);
                    if (GUILayout.Button("Suicide"))
                        Players.Die(true);
                    if (GUILayout.Button("Cum"))
                        Players.Cum(true);
                    if (GUILayout.Button("Fall Down"))
                        Players.Falldown(true);
                    if (GUILayout.Button("Enter Realm"))
                        Players.JoinRealm(true);
                    if (GUILayout.Button("Exit Realm"))
                        Players.RemoveRealm(true);
                    GUILayout.EndHorizontal();
                }
                if(Players.InGame.Count > 0)
                {
                    CenterLabel("Execute Object");
                    GUILayout.BeginHorizontal();
                    foreach (var playerkvp in new Dictionary<Player, bool>(Players.InGame)) // // Iterate over a copied collection to avoid errors, 不然会报错
                    {
                        Players.InGame[playerkvp.Key] = GUILayout.Toggle(Players.InGame[playerkvp.Key], playerkvp.Key.refs.view.Owner.NickName);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Revive"))
                        Players.Respawn();
                    if (GUILayout.Button("Kill"))
                        Players.Die();
                    if (GUILayout.Button("Cum"))
                        Players.Cum();
                    if (GUILayout.Button("Fall Down"))
                        Players.Falldown();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Bouncing Bomb"))
                        Players.Explode();
                    if (GUILayout.Button("Clap"))
                        Players.PlayEmote(43);
                    if (GUILayout.Button("Flip Off"))
                        Players.PlayEmote(52);
                    if (GUILayout.Button("Push-Up"))
                        Players.PlayEmote(54);
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fly Towards Oneself"))
                        Players.DragToLocal();
                    if (GameObject.FindObjectOfType<PlayerCustomizer>() && Players.GetChoosedPlayerCount() == 1 && GUILayout.Button("强制进入终端"))
                        Players.ForceEnterTerminal();
                    if (GUILayout.Button("Enter Realm"))
                        Players.JoinRealm();
                    if (GUILayout.Button("Exit Realm"))
                        Players.RemoveRealm();
                    GUILayout.EndHorizontal();
                }

                CenterLabel("Strongly condemn a certain domestic fool who makes money with open-source GitHub code");
                GUILayout.EndArea();
            }
            if (Hack.GUILabel == Hack.Label.Items)
            {
                int x = 0, y = 0;
                GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));

                CenterLabel("Generation Method");
                Items.SpawnMethod = (Items.SpawnType)GUILayout.Toolbar((int)Items.SpawnMethod, new string[] { "Add to Inventory", "Create in Front", "Call Drone to Spawn" });
                scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                GUILayout.BeginHorizontal();
                Items.PartyMode = GUILayout.Toggle(Items.PartyMode, "Unlimited Party Fireworks");
                Items.InfinityPower = GUILayout.Toggle(Items.InfinityPower, "Unlimited Electricity");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                Items.BlockBombSpawn = GUILayout.Toggle(Items.BlockBombSpawn, "Prevent Bomb Spawn");
                Items.NoGrabberLimit = GUILayout.Toggle(Items.NoGrabberLimit, "No Grabber Limit");
                GUILayout.EndHorizontal();
                if (Player.localPlayer)
                {
                    CenterLabel("Item Operations");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Clear All Items"))
                        Items.DestoryDropedItems();
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Generate All Emote Books"))
                        Items.SpawnEmoteBooks();
                    if (GUILayout.Button("Generate All Items"))
                        Items.SpawnAllItems();
                    GUILayout.EndHorizontal();

                    int count = 0;
                    //灯光
                    CenterLabel("Target Players");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Lights])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }

                    // Medical Equipment
                    CenterLabel("Medical Equipment");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Medicals])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }

                    //Tools
                    CenterLabel("Tools");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Tools])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }

                    /// Emotes
                    CenterLabel("Emotes");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Emotes])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }

                    // Miscellaneous
                    CenterLabel("Miscellaneous");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Miscs])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }

                    // Other
                    CenterLabel("Other");
                    foreach (KeyValuePair<byte, string> item in Items.ItemsTypeList[Items.ItemType.Others])
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(item.Value))
                        {
                            switch (Items.SpawnMethod)
                            {
                                case Items.SpawnType.AddToInventory:
                                    Items.SpawnItem(item.Key);
                                    break;

                                case Items.SpawnType.CreatePickup:
                                    Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 1.5f));
                                    break;

                                case Items.SpawnType.CallDrone:
                                    Items.SpawnItem(new byte[] { item.Key });
                                    break;
                            }
                        }

                        count++;

                        if (count >= 3)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }
                    if (count > 0)
                    {
                        GUILayout.EndHorizontal();
                        count = 0;
                    }
                }
                CenterLabel("Strong condemnation of those domestically monetizing GitHub");
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            if (Hack.GUILabel == Hack.Label.Monsters)
            {
                int x = 0, y = 0;
                GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));
                if (Player.localPlayer)
                {
                    CenterLabel("Functions");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Remove"))
                        Monsters.KillAll();
                    if (GUILayout.Button("Kill AI"))
                        Monsters.Die();
                    if (GUILayout.Button("Spawn Bomb"))
                        Monsters.Explode();
                    if (GUILayout.Button("Fall Down"))
                        Monsters.Falldown();
                    if (GUILayout.Button("Spray"))
                        Monsters.Cum();
                    if (GUILayout.Button("Drag Nearby"))
                        Monsters.DragToLocal();
                    GUILayout.EndHorizontal();
                    Monsters.AutoRemoveLocalMonsters = GUILayout.Toggle(Monsters.AutoRemoveLocalMonsters, "Automatically Remove Local Monsters");
                    if (GUILayout.Button("Make Dogs Bark"))
                    {
                        Monsters.MakeMouthesScream();
                    }
                    if (GUILayout.Button("Clear All Monsters"))
                    {
                        Monsters.ClearAllMonsters();
                    }

                    CenterLabel("Spawn");
                    int count = 0;
                    foreach (string name in Monsters.MonsterNames)
                    {
                        if (count == 0)
                            GUILayout.BeginHorizontal();

                        if (GUILayout.Button(name))
                            Monsters.SpawnMonster(name);

                        count++;

                        if (count >= 2)
                        {
                            GUILayout.EndHorizontal();
                            count = 0;
                        }
                    }

                    if (count > 0)
                        GUILayout.EndHorizontal();
                }
                else CenterLabel("This function can only be used in-game!");
                CenterLabel("Strong condemnation of those domestically monetizing GitHub open-source code");
                GUILayout.EndArea();
            }
            if (Hack.GUILabel == Hack.Label.ESP)
            {
                int x = 0, y = 0;
                GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));
                GUILayout.BeginHorizontal();
                ESP.EnablePlayerESP = GUILayout.Toggle(ESP.EnablePlayerESP, "Players");
                ESP.EnableMonsterESP = GUILayout.Toggle(ESP.EnableMonsterESP, "Monsters");
                ESP.EnableItemESP = GUILayout.Toggle(ESP.EnableItemESP, "Items");
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                ESP.EnableDrawLine = GUILayout.Toggle(ESP.EnableDrawLine, "Line");
                ESP.EnableDrawString = GUILayout.Toggle(ESP.EnableDrawString, "Name");
                ESP.EnableDistance = GUILayout.Toggle(ESP.EnableDistance, "Distance");
                GUILayout.EndHorizontal();
                CenterLabel("Strong condemnation of those domestically monetizing GitHub open-source code");
                GUILayout.EndArea();
            }
            if (Hack.GUILabel == Hack.Label.Misc)
            {
                int x = 0, y = 0;
                GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));

                CenterLabel("Lobby");
                GUILayout.BeginHorizontal();
                Misc.AutoJoinRandom = GUILayout.Toggle(Misc.AutoJoinRandom, "Auto Quick Play");
                Misc.ForceJoinOthersRoom = GUILayout.Toggle(Misc.ForceJoinOthersRoom, "Force Join Others' Room");
                GUILayout.EndHorizontal();
                Misc.DisableAutoJoinRandomWhenJoined = GUILayout.Toggle(Misc.DisableAutoJoinRandomWhenJoined, "Disable Auto Quick Play After Joining");
                if (GUILayout.Button("Create Public Multiplayer Room") && MainMenuHandler.Instance)
                    MainMenuHandler.Instance.SilentHost();

                CenterLabel("Miscellaneous");
                if (GUILayout.Button("Dump Items List To Console"))
                    Items.DumpItemsToConsole();

                if (GUILayout.Button("Open GitHub Project Link"))
                    Win32.ShellExecuteA(IntPtr.Zero, new StringBuilder("open"), new StringBuilder(@"https://github.com/xiaodo1337/Content-Warning-Cheat"), new StringBuilder(), new StringBuilder(), 0);
                CenterLabel("Strong condemnation of those domestically monetizing GitHub open-source code");
                GUILayout.EndArea();
            }
        }
        void CenterLabel(string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
    class Hack
    {
        public static Label GUILabel = Label.Misc;
        public enum Label
        {
            Items = 0,
            Monsters,
            ESP,
            Misc,
            Players
        }
    }
}
