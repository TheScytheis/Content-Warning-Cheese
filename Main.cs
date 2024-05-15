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
using Steamworks;
using Sirenix.Utilities;
using UnityEngine.SceneManagement;
using EPOOutline;
using ExitGames.Client.Photon;
using BepInEx.Logging;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters;
using UnityEngine.UI;

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
        public bool joiningServer = false;

        private string moneyAddValue;
        public static VideoHandle lastVideoID;
        public static ClipID lastClipID;
        private Player selectedPlayer;
        public bool forceThrowRagdolls = true;
        public bool unlimitedFilm = false;
        //Troll Video Dropdown
        private bool isFolderDropdownVisible = false;
        private Vector2 folderScrollPosition;
        private string folderButtonText = "Select Video";
        private int selectedFolderIndex = -1;
        private string selectedVideo;
        //Region Dropdown
        private bool isRegionDropdownVisible = false;
        private Vector2 regionScrollPosition;
        private string regionButtonText = "Select Region";
        private int selectedRegionIndex = -1;
        private string selectedRegion;
        public static LobbyManager lobbyManager = new LobbyManager();
        public static GameObject LobbiesPage;

        //Helmet UI
        private static GameObject HelmetUI;
        private static TextMeshProUGUI myPublicName;
        private static TextMeshProUGUI serverId;
        private static TextMeshProUGUI serverHost;
        private static TextMeshProUGUI credits;

        //Nickname
        private string spoofedName = "Kirito Waifu 69 uWu";

        public ColorWheelWindow colorUtil = new ColorWheelWindow();

        public static void ApplyPatches()
        {
            Debug.Log("Trying to apply patches.");
            var harmony = new Harmony("com.holden.codewarning");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Debug.Log("Harmony patches applied.");
        }

        [HarmonyPatch(typeof(MainMenuHandler), "Start")]
        public static class StartPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                lobbyManager.Start();
            }
        }

        

        public static bool wasSentByForce = false;

        [HarmonyPatch(typeof(ExtractVideoMachine), "RPC_Success")]
        public static class ExtractSuccessPatch
        {
            [HarmonyPostfix]
            public static void OnSuccess()
            {
                if (!wasSentByForce)
                {
                    return;
                }
                Console.WriteLine("Listener OK");
                var machine = GameObject.FindObjectOfType<ExtractVideoMachine>();
                var videoHandleField = typeof(ExtractVideoMachine).GetField("m_videoHandle", BindingFlags.NonPublic | BindingFlags.Instance);
                if (videoHandleField == null)
                {
                    Console.WriteLine("m_videoHandle field not found.");
                    return;
                }
                VideoHandle video = (VideoHandle)videoHandleField.GetValue(machine);

                var flashcard = new FlashcardEntry { videoID = video };
                var tvs = GameObject.FindObjectsOfType<UploadVideoStation>();
                foreach (var tv in tvs)
                {
                    var methodInfo = typeof(UploadVideoStation).GetMethod("UploadFlashcard", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo == null)
                    {
                        Console.WriteLine("UploadFlashcard method not found.");
                        continue;
                    }
                    methodInfo.Invoke(tv, new object[] { flashcard });
                }
            }
        }

        public static class MyCustomSettings
        {
            public static string CustomCloudRegion = "defaultRegion";
        }

        [HarmonyPatch(typeof(SteamLobbyHandler), "OnMatchListReceived")]
        public static class OnMatchListReceivedPatch
        {
            [HarmonyPrefix] // Use Prefix to change behavior before the original method runs
            public static bool Prefix(LobbyMatchList_t param, bool biofailure, SteamLobbyHandler __instance)
            {
                if (biofailure)
                {
                    Debug.LogError("Matchlist Biofail");
                    return false; // Skip the original method
                }

                if (param.m_nLobbiesMatching == 0)
                {
                    Debug.Log("Found No Matches hosting");
                    UnityEngine.Object.FindObjectOfType<MainMenuHandler>().SilentHost();
                    return false; // Skip the original method
                }

                List<(CSteamID, int)> list = new List<(CSteamID, int)>();
                for (int i = 0; i < param.m_nLobbiesMatching; i++)
                {
                    CSteamID lobbyByIndex = SteamMatchmaking.GetLobbyByIndex(i);
                    string lobbyData = SteamMatchmaking.GetLobbyData(lobbyByIndex, "ContentWarningVersion");
                    string lobbyData2 = SteamMatchmaking.GetLobbyData(lobbyByIndex, "PhotonRegion");
                    int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(lobbyByIndex);
                    int lobbyMemberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyByIndex);

                    // Use the patched cloud region instead of PhotonNetwork.CloudRegion
                    string cloudRegion = MyCustomSettings.CustomCloudRegion;
                    bool flag = !string.IsNullOrEmpty(lobbyData2) && lobbyData2 == cloudRegion;
                    if (lobbyData == new BuildVersion(Application.version).ToMatchmaking() && flag && numLobbyMembers < 4)
                    {
                        list.Add((lobbyByIndex, i));
                    }
                }
                Debug.Log("Received SteamLobby Matchlist: " + param.m_nLobbiesMatching + " Matching: " + list.Count);
                if (list.Count > 0)
                {
                    CSteamID lobbyByIndex2 = SteamMatchmaking.GetLobbyByIndex(list.GetRandom().Item2);
                    MethodInfo methodInfo = typeof(SteamLobbyHandler).GetMethod("JoinLobby", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(__instance, new object[] { lobbyByIndex2 });
                    }
                }
                else
                {
                    Debug.Log("Found No Matches hosting");
                    UnityEngine.Object.FindObjectOfType<MainMenuHandler>().SilentHost();
                }
                // Original method logic continues or modified version...
                // Potentially call original method with base call or just return true or false based on your needs

                return false; // Return true to continue with original method after prefix, false to skip it
            }
        }

        [HarmonyPatch(typeof(GameHandler), "CheckPlugins")]
        public static class CheckPluginsPatch
        {
            [HarmonyPrefix] // Use Prefix to change behavior before the original method runs
            public static bool Prefix()
            {
                return false; // Return true to continue with original method after prefix, false to skip it
            }
        }

        [HarmonyPatch(typeof(LoadBalancingClient), nameof(LoadBalancingClient.OpJoinOrCreateRoom))]
        public static class PatchConnect
        {
            [HarmonyPostfix]
            public static void Connect()
            {
                NameSpoof.TrySetNickname();
                //SetHelmetUI();
            }

        }

        [HarmonyPatch(typeof(NextStepUI), "Show")]
        public static class PatchShowNextStepUI
        {
            [HarmonyPostfix]
            public static void NextStepUIPostFix()
            {
                SetHelmetUI();
            }

        }


        [HarmonyPatch(typeof(UIPageHandler), "TransistionToPage", typeof(UIPage), typeof(PageTransistion))]
        public static class PatchTransition
        {
            [HarmonyPrefix]
            public static void TransistionToPage(UIPage page, PageTransistion pageTransistion) => MyPageUI.TryAttachToPageHandler();
        }

        [HarmonyPatch(typeof(Player), "GetMicValueFromDecibels")]
        public static class PatchGetMicValue
        {
            [HarmonyPrefix]
            public static bool NewGetMicValueFromDecibels(float decibels, ref float __result)
            {
                // Define new bounds based on empirical data or testing
                float minDB = -60f;  // Adjusted based on typical ambient noise levels
                float maxDB = -10f;  // Adjusted to the upper range of normal speech

                // Non-linear remapping using an exponential curve to emphasize differences in typical speech levels
                float expBase = 1.5f;  // Adjust base to control the curve's steepness; greater than 1.0 makes it more sensitive at lower volumes
                float normalizedDB = Mathf.InverseLerp(minDB, maxDB, decibels);  // Normalize dB to a 0-1 range
                float exponentialValue = Mathf.Pow(expBase, normalizedDB) - 1.0f;
                exponentialValue /= (expBase - 1.0f);  // Normalize the result so it also ranges from 0 to 1

                __result = Mathf.Clamp01(exponentialValue);  // Ensure the result stays within the 0 to 1 range
                return false;  // Return false to indicate that the original method should not be run
            }

        }

        [HarmonyPatch(typeof(HelmetText), "InternalHelmetText")]
        public static class PatchInternalHelmetText
        {
            [HarmonyPrefix]
            public static bool Prefix(ref IEnumerator __result, string text, float time, HelmetText __instance)
            {
                // Create a reflection utility instance for the HelmetText object
                var reflector = new ReflectionUtil<HelmetText>(__instance);

                // Skipping the initial 1-second wait
                __result = ModifiedInternalHelmetText(text, time, reflector);
                return false;  // Return false to skip the original method
            }

            private static IEnumerator ModifiedInternalHelmetText(string text, float time, ReflectionUtil<HelmetText> reflector)
            {
                // Set writing flag to true using reflection
                reflector.SetValue("m_Writing", true);

                // Get the TextMeshProUGUI object using reflection
                var textObject = reflector.GetValue("m_TextObject", false, false) as TextMeshProUGUI;

                // Process text without the initial delay
                char[] array = text.ToArray();
                string written = string.Empty;
                foreach (char c in array)
                {
                    written += c;
                    textObject.text = written;
                    textObject.SetAllDirty();
                    yield return new WaitForSecondsRealtime((float)reflector.GetValue("TIME_BETWEEN_CHARACTERS", false, false));
                }

                yield return new WaitForSecondsRealtime(time);

                // Invoke the ClearText method using reflection
                reflector.Invoke("ClearText", false);
            }
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

        [HarmonyPatch(typeof(ProjectileStick), "Stick")]
        public static class MakeTheNerfsBoom
        {
            [HarmonyPostfix]
            public static void StickPost(ref RaycastHit hit, ProjectileStick __instance)
            {
                var hitTransform2 = __instance.transform;
                var player = hit.collider.transform.root.GetComponent<Player>();
                var dir = hitTransform2.rotation * hitTransform2.forward;
                MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
                if (methodInfo != null)
                {
                    methodInfo.Invoke(player, new object[] { 5f, dir, 5f });
                }
            }
        }

        [HarmonyPatch(typeof(PhysicsSound), "OnCollisionEnter")]
        public static class PhysicsSoundCollisionPatch
        {
            // This is the postfix method that Harmony will call after OnCollisionEnter
            [HarmonyPostfix]
            public static void Postfix(Collision collision, PhysicsSound __instance)
            {
                if (!Players.strongArm)
                    return;
                // Check if the collided object has a Player component
                var throwable = __instance.gameObject.GetComponent<TestUnityPlugin.Throwable.Throwables>();
                if (throwable == null)
                    return;
                var player = collision.collider.GetComponentInParent<Player>();
                if (player != null && throwable.recentlyThrown)
                {
                    // Here, you decide the direction and magnitude of the force
                    Vector3 direction = (__instance.transform.position - player.transform.position).normalized;
                    float forceMagnitude = 5f; // Example force magnitude
                    Vector3 force = direction * forceMagnitude;

                    // Apply your force or any other effect
                    MethodInfo soundmethodInfo = typeof(Player).GetMethod("CallMakeSound", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (soundmethodInfo != null)
                    {
                        soundmethodInfo.Invoke(player, new object[] { 0 });
                    }
                    MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(player, new object[] { 5f, force, 5f });
                    }
                }
                throwable.recentlyThrown = false;
            }
        }

        [HarmonyPatch(typeof(ItemInstance), "InitItem")]
        public static class ItemInstanceInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ItemInstance __instance)
            {
                FieldInfo isHeldinfo = typeof(ItemInstance).GetField("isHeld", BindingFlags.NonPublic | BindingFlags.Instance);
                if(isHeldinfo != null)
                {
                    bool isHeld = (bool)isHeldinfo.GetValue(__instance);
                    if (!isHeld)
                    {
                        var throwable = __instance.gameObject.GetComponent<TestUnityPlugin.Throwable.Throwables>();
                        if (throwable == null)
                        {
                            __instance.gameObject.AddComponent<TestUnityPlugin.Throwable.Throwables>();
                        }
                    }
                }
            }
        }

        public static readonly object keyByteFive = (object)(byte)5;
        public static readonly object keyByteThree = (object)(byte)3;
        public static readonly object keyByteFour = (object)(byte)4;
        /*
        [HarmonyPatch(typeof(PhotonNetwork), "ExecuteRpc")]
        public static class PatchToBlockRpcs
        {
            [HarmonyPrefix]
            public static bool ExecuteRPC(ExitGames.Client.Photon.Hashtable rpcData, Photon.Realtime.Player sender)
            {
                if (sender is null || sender.GamePlayer() is null) return true;

                string rpc = rpcData.ContainsKey(keyByteFive) ?
                    PhotonNetwork.PhotonServerSettings.RpcList[(int)(byte)rpcData[keyByteFive]] :
                    (string)rpcData[keyByteThree];

                if (!sender.IsLocal && sender.GamePlayer().Handle().IsRPCBlocked())
                {
                    Debug.LogError($"RPC {rpc} was blocked from {sender.NickName}.");
                    return false;
                }

                Debug.LogWarning($"Received RPC '{rpc}' From '{sender.NickName}'");

                sender.GamePlayer().Handle().OnReceivedRPC(rpc, rpcData);


                return true;
            }
        }
        */

        private static HelloWorld instance;
        public static HelloWorld Instance
        {
            get
            {
                if (instance == null) instance = new HelloWorld();
                return instance;
            }
        }

        void Start()
        {
            instance = this;
            //Bypass Plugin Check
            Traverse.Create(GameHandler.Instance).Field("m_pluginHash").SetValue(null);
            ApplyPatches();

            MyPageUI.TryAttachToPageHandler();

            if (NameSpoof._enabled)
            {
                NameSpoof.OnValueChanged(spoofedName);
            }

            cursorHandler = FindObjectOfType<CursorHandler>();

            HelloWorld.assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            HelloWorld.assets = AssetBundle.LoadFromFile(Path.Combine(HelloWorld.assemblyLocation, "spectatedependancies"));
            bool flag2 = HelloWorld.assets == null;
            if (flag2)
            {
                Debug.LogError("Failed to load custom assets.");
            }
        }
        private static CursorLockMode lockMode = CursorLockMode.Confined;

        private static CursorHandler cursorHandler;

        public static void ShowCursor()
        {
            //LethalMenu.localPlayer?.playerActions.Disable();
            if (cursorHandler != null)
            {
                cursorHandler.enabled = false;
            }
            else
            {
                cursorHandler = FindObjectOfType<CursorHandler>();
                cursorHandler.enabled = false;
            }
            Cursor.visible = true;
            lockMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Confined;
        }

        public static void HideCursor()
        {
            //LethalMenu.localPlayer?.playerActions.Enable();
            if (cursorHandler != null)
            {
                if (!cursorHandler.enabled)
                {
                    cursorHandler.enabled = true;
                    Cursor.visible = true;
                }
            }
            else
            {
                if (!cursorHandler.enabled)
                {
                    cursorHandler = FindObjectOfType<CursorHandler>();
                    cursorHandler.enabled = true;
                    Cursor.visible = true;
                }
            }
        }

        void OnGUI()
        {
            ESP.StartESP();
            Renderer.DrawCrosshair(Color.white, 25, 1);
            if (!DisplayingWindow)
            {
                return;
            }
            colorUtil.DrawWindow();
            GUI.backgroundColor = Color.black;
            windowRect = ClampToScreen(GUI.Window(9, windowRect, WindowFunc, "Holden's Content Warning MOD!"));
            if (DisplayingLobbyWindow)
            {
                lobbyWindowRect = ClampToScreen(GUI.Window(10, lobbyWindowRect, LobbyWindowFunc, "The Game Lobbies!"));
            }
        }
        Rect ClampToScreen(Rect window)
        {
            window.x = Mathf.Clamp(window.x, 0, Screen.width - window.width);
            window.y = Mathf.Clamp(window.y, 0, Screen.height - window.height);
            return window;
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
                if (selectedPlayer != null && selectedPlayer.gameObject.GetComponent<Outlinable>())
                {
                    selectedPlayer.gameObject.GetComponent<Outlinable>().enabled = false;
                }
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

                            if (selectedPlayer.gameObject.GetComponent<Outlinable>())
                            {
                                selectedPlayer.gameObject.GetComponent<Outlinable>().enabled = true;
                            }
                            else
                            {
                                var m_outline = selectedPlayer.gameObject.GetComponent<Outlinable>();
                                if (m_outline == null)
                                {
                                    m_outline = selectedPlayer.gameObject.AddComponent<Outlinable>();
                                    // Configure the outlinable settings
                                    m_outline.OutlineParameters.Color = Color.green; // Set the outline color
                                    m_outline.OutlineParameters.DilateShift = 1f; // Set the outline width

                                    // Optionally add outline configurations here, such as adding specific outline layers
                                    m_outline.OutlineParameters.Color = Color.red;

                                    m_outline.AddAllChildRenderersToRenderingList(RenderersAddingMode.All, includeInactive: false);
                                    m_outline.enabled = true;
                                }
                            }
                            if (selectedPlayer.refs.view.Controller != null)
                            {
                                MethodInfo methodInfo = typeof(Player).GetMethod("CallMakeSound", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (methodInfo != null)
                                {
                                    methodInfo.Invoke(selectedPlayer, new object[] { 0 });
                                }
                                HelmetText.Instance.SetHelmetText("Player Selected: " + selectedPlayer.refs.view.Controller.ToString(), 2.5f);
                            }
                            else
                            {
                                HelmetText.Instance.SetHelmetText("Monster Selected", 2.5f);
                            }
                        }
                        else
                        {
                            selectedPlayer = null;
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
                bool isThereSomeoneInRealm = false;
                if (selectedPlayer != null && playerComponent != null)
                {
                    if (!playerComponent.data.playerIsInRealm)
                    {
                        isThereSomeoneInRealm = false;
                        // Check if there is anyone in a realm
                        foreach (var keyValuePair in Players.InGame)
                        {
                            if (keyValuePair.Key.data.playerIsInRealm)
                            {
                                isThereSomeoneInRealm = true;
                                break;  // Stop checking once you find someone in a realm
                            }
                        }
                        if (isThereSomeoneInRealm)
                        {
                            Debug.LogWarning("Sending to existing realm");
                            ShadowRealmHandler instance = ShadowRealmHandler.instance; // Get the instance
                            ReflectionUtil<ShadowRealmHandler> reflector = new ReflectionUtil<ShadowRealmHandler>(instance);
                            reflector.Invoke("AddPlayerToExistingRealm", false, playerComponent, 6);
                        }
                        else
                        {
                            isThereSomeoneInRealm = true;
                            var realms = ShadowRealmHandler.instance.Reflect().GetValue("currentRealms", false, false) as Realm[];
                            PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                            view.RPC("RPCA_AddRealm", RpcTarget.All, realms[8], 6, playerComponent.refs.view.ViewID);
                        }
                    }
                    else
                    {
                        // Access the private field 'currentRealms'
                        FieldInfo fieldInfo = typeof(ShadowRealmHandler).GetField("currentRealms", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfo != null)
                        {
                            Realm[] currentRealms = (Realm[])fieldInfo.GetValue(ShadowRealmHandler.instance);

                            // Find the realm the player is currently in
                            foreach (Realm realm in currentRealms)
                            {
                                if (realm != null)
                                {
                                    RealmGateTrigger triggerComponent = realm.realmObject.GetComponentInChildren<RealmGateTrigger>();
                                    // Use reflection to access the 'realmData' field
                                    FieldInfo realmDataField = typeof(RealmGateTrigger).GetField("realmData", BindingFlags.Instance | BindingFlags.NonPublic);
                                    Realm realmData = (Realm)realmDataField.GetValue(triggerComponent);

                                    if (realmData != null && realmData.playersInRealm.Contains(playerComponent))
                                    {
                                        PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                                        view.RPC("RPCA_RemovePlayerFromRealm", RpcTarget.All, 6, playerComponent.refs.view.ViewID, Player.localPlayer.gameObject.transform.position);
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
            float Fall()
            {
                if (forceThrowRagdolls)
                {
                    return 2f;
                }
                else
                {
                    return 0f;
                }
            } 
            MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(selectedPlayer, new object[] { 0f, force, Fall() });
            }
            yield return new WaitForSeconds(0.2f);
        }

        public void UnlimitedFilm()
        {
            if (Player.localPlayer is null || !unlimitedFilm) return;

            ItemInstance item = Player.localPlayer.data.currentItem;
            if (item is null || item.GetComponent<VideoCamera>() is null) return;
            VideoCamera camera = item.GetComponent<VideoCamera>();

            VideoInfoEntry videoInfoEntry = camera.Reflect().GetValue("m_recorderInfoEntry") as VideoInfoEntry;
            videoInfoEntry.timeLeft = videoInfoEntry.maxTime;
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
                                    methodInfo.Invoke(selectedPlayer, new object[] { 0f, totalForce, -30f });
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
        public bool flyHack = true;
        public bool armsUp = true;
        public float flyForce = 4;

        public void Superman()
        {
            if (Input.GetMouseButton(1) && flyHack)
            {
                if (Player.localPlayer != null)
                {
                    var selected = Player.localPlayer;

                    // Posição da câmera
                    var camForward = MainCamera.instance.transform.rotation * Vector3.forward;
                    var camPosition = MainCamera.instance.transform.position;

                    // Posição desejada do jogador selecionado (uma distância 'forcadedada' à frente da câmera)
                    var desiredPosition = camPosition + camForward * flyForce;
                    FieldInfo rigListField = typeof(PlayerRagdoll).GetField("rigList", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rigListField != null)
                    {
                        var rigList = rigListField.GetValue(selected.refs.ragdoll) as List<Rigidbody>;
                        if (rigList != null)
                        {

                            foreach (var rg in rigList)
                            {
                                if (rg != null && rg.name == "Hip")
                                {
                                    // Calcula a diferença entre a posição atual e a posição desejada
                                    Vector3 positionDiff = desiredPosition - rg.position;

                                    // Calcula a força a ser aplicada na direção oposta à diferença de posição
                                    Vector3 force = positionDiff.normalized * positionDiff.magnitude;

                                    // Aplica a força aos membros do rg
                                    MethodInfo methodInfo = typeof(Player).GetMethod("CallAddForceToBodyParts", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (armsUp)
                                    {
                                        if (methodInfo != null)
                                        {
                                            methodInfo.Invoke(selected, new object[] { new int[] { 5, 8 }, new Vector3[] { force, force } });
                                            selected.data.fallTime = 0f;
                                            selected.data.sinceGrounded = 0f;
                                        }
                                    }
                                    if (methodInfo != null)
                                    {
                                        methodInfo.Invoke(selected, new object[] { new int[] { 0, 1, 2, 5, 8 }, new Vector3[] { force, force, force, force, force } });
                                        selected.data.fallTime = 0f;
                                        selected.data.sinceGrounded = 0f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

 

        private static void SetHelmetUI()
        {
            if(HelmetUI == null)
            {
                Debug.Log("HELMET UI SETUP PASSEDDDD !!!!!!!!");
                var ogUI = FindObjectOfType<NextStepUI>();
                if (ogUI == null)
                    return;
                HelmetUI = Instantiate(ogUI.gameObject, ogUI.gameObject.transform.parent, true);
                Destroy(HelmetUI.GetComponent<NextStepUI>());
                Destroy(HelmetUI.GetComponentInChildren<UI_DaysLeft>());
                Destroy(HelmetUI.GetComponentInChildren<UI_Views>());
                HelmetUI.gameObject.transform.localPosition = new Vector3(HelmetUI.gameObject.transform.localPosition.x, HelmetUI.gameObject.transform.localPosition.y -531f, HelmetUI.gameObject.transform.localPosition.z);
                AdjustHUI();
            }
        }

        private static void AdjustHUI()
        {
            if(HelmetUI != null)
            {
                TextMeshProUGUI[] tmPros = HelmetUI.GetComponentsInChildren<TextMeshProUGUI>();
                foreach(TextMeshProUGUI textMPRO in tmPros)
                {
                    if(textMPRO != null)
                    {
                        if(textMPRO.gameObject.name == "Item Name")
                        {
                            myPublicName = textMPRO;
                            myPublicName.text = "My Nickname: " + PhotonNetwork.LocalPlayer.NickName;
                            continue;
                        }
                        if(textMPRO.gameObject.name == "DAYS")
                        {
                            //hmmm this isnt working for some reason
                            serverHost = textMPRO;
                            serverHost.text = "Host: " + PhotonNetwork.MasterClient.NickName;
                            continue;
                        }
                        if(textMPRO.gameObject.name == "VIEWS")
                        {
                            serverId = textMPRO;
                            serverId.text = "Lobby ID: " + PhotonNetwork.CurrentRoom.Name.Remove(5);
                            continue;
                        }
                        if (textMPRO.gameObject.name == "Text (TMP) (2)")
                        {
                            serverId = textMPRO;
                            serverId.text = "Region: " + PhotonNetwork.CloudRegion.ToUpper();
                            continue;
                        }
                    }
                }

            }
        }

        public bool DisplayingLobbyWindow = false;
        private float timer = 0f;
        private float interval = 1.0f; // 1 second interval
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                DisplayingWindow = !DisplayingWindow;
                if (DisplayingWindow)
                {
                    if(Player.localPlayer != null)
                    {
                        ShowCursor();
                        Player.localPlayer.data.hookedIntoTerminal = true;
                    }
                }
                else
                {
                    if (Player.localPlayer != null)
                    {
                        HideCursor();
                        Player.localPlayer.data.hookedIntoTerminal = false;
                    }
                }
            }
            AdjustHUI();
            MurderBind();
            ShadowRealmBind();
            SelectPlayer();
            ForceThrow();
            Revive();
            FakeTP();
            TrollFilm();
            UnlimitedFilm();
            Data.UpdateData();
            Players.Run();
            Items.Run();
            Misc.Run();
            Players.FlyOthers();
            Players.IfSqueakThenPunish(Players.punishAll, Players.ifScreamingThenPunish);
            Superman();

            // Timer check
            timer += Time.deltaTime;
            if ((DisplayingLobbyWindow || (LobbiesPage != null && LobbiesPage.activeInHierarchy)) && timer >= interval)
            {
                SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
                SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
                SteamMatchmaking.RequestLobbyList();
                // Reset timer
                timer = 0f;
            }

        }
        public bool DisplayingWindow = false;
        public Rect windowRect = new Rect(0, 0, 500, 650);
        public Rect lobbyWindowRect = new Rect(0, 0, 600, 650);
        private readonly int title_height = 17;
        private Vector2 scrollPosition = Vector2.zero;
        private void WindowFunc(int winId)
        {
            try
            {
                // Draggable window
                GUI.DragWindow(new Rect(0, 0, windowRect.width, title_height));

                // Toolbar
                GUILayout.BeginArea(new Rect(0, title_height + 5, windowRect.width, 30));
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Self"))
                    Hack.GUILabel = Hack.Label.Self;
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

                GUILayout.EndArea();

                if(Hack.GUILabel == Hack.Label.Self)
                {
                    int x = 0, y = 0;
                    GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));

                    GUILayout.BeginHorizontal();
                    Players.InfinityHealth = GUILayout.Toggle(Players.InfinityHealth, "Unlimited Health");
                    Players.InfinityStamina = GUILayout.Toggle(Players.InfinityStamina, "Unlimited Stamina");
                    Players.InfinityOxy = GUILayout.Toggle(Players.InfinityOxy, "Unlimited Oxygen");
                    unlimitedFilm = GUILayout.Toggle(unlimitedFilm, "Unlimited Film");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    forceThrowRagdolls = GUILayout.Toggle(forceThrowRagdolls, "Force Throw Ragdolls");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    Players.InfinityJump = GUILayout.Toggle(Players.InfinityJump, "No Jump Interval");
                    Players.NeverFalldown = GUILayout.Toggle(Players.NeverFalldown, "Never Fall Down");
                    Players.NeverDie = GUILayout.Toggle(Players.NeverDie, "Never Die");
                    flyHack = GUILayout.Toggle(flyHack, "Superman");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        NameSpoof._enabled = GUILayout.Toggle(NameSpoof._enabled, "Spoof Name");
                        // Use GUILayout.TextField to input and update the name
                        string newName = GUILayout.TextField(spoofedName, 100);
                        if (NameSpoof._enabled && PhotonNetwork.LocalPlayer.NickName != newName)
                        {
                            spoofedName = newName; // Update the stored name
                            NameSpoof.OnValueChanged(spoofedName);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if(GUILayout.Button("Change Face Color"))
                    {
                        colorUtil.showColorWheel = !colorUtil.showColorWheel;
                        if (colorUtil.showColorWheel)
                        {
                            colorUtil.OnColorSelected += ChangeFaceColorSelected;
                            //After done selecting
                        }
                        else
                        {
                            colorUtil.OnColorSelected -= ChangeFaceColorSelected;
                        }
                    }
                    GUILayout.EndHorizontal();
                    CenterLabel($"SUPERMAN Speed Multiplier: {Math.Round(flyForce, 2)}x");
                    flyForce = GUILayout.HorizontalSlider(flyForce, 1f, 20f);
                    
                    //CenterLabel($"Running Speed Multiplier: {Math.Round(Players.SprintMultipiler, 2)}x");
                    //Players.SprintMultipiler = GUILayout.HorizontalSlider(Players.SprintMultipiler, 1f, 20f);
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

                    GUILayout.EndArea();
                }
                if (Hack.GUILabel == Hack.Label.Players)
                {
                    int x = 0, y = 0;
                    GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));
                    GUILayout.BeginHorizontal();
                    Players.strongArm = GUILayout.Toggle(Players.strongArm, "Give em a strong arm");
                    Players.flyOthers = GUILayout.Toggle(Players.flyOthers, "Let em fly");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    Players.ifScreamingThenPunish = GUILayout.Toggle(Players.ifScreamingThenPunish, "If Screaming then Punish");
                    Players.punishAll = GUILayout.Toggle(Players.punishAll, "Punish All?");
                    GUILayout.EndHorizontal();


                    CenterLabel($"Average Volume Threshold: {Players.thresholdForPunishment}");
                    Players.thresholdForPunishment = GUILayout.HorizontalSlider(Players.thresholdForPunishment, 0f, 1f);

                    
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
                        if (GUILayout.Button("Bring to Me"))
                            Players.BringToMe();
                        if (GUILayout.Button("Revive"))
                            Players.Respawn();
                        if (GUILayout.Button("Kill"))
                            Players.Die();
                        if (GUILayout.Button("Cum"))
                            Players.Cum();
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
                        if (GUILayout.Button("Gay Face"))
                            StartCoroutine(Players.TrollPlayerTerminal());
                        if (GameObject.FindObjectOfType<PlayerCustomizer>() && Players.GetChoosedPlayerCount() == 1 && GUILayout.Button("Force entry into terminal"))
                            Players.ForceEnterTerminal();
                        if (GUILayout.Button("Enter Realm"))
                            Players.JoinRealm();
                        if (GUILayout.Button("Exit Realm"))
                            Players.RemoveRealm();
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(20f);

                    if (Players.GetChoosedPlayerCount() >= 1)
                    {
                        Dictionary<Player, List<ItemDescriptor>> InvItems = Players.GetPlayerInventoryItems();
                        foreach (var playerItems in InvItems)
                        {
                            Player player = playerItems.Key;
                            List<ItemDescriptor> items = playerItems.Value;

                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Player: " + SteamFriends.GetFriendPersonaName(player.GetSteamID())); // Optionally display player name
                            foreach (ItemDescriptor item in items)
                            {
                                if (GUILayout.Button(item.item.displayName))
                                {
                                    Players.StealItem(player, item);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.EndArea();
                }
                if (Hack.GUILabel == Hack.Label.Items)
                {
                    int x = 0, y = 0;
                    GUILayout.BeginArea(new Rect(x, y + (title_height + 35), windowRect.width, windowRect.height - y - (title_height + 35)));

                    CenterLabel("Generation Method");
                    Items.SpawnMethod = (Items.SpawnType)GUILayout.Toolbar((int)Items.SpawnMethod, new string[] { "Add to Inventory", "Spawn On Other Inv", "Create in Front", "Call Drone to Spawn" });
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                                        Items.SpawnItem(new byte[] { item.Key }, UsefulFuncs.GetCrosshairPosition(MaxDistance: 5f));
                                        break;

                                    case Items.SpawnType.CallDrone:
                                        Items.SpawnItem(new byte[] { item.Key });
                                        break;
                                    case Items.SpawnType.SpawnOnOtherInv:
                                        Items.SpawnItem(item.Key, true);
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
                        if (GUILayout.Button("Revive All"))
                            Monsters.ReviveAll();
                        if (GUILayout.Button("Kill AI"))
                            Monsters.KillAll();
                        if (GUILayout.Button("Spawn Bomb"))
                            Monsters.Explode();
                        if (GUILayout.Button("Fall Down"))
                            Monsters.Falldown();
                        if (GUILayout.Button("Spray"))
                            Monsters.Cum();
                        if (GUILayout.Button("Drag Nearby"))
                            Monsters.DragToLocal();
                        if (GUILayout.Button("Make Dogs Bark"))
                        {
                            Monsters.MakeMouthesScream();
                        }
                        if (GUILayout.Button("Clear All Monsters"))
                        {
                            Monsters.ClearAllMonsters();
                        }
                        GUILayout.EndHorizontal();

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

                    GUILayout.BeginVertical("Lobby Region Select", GUI.skin.box, GUILayout.Width(windowRect.width));
                    {
                        GUILayout.Space(20f);

                        // Assuming isFolderDropdownVisible and folderButtonText are defined similarly to your original variables
                        if (GUILayout.Button(regionButtonText/*, GUILayout.Width(200), GUILayout.Height(40)*/))
                        {
                            isRegionDropdownVisible = !isRegionDropdownVisible; // Toggle visibility of the dropdown
                        }

                        if (isRegionDropdownVisible)
                        {
                            // Set the scroll area for the folders dropdown
                            regionScrollPosition = GUILayout.BeginScrollView(regionScrollPosition, GUILayout.Width(270), GUILayout.Height(200));

                            if (GUILayout.Button("US"))
                            {
                                selectedRegion = "us";
                                MyCustomSettings.CustomCloudRegion = selectedRegion;
                                isRegionDropdownVisible = false; // Close the dropdown
                            }
                            if (GUILayout.Button("SA"))
                            {
                                selectedRegion = "sa";
                                MyCustomSettings.CustomCloudRegion = selectedRegion;
                                isRegionDropdownVisible = false; // Close the dropdown
                            }
                            if (GUILayout.Button("EU"))
                            {
                                selectedRegion = "eu";
                                MyCustomSettings.CustomCloudRegion = selectedRegion;
                                isRegionDropdownVisible = false; // Close the dropdown
                            }
                            if (GUILayout.Button("USW"))
                            {
                                selectedRegion = "usw";
                                MyCustomSettings.CustomCloudRegion = selectedRegion;
                                isRegionDropdownVisible = false; // Close the dropdown
                            }
                            if (GUILayout.Button("AS"))
                            {
                                selectedRegion = "as";
                                MyCustomSettings.CustomCloudRegion = selectedRegion;
                                isRegionDropdownVisible = false; // Close the dropdown
                            }
                            GUILayout.EndScrollView();
                        }

                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical("Private Server Breach", GUI.skin.box);
                    {
                        GUILayout.Space(20f);

                        if (GUILayout.Button("Join Random Private Server"))
                        {
                            if(MainMenuHandler.Instance != null)
                            {
                                StartCoroutine(JoinRandomPrivateGame());
                            }
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginHorizontal();
                    Misc.AutoJoinRandom = GUILayout.Toggle(Misc.AutoJoinRandom, "Auto Quick Play");
                    Misc.ForceJoinOthersRoom = GUILayout.Toggle(Misc.ForceJoinOthersRoom, "Force Join Others' Room");
                    GUILayout.EndHorizontal();
                    Misc.DisableAutoJoinRandomWhenJoined = GUILayout.Toggle(Misc.DisableAutoJoinRandomWhenJoined, "Disable Auto Quick Play After Joining");
                    if (GUILayout.Button("Create Public Multiplayer Room") && MainMenuHandler.Instance)
                        MainMenuHandler.Instance.SilentHost();
                    GUILayout.BeginVertical("Lobby Options", GUI.skin.box);
                    {
                        GUILayout.Space(20f);

                        //HospitalBill
                        string moneyValue = GUILayout.TextField(moneyAddValue);
                        moneyAddValue = moneyValue;
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Add"))
                            {
                                int value;
                                int.TryParse(moneyAddValue, out value);
                                SendHospitalBill(-value);
                            }
                            if (GUILayout.Button("Remove"))
                            {
                                int value;
                                int.TryParse(moneyAddValue, out value);
                                SendHospitalBill(value);
                            }
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Add 500k Meta Coins"))
                            {
                                SurfaceNetworkHandler.Instance.photonView.RPC("RPCA_OnNewWeek", RpcTarget.All, 100);
                                //MetaProgressionHandler.AddMetaCoins(500000);
                            }
                            if (GUILayout.Button("Give Money and Views"))
                            {
                                var __instance = new ExtractVideoMachine();
                                var reflector = new ReflectionUtil<ExtractVideoMachine>(__instance);
                                var view = reflector.GetValue("m_photonView") as PhotonView;
                                view.RPC("RPC_ExtractDone", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, false);
                            }
                            if (GUILayout.Button("Skip Day (Host Only)"))
                            {
                                AdvanceDay();
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Restock Hat Store"))
                        {
                            MethodInfo methodInfo = typeof(HatShop).GetMethod("StockShop", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (methodInfo != null)
                            {
                                methodInfo.Invoke(HatShop.instance, new object[] { });
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Break Lobby"))
                    {
                        // Check if the local player is the master client
                        if (PhotonNetwork.LocalPlayer.IsMasterClient)
                        {
                            foreach (var keyValuePair in Players.InGame)
                            {
                                // Ensure the key is not null and the player is not the local player
                                if (keyValuePair.Key && keyValuePair.Key != Player.localPlayer)
                                {
                                    // Found a player that is not the local player, set them as the master client
                                    PhotonNetwork.CurrentRoom.SetMasterClient(keyValuePair.Key.refs.view.Owner);
                                    break;  // Stop checking once a new master is found
                                }
                            }
                        }
                        else
                        {
                            // If the local player is not the master client, set the local player as the master client
                            PhotonNetwork.CurrentRoom.SetMasterClient(Player.localPlayer.refs.view.Owner);
                        }

                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();

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
                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Force Extraction"))
                            {
                                VideoCamera[] getAllHandlers = FindObjectsOfType<VideoCamera>();
                                wasSentByForce = true;
                                foreach (var camera in getAllHandlers)
                                {
                                    if (camera != null && camera.isHeldByMe)
                                    {
                                        // Reflectively access the private field 'm_recorderInfoEntry'
                                        FieldInfo recorderInfoEntryField = typeof(VideoCamera).GetField("m_recorderInfoEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                                        VideoInfoEntry handler = null;
                                        if (recorderInfoEntryField != null)
                                        {
                                            handler = recorderInfoEntryField.GetValue(camera) as VideoInfoEntry;
                                        }
                                        if (handler != null && handler.videoID.id != null)
                                        {
                                            var machine = FindObjectOfType<ExtractVideoMachine>();
                                            var tv = FindObjectsOfType<UploadVideoStation>();
                                            machine.StartExtract(handler.videoID);
                                            foreach (var v in tv)
                                            {
                                                v.Unlock();
                                            }
                                            HelmetText.Instance.SetHelmetText("Hold camera to force extraction", 1.5f);
                                        }
                                    }
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
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
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    CenterLabel("Miscellaneous");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Toggle Lobby Window"))
                    {
                        DisplayingLobbyWindow = !DisplayingLobbyWindow;
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
                    GUILayout.EndArea();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error drawing main  window: " + ex);
            }
        }

        private Vector2 lobbyScrollPosition;
        public string regionToSort = "all";
        private static string usLabel = "US";
        private static string uswLabel = "USW";
        private static string saLabel = "SA";
        private static string asiaLabel = "ASIA";
        private static string euLabel = "EU";
        private static string allLabel = "ALL";
        // Set the scroll area for the folders dropdown
        private void LobbyWindowFunc(int winId)
        {
            try
            {
                GUI.DragWindow(new Rect(0, 0, lobbyWindowRect.width, title_height));

                GUILayout.BeginVertical("Sort by Region", GUI.skin.box);
                {
                    GUILayout.Space(20f);
                    GUILayout.BeginHorizontal();


                    if (GUILayout.Button(usLabel + " (" + lobbyManager.usLobbies.Count + ")"))
                    {
                        regionToSort = "us";
                    }
                    if (GUILayout.Button(uswLabel + " (" + lobbyManager.uswLobbies.Count + ")"))
                    {
                        regionToSort = "usw";
                    }
                    if (GUILayout.Button(euLabel + " (" + lobbyManager.euLobbies.Count + ")"))
                    {
                        regionToSort = "eu";
                    }
                    if (GUILayout.Button(saLabel + " (" + lobbyManager.saLobbies.Count + ")"))
                    {
                        regionToSort = "sa";
                    }
                    if (GUILayout.Button(asiaLabel + " (" + lobbyManager.asiaLobbies.Count + ")"))
                    {
                        regionToSort = "asia";
                    }
                    if (GUILayout.Button(allLabel + "  (" + lobbyManager.lobbyList.Count + ")"))
                    {
                        regionToSort = "all";
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                int x = 0, y = 0;
                GUILayout.BeginVertical("Available Lobbies", GUI.skin.box);
                {
                    GUILayout.Space(20f);

                    if (lobbyManager.lobbyList.Count > 0)
                    {
                        lobbyScrollPosition = GUILayout.BeginScrollView(lobbyScrollPosition, GUILayout.Width(600), GUILayout.Height(550));
                        foreach (var room in lobbyManager.lobbyList)
                        {
                            if (room.IsValid())
                            {
                                if (joiningServer)
                                    continue;
                                var pLimit = SteamMatchmaking.GetLobbyMemberLimit(room);
                                var roomId = SteamMatchmaking.GetLobbyData(room, "Photon");
                                var m_players = SteamMatchmaking.GetNumLobbyMembers(room);
                                var m_region = SteamMatchmaking.GetLobbyData(room, "PhotonRegion");
                                //SteamMatchmaking.JoinLobby(room);
                                var m_hostID = SteamMatchmaking.GetLobbyOwner(room);
                                //SteamMatchmaking.LeaveLobby(room);
                                var m_host = SteamFriends.GetFriendPersonaName(m_hostID);

                                var title = $"Host: {m_host.Substring(0, Math.Min(8, m_host.Length))} | {m_region.ToUpper()} | Players: {m_players}/{pLimit}";
                                if (regionToSort != "all" && m_region != regionToSort)
                                    continue;
                                if (GUILayout.Button(title))
                                {
                                    MethodInfo methodInfo = typeof(SteamLobbyHandler).GetMethod("JoinLobby", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (methodInfo != null)
                                    {
                                        //StartCoroutine(JoinCD());
                                        methodInfo.Invoke(MainMenuHandler.SteamLobbyHandler, new object[] { room });
                                    }
                                }
                            }
                        }
                        GUILayout.EndScrollView();
                    }

                }
                GUILayout.EndVertical();

            }
            catch (Exception ex)
            {
                Debug.LogError("Error drawing window: " + ex);
            }
        }

        private IEnumerator JoinCD()
        {
            joiningServer = true;
            yield return new WaitForSeconds(15f);
            joiningServer = false;
        }

        public static void AdvanceDay()
        {
            if (!SurfaceNetworkHandler.Instance) return;
            FieldInfo m_Startedinfo = typeof(SurfaceNetworkHandler).GetField("m_Started", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_Startedinfo != null)
            {
                bool m_Started = (bool)m_Startedinfo.GetValue(SurfaceNetworkHandler.Instance);
                if (!m_Started)
                {
                    SurfaceNetworkHandler.Instance.RequestStartGame();
                    var view = SurfaceNetworkHandler.Instance.Reflect().GetValue("m_View") as PhotonView;
                    view.RPC("RPCA_Sleep", RpcTarget.All);
                }
                
            }
        }
        public static void SendHospitalBill(int amount)
        {
            var p = PhotonNetwork.PlayerListOthers.ToList().Count > 0 ? PhotonNetwork.PlayerListOthers.ToList().First() : PhotonNetwork.LocalPlayer;
            // Create a list of tuples manually
            List<(int, int)> bill = new List<(int, int)>();
            bill.Add((p.ActorNumber, amount)); // Add the tuple to the list

            SurfaceNetworkHandler.Instance.Reflect().Invoke("SendHospitalBill", false, bill);
        }


        // Token: 0x04000001 RID: 1
        public const string GUID = "spectatedependancies";

        // Token: 0x04000006 RID: 6
        public static ManualLogSource Log;

        // Token: 0x04000007 RID: 7
        public static string assemblyLocation;

        // Token: 0x04000008 RID: 8
        public static AssetBundle assets;

        public static void SetCloudRegion(string region)
        {
            var networkingClient = PhotonNetwork.NetworkingClient; // Get the LoadBalancingClient instance
            var clientType = networkingClient.GetType(); // Get the Type of LoadBalancingClient

            // If LoadBalancingClient has a private or internal field or property storing the region, access it
            var cloudRegionField = clientType.GetField("cloudRegionField", BindingFlags.NonPublic | BindingFlags.Instance); // Adjust field name as necessary

            if (cloudRegionField != null)
            {
                cloudRegionField.SetValue(networkingClient, region); // Set the new region
            }
            else
            {
                Debug.LogError("Cloud region field not found in LoadBalancingClient.");
            }
        }

        private void ChangeFaceColorSelected(Color color)
        {
            Player.localPlayer.refs.visor.ApplyVisorColor(color);
            colorUtil.OnColorSelected -= ChangeFaceColorSelected;
        }

        public static IEnumerator JoinRandomPrivateGame()
        {
            Debug.Log("Joining Random Private Photon Room");
            //Hosting a game, Allows me to kick myself from the photon room, the game then bugs out and places me in a random what seems to be in progress, full, or private lobbies or all 3
            MainMenuHandler.Instance.Host(1); //host nonsaveable save to not screw with any other save

            //lets wait for the hosted lobby
            yield return new WaitForSeconds(4f);

            Debug.Log("Self Kicking / going to private lobby");
            //enable kicking in the photon room (this is local even if you are master client thus why we host our own lobby)
            PhotonNetwork.EnableCloseConnection = true;

            //Send the kick notification to myself (with it enabled and being the master as it does check if the request is sent by the master)
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions()
            {
                TargetActors = new int[1] { PhotonNetwork.LocalPlayer.ActorNumber }
            };
            PhotonNetwork.NetworkingClient.OpRaiseEvent((byte)203, (object)null, raiseEventOptions, SendOptions.SendReliable);

            //Waiting for the kick to happen and be randomly sent to another lobby, this takes a few seconds
            yield return new WaitForSeconds(5f);

            Debug.Log("Handle Surface Joining");

            //if (SceneManager.GetActiveScene().name != "SurfaceScene") //if in underworld go under with em, stopping lag. Else saty with em
            //else


            SurfaceNetworkHandler.Instance.photonView.RPC("RPC_LoadScene", RpcTarget.All, "FactoryScene");
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
            Players,
            Self
        }
    }
}
