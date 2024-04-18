using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Zorro.Core.CLI;
using Zorro.PhotonUtility;
using Zorro.UI;

namespace TestUnityPlugin
{
    class Win32
    {
        [DllImport("Shell32.dll")]
        public static extern int ShellExecuteA(IntPtr hwnd, StringBuilder lpszOp, StringBuilder lpszFile, StringBuilder lpszParams, StringBuilder lpszDir, int FsShowCmd);
    }
    [BepInPlugin("holden.plugin.test.HelloWorld", "Hello, World!", "1.0")]
    public class HelloWorld : BaseUnityPlugin
    {
        public static VideoHandle lastVideoID;
        public static ClipID lastClipID;
        private Player selectedPlayer;
        //Troll Video Dropdown
        private bool isFolderDropdownVisible = false;
        private Vector2 folderScrollPosition;
        private string folderButtonText = "Select Video";
        private int selectedFolderIndex = -1;
        private string selectedVideo;

        public static void ApplyPatches()
        {
            Debug.Log("Trying to apply patches.");
            var harmony = new Harmony("com.holden.codewarning");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("Harmony patches applied.");
        }

        [HarmonyPatch(typeof(RecordingsHandler), "StartRecording")]
        public static class StartRecordingPatch
        {
            private static MethodInfo FindFreePlayerMethod;
            private static MethodInfo StopRecordingMethod;
            private static FieldInfo PlayersRecordingField;

            static StartRecordingPatch()
            {
                // Using reflection to access private/internal methods and fields
                Type recordingHandlerType = typeof(RecordingsHandler);

                FindFreePlayerMethod = recordingHandlerType.GetMethod("FindFreePlayer", BindingFlags.NonPublic | BindingFlags.Static);
                StopRecordingMethod = recordingHandlerType.GetMethod("StopRecording", BindingFlags.NonPublic | BindingFlags.Static);
                PlayersRecordingField = recordingHandlerType.GetField("m_playersRecording", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            [HarmonyPrefix]
            public static bool Prefix(ItemInstanceData data, PhotonView playerView)
            {
                if (!data.TryGetEntry<VideoInfoEntry>(out var t))
                {
                    Debug.LogError("No VideoInfoEntry found in instance data");
                    return false;
                }

                if (t.videoID.Equals(VideoHandle.Invalid))
                {
                    t.videoID = new VideoHandle(Guid.NewGuid());
                    Debug.Log($"Camera has no video ID, creating new video ID, {t.videoID}");
                }

                t.isRecording = true;
                t.SetDirty();

                int num = (playerView == null) ? (int)FindFreePlayerMethod.Invoke(null, null) : playerView.OwnerActorNr;
                if (num == -1)
                {
                    Debug.LogError("No free player found to record camera...");
                    return false;
                }

                var playersRecording = (BidirectionalDictionary<int, VideoHandle>)PlayersRecordingField.GetValue(RecordingsHandler.Instance);
                if (playersRecording.ContainsKey(num))
                {
                    VideoHandle fromKey = playersRecording.GetFromKey(num);
                    var camerasCurrentRecording = RecordingsHandler.GetCamerasCurrentRecording();
                    if (camerasCurrentRecording.Contains(fromKey))
                    {
                        ItemInstanceData o;
                        if (ItemInstanceDataHandler.TryGetInstanceData(camerasCurrentRecording.Get(fromKey), out o))
                        {
                            StopRecordingMethod.Invoke(null, new object[] { o });
                            RecordingsHandler.StartRecording(o, null);
                        }
                    }
                }

                // Create the command package with a new ClipID
                var newClipID = new ClipID(Guid.NewGuid());
                lastClipID = newClipID;
                lastVideoID = t.videoID;

                CustomCommands<CustomCommandType>.SendPackage(new StartRecordingCommandPackage
                {
                    CameraDataGuid = data.m_guid,
                    VideoID = t.videoID,
                    ClipID = newClipID,
                    CameraOwner = num
                }, ReceiverGroup.All);

                return false; // Prevent the original method from executing
            }
        }
        void Start()
        {
            //Bypass Plugin Check
            Traverse.Create(GameHandler.Instance).Field("m_pluginHash").SetValue(null);
            ApplyPatches();
        }
        void OnGUI()
        {
            ESP.StartESP();
            //显示菜单
            if (!DisplayingWindow)
                return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
            windowRect = GUI.Window(114519810, windowRect, WindowFunc, "Holden's Content Warning MOD!");
        }

        private GameObject lastDetectedObject = null;
        public void SelectPlayer()
        {
            if (!Input.anyKey || !Input.anyKeyDown)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                // Cast a ray from the center of the screen (camera)
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
                RaycastHit hit;

                // Check if the ray hits any object within maxDistance
                if (Physics.Raycast(ray, out hit, 1000))
                {
                    // Check if the hit object is different from the last detected object
                    if (hit.collider.gameObject != lastDetectedObject)
                    {
                        // Update the last detected object
                        lastDetectedObject = hit.collider.gameObject;
                        if (lastDetectedObject.GetComponentInParent<Player>() != null)
                        {
                            selectedPlayer = lastDetectedObject.GetComponentInParent<Player>();

                            //Ativar som
                            MethodInfo methodInfo = typeof(Player).GetMethod("CallMakeSound", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(selectedPlayer, new object[] { 0 });
                            }
                            HelmetText.Instance.SetHelmetText("Player Selected: " + selectedPlayer.refs.view.Controller.ToString(), 2.5f);
                        }
                        else
                        {
                            selectedPlayer = null;
                            //Ativar som
                            MethodInfo methodInfo = typeof(Player).GetMethod("CallMakeSound", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(Player.localPlayer, new object[] { 2 });
                            }
                            HelmetText.Instance.SetHelmetText("Deselected", 2.5f);
                        }

                    }
                }
                else
                {
                    lastDetectedObject = null;
                }
                lastDetectedObject = null;
            }
        }
        public void MurderBind()
        {
            if (!Input.anyKey || !Input.anyKeyDown)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (selectedPlayer != null && selectedPlayer.GetComponent<Player>() != null)
                {
                    MethodInfo methodInfo = typeof(Player).GetMethod("CallDie", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(selectedPlayer, new object[] { });
                    }
                }
            }
        }

        //pegar nome do player player.refs.view.controller.name

        public void ShadowRealmBind()
        {
            if (!Input.anyKey || !Input.anyKeyDown)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                Player playerComponent = selectedPlayer.GetComponent<Player>();
                if (selectedPlayer != null && playerComponent != null)
                {
                    if (!playerComponent.data.playerIsInRealm)
                    {
                        ShadowRealmHandler.instance.TeleportPlayerToRandomRealm(playerComponent);
                    }
                    else
                    {
                        // Access the private field 'currentRealms'
                        FieldInfo fieldInfo = typeof(ShadowRealmHandler).GetField("currentRealms", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            GameObject[] currentRealms = (GameObject[])fieldInfo.GetValue(ShadowRealmHandler.instance);

                            // Find the realm the player is currently in
                            foreach (GameObject realm in currentRealms)
                            {
                                if (realm != null && realm.GetComponentInChildren<RealmGateTrigger>().playerInRealm == playerComponent)
                                {
                                    // Access the private method 'PlayerLeaveRealm'
                                    MethodInfo methodInfo = typeof(ShadowRealmHandler).GetMethod("PlayerLeaveRealm", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (methodInfo != null)
                                    {
                                        methodInfo.Invoke(ShadowRealmHandler.instance, new object[] { playerComponent, realm });
                                        break; // Exit after the correct realm is found and the method is invoked
                                    }
                                    else
                                    {
                                        Debug.LogError("Method 'PlayerLeaveRealm' not found.");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public float forceMagnitude = 10f;
        public void ForceThrow()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            // Check if the scroll wheel is scrolled up
            if (scrollInput > 0f)
            {
                StartCoroutine(ForceThrowCR(forceMagnitude));
            }
            // Check if the scroll wheel is scrolled down
            else if (scrollInput < 0f)
            {
                // Invert the force magnitude for scrolling down
                float invertedForceMagnitude = -forceMagnitude;
                StartCoroutine(ForceThrowCR(invertedForceMagnitude));
            }
        }

        private IEnumerator ForceThrowCR(float forceMagnitude)
        {
            // Get the camera's forward direction
            Vector3 cameraForward = Camera.main.transform.forward;

            // Calculate the force vector in the camera's forward direction
            Vector3 force = cameraForward * forceMagnitude;

            // Call the RPC function to apply the force
            MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(selectedPlayer, new object[] { 0f, force, 2.5f });
            }
            yield return new WaitForSeconds(0.2f);
        }

        public void Revive()
        {
            if (!Input.anyKey || !Input.anyKeyDown)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (selectedPlayer != null && selectedPlayer.GetComponent<Player>() != null)
                {
                    selectedPlayer.CallRevive();
                }
            }
        }
        private bool isCoroutineRunning = false;
        private bool isApplyingForce = false;
        public void FakeTP()
        {
            if (Input.GetMouseButton(2))
            {
                if (selectedPlayer != null && selectedPlayer.GetComponent<Player>() != null)
                {
                    if (!isApplyingForce)
                    {
                        isApplyingForce = true;
                        StartCoroutine(ApplyForceCoroutine());
                    }
                }
            }
            else
            {
                isApplyingForce = false;
            }
        }

        private IEnumerator ApplyForceCoroutine()
        {
            FieldInfo bpListField = typeof(PlayerRagdoll).GetField("bodypartList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (bpListField != null)
            {
                List<Bodypart> bpList = (List<Bodypart>)bpListField.GetValue(selectedPlayer.GetComponent<PlayerRagdoll>());
                while (isApplyingForce)
                {
                    foreach (Bodypart bp in bpList)
                    {
                        FieldInfo rbField = typeof(Bodypart).GetField("rig", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (rbField != null)
                        {
                            Rigidbody rb = (Rigidbody)rbField.GetValue(bp);
                            if (bp != null && bp.bodypartType == BodypartType.Torso)
                            {
                                Vector3 totalForce = -rb.velocity - rb.angularVelocity;

                                MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (methodInfo != null)
                                {
                                    methodInfo.Invoke(selectedPlayer, new object[] { 0f, totalForce, 0f });
                                }
                                else
                                {
                                    Debug.LogError("Method 'CallTakeDamageAndAddForceAndFall' not found.");
                                }

                                break;
                            }
                        }
                    }

                    yield return new WaitForFixedUpdate();
                }
            }
        }

        public void TrollFilm()
        {
            if (!Input.anyKey || !Input.anyKeyDown)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Debug.Log($"Starting troll script with VideoHandle: {lastVideoID.ToString()} and ClipID: {lastClipID.ToString()}");
                // Find all objects of type VideoCamera in the scene
                VideoCamera[] allVideoCameras = UnityEngine.Object.FindObjectsOfType<VideoCamera>(true); // true to include inactive objects
                VideoCamera videoCamera;
                // Iterate through all found VideoCamera objects
                foreach (VideoCamera aVideoCamera in allVideoCameras)
                {
                    // Check if the videoCamera is held by the current player
                    if (aVideoCamera.isHeldByMe)
                    {
                        // Log or handle the camera being held
                        Debug.Log("VideoCamera held by me found: " + aVideoCamera.name);
                        videoCamera = aVideoCamera;
                        if (videoCamera != null)
                        {
                            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string vhPath = Path.Combine(localAppDataPath, "Temp\\rec", lastVideoID.ToString());

                            // Create or clear the directory
                            string clipPath = Path.Combine(vhPath, lastClipID.ToString());
                            CreateOrClearDirectory(clipPath);

                            //Time to copy the file
                            string trollPath = selectedVideo;
                            string sourceFileName = "output.webm"; // file name
                            string sourceFilePath = Path.Combine(trollPath, sourceFileName);
                            string destinationFilePath = Path.Combine(clipPath, sourceFileName);
                            try
                            {
                                // Check if the source file exists
                                if (File.Exists(sourceFilePath))
                                {
                                    Debug.Log("Trying to copy file.");
                                    // Copy the file and allow overwriting of the destination file if it exists
                                    File.Copy(sourceFilePath, destinationFilePath, true);
                                    Debug.Log("File copied successfully.");
                                    HelmetText.Instance.SetHelmetText(folderButtonText + " has been injected", 2.5f);
                                }
                                else
                                {
                                    Debug.LogError($"Source file does not exist: {sourceFilePath}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log any errors during the copy process
                                Debug.LogError($"Error copying file: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogError("No VideoCamera found in scene.");
                        }
                    }
                }
            }
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

            MurderBind();
            ShadowRealmBind();
            SelectPlayer();
            ForceThrow();
            Revive();
            FakeTP();
            TrollFilm();
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
                if (Players.InGame.Count > 0)
                {
                    CenterLabel("Execute Object");
                    GUILayout.BeginHorizontal();
                    foreach (var playerkvp in new Dictionary<Player, bool>(Players.InGame)) // // Iterate over a copied collection to avoid errors, 不然会报错
                    {
                        Players.InGame[playerkvp.Key] = GUILayout.Toggle(Players.InGame[playerkvp.Key], playerkvp.Key.refs.view.Owner.NickName);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Teleport To"))
                        Players.TeleportTo();
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
                    if (GameObject.FindObjectOfType<PlayerCustomizer>() && Players.GetChoosedPlayerCount() == 1 && GUILayout.Button("Force entry into terminal"))
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                                    Items.SpawnItem(new byte[] { item.Key });
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
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Open Extractor"))
                    {
                        SurfaceNetworkHandler.UnlockExtractor();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Open Console"))
                    {
                        foreach (DebugUIHandler item in FindObjectsOfType<DebugUIHandler>())
                        {
                            item.Show();
                        }
                    }

                    if (GUILayout.Button("Close Console"))
                    {
                        foreach (DebugUIHandler item in FindObjectsOfType<DebugUIHandler>())
                        {
                            item.Hide();
                        }
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Black Screen/Break Lobby"))
                {
                    foreach (PhotonGameLobbyHandler handler in FindObjectsOfType<PhotonGameLobbyHandler>())
                    {
                        handler.photonView.RPC("RPC_StartTransition", RpcTarget.Others, Array.Empty<object>());
                        RetrievableResourceSingleton<TransitionHandler>.Instance.TransitionToBlack(3f, delegate
                        {
                            if (!PhotonNetwork.InRoom)
                            {
                                return;
                            }
                            VerboseDebug.Log("Returning To Surface!");
                            RetrievableSingleton<PersistentObjectsHolder>.Instance.FindPersistantObjects();
                            PhotonNetwork.LoadLevel("SurfaceScene");
                        }, 3f);
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("Troll Video Selection", GUI.skin.box);
                {
                    GUILayout.Space(20f);

                    // Assuming isFolderDropdownVisible and folderButtonText are defined similarly to your original variables
                    if (GUILayout.Button(folderButtonText/*, GUILayout.Width(200), GUILayout.Height(40)*/))
                    {
                        isFolderDropdownVisible = !isFolderDropdownVisible; // Toggle visibility of the dropdown
                    }

                    if (isFolderDropdownVisible)
                    {
                        // Set the scroll area for the folders dropdown
                        folderScrollPosition = GUILayout.BeginScrollView(folderScrollPosition, GUILayout.Width(270), GUILayout.Height(200));

                        // Get the directory where the current DLL is executing
                        string directoryOfExecutingAssembly = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                        // Define the path to the CWVideos folder relative to the DLL's location
                        string cwVideosPath = Path.Combine(directoryOfExecutingAssembly, "CWVideos");

                        // Ensure the CWVideos directory exists
                        if (Directory.Exists(cwVideosPath))
                        {
                            // Get all directories within the CWVideos folder
                            string[] folderPaths = Directory.GetDirectories(cwVideosPath);
                            string[] folderNames = new string[folderPaths.Length];

                            // Extract just the folder names for display
                            for (int i = 0; i < folderPaths.Length; i++)
                            {
                                folderNames[i] = new DirectoryInfo(folderPaths[i]).Name;
                            }

                            // Create a button for each folder
                            for (int i = 0; i < folderNames.Length; i++)
                            {
                                if (GUILayout.Button(folderNames[i] /*GUILayout.Width(190), GUILayout.Height(30)*/))
                                {
                                    selectedFolderIndex = i; // Track the selected index
                                    folderButtonText = folderNames[i]; // Update the button text to show the selected folder
                                    selectedVideo = folderPaths[i];
                                    Debug.Log("Selected Video " + selectedVideo);
                                    isFolderDropdownVisible = false; // Close the dropdown
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label("CWVideos folder not found.");
                        }

                        GUILayout.EndScrollView();
                    }

                }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Convert new Videos"))
                {
                    string directoryOfExecutingAssembly = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    // Define the path to the VideosToConvert folder relative to the DLL's location
                    string videosToConvertPath = Path.Combine(directoryOfExecutingAssembly, "VideosToConvert");
                    string cwVideosPath = Path.Combine(directoryOfExecutingAssembly, "CWVideos");

                    // Ensure the VideosToConvert directory exists
                    if (Directory.Exists(videosToConvertPath))
                    {
                        // Get all files within the VideosToConvert folder
                        string[] filePaths = Directory.GetFiles(videosToConvertPath);

                        if (filePaths.Length == 0)
                        {
                            GUILayout.Label("No videos to convert.");
                        }
                        else
                        {
                            foreach (string filePath in filePaths)
                            {
                                // Perform conversion for each file
                                string fileName = Path.GetFileName(filePath); // Get the file name
                                Conversor conversor = new Conversor();
                                conversor.EncodeVideo(filePath, cwVideosPath, fileName);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("VideosToConvert folder not found.");
                    }
                }
                if (GUILayout.Button("Analyze new Videos"))
                {
                    string directoryOfExecutingAssembly = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                    // Define the path to the VideosToConvert folder relative to the DLL's location
                    string videosToConvertPath = Path.Combine(directoryOfExecutingAssembly, "VideosToConvert");

                    // Ensure the VideosToConvert directory exists
                    if (Directory.Exists(videosToConvertPath))
                    {
                        // Get all files within the VideosToConvert folder
                        string[] filePaths = Directory.GetFiles(videosToConvertPath);

                        if (filePaths.Length == 0)
                        {
                            GUILayout.Label("No videos to analyze.");
                        }
                        else
                        {
                            foreach (string filePath in filePaths)
                            {
                                // Perform conversion for each file
                                Conversor conversor = new Conversor();
                                conversor.AnalyzeVideo(filePath, directoryOfExecutingAssembly);
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("VideosToConvert folder not found.");
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        void CreateOrClearDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Debug.Log($"Clip Directory cleared");
            }
            Directory.CreateDirectory(path);
            Debug.Log($"Clip Directory created");
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
