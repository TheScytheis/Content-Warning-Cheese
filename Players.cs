using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using pworld.Scripts;
using Steamworks;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using static HelperFunctions;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

namespace TestUnityPlugin
{
    public class Players
    {
        public static bool strongArm = true;
        public static bool ifScreamingThenPunish = false;
        public static bool punishAll = false;
        public static bool flyOthers = false;
        public static bool isCheckingInventory = false;
        public static bool InfinityHealth = false;
        public static bool InfinityOxy = false;
        public static bool InfinityStamina = true;
        public static bool InfinityJump = true;
        public static bool InfinityBattary = true;
        public static bool NeverFalldown = false;
        public static bool NeverDie = false;
        public static float SprintMultipiler = 1f;
        public static float JumpHeightMultipiler = 1f;
        public static Dictionary<Player, bool> InGame = new Dictionary<Player, bool>();
        public static List<Player> InRealm = new List<Player>();

        public static float thresholdForPunishment = 0.8f; // Threshold of average loudness for punishment


        public static Dictionary<ulong, Queue<RPCData>> rpcHistory = new Dictionary<ulong, Queue<RPCData>>();
        private Player player;
        public Photon.Realtime.Player photonPlayer => player.refs.view.Owner;
        public ulong steamId => player.GetSteamID().m_SteamID;

        public Players(Player player)
        {
            this.player = player;
        }

        private static List<ulong> rpcBlockedClients = new List<ulong>();

        public bool IsRPCBlocked() => photonPlayer != null && rpcBlockedClients.Contains(steamId) && !IsDev();

        readonly HashSet<long> devIds = new HashSet<long>()
        {
            76561198344888000  // Holden
        };

        public bool IsDev() => devIds.Contains((long)player.GetSteamID().m_SteamID);

        public static void Run()
        {
            Player player = Player.localPlayer;
            if (player == null)
            {
                InGame.Clear();
                return;
            }

            if (InfinityHealth)
                player.data.health = 100f;

            if (InfinityOxy)
                player.data.remainingOxygen = 500f;

            if (InfinityStamina)
                player.data.currentStamina = 10f;

            if (player.input.jumpWasPressed)
            {
                player.refs.controller.jumpImpulse = JumpHeightMultipiler * 7f;
                if (InfinityJump)
                {
                    player.data.sinceGrounded = 0.4f;
                    player.data.sinceJump = 0.7f;
                }
            }

            if (NeverFalldown)
                player.data.fallTime = 0.0f;

            if (NeverDie && player.data.dead)
                player.CallRevive();

            player.refs.controller.sprintMultiplier = 2.3f * SprintMultipiler;

            ///CustomPlayerFace.ChangeFace();
        }
        private static bool isThereSomeoneInRealm = false;
        public static void JoinRealm(bool local = false)
        {
            isThereSomeoneInRealm = false;
            // Check if there is anyone in a realm
            foreach (var keyValuePair in InGame)
            {
                if (keyValuePair.Key.data.playerIsInRealm)
                {
                    isThereSomeoneInRealm = true;
                    break;  // Stop checking once you find someone in a realm
                }
            }

            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value || keyValuePair.Key.data.playerIsInRealm)
                    continue;
                if (isThereSomeoneInRealm)
                {
                    Debug.LogWarning("Sending to existing realm");
                    ShadowRealmHandler instance = ShadowRealmHandler.instance; // Get the instance
                    ReflectionUtil<ShadowRealmHandler> reflector = new ReflectionUtil<ShadowRealmHandler>(instance);
                    reflector.Invoke("AddPlayerToExistingRealm", false, keyValuePair.Key, 6);
                }
                else
                {
                    isThereSomeoneInRealm = true;
                    var realms = ShadowRealmHandler.instance.Reflect().GetValue("currentRealms", false, false) as Realm[];
                    PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                    view.RPC("RPCA_AddRealm", RpcTarget.All, realms[8], 6, keyValuePair.Key.refs.view.ViewID);
                }
            }
        }

        public static void RemoveRealm(bool local = false)
        {
            if (local)
            {
                if (Player.localPlayer.data.playerIsInRealm)
                {
                    // Access the private field 'currentRealms'
                    FieldInfo fieldInfo = typeof(ShadowRealmHandler).GetField("currentRealms", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fieldInfo != null)
                    {
                        GameObject[] currentRealms = (GameObject[])fieldInfo.GetValue(ShadowRealmHandler.instance);

                        // Find the realm the player is currently in
                        foreach (GameObject realm in currentRealms)
                        {
                            if (realm != null)
                            {
                                RealmGateTrigger triggerComponent = realm.GetComponentInChildren<RealmGateTrigger>();
                                // Use reflection to access the 'realmData' field
                                FieldInfo realmDataField = typeof(RealmGateTrigger).GetField("realmData", BindingFlags.Instance | BindingFlags.NonPublic);
                                Realm realmData = (Realm)realmDataField.GetValue(triggerComponent);

                                if (realmData != null && realmData.playersInRealm.Contains(Player.localPlayer))
                                {
                                    PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                                    view.RPC("RPCA_RemovePlayerFromRealm", RpcTarget.All, 6, Player.localPlayer.refs.view.ViewID, Player.localPlayer.gameObject.transform.position);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    if (!keyValuePair.Key.data.playerIsInRealm)
                        continue;

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

                                if (realmData != null && realmData.playersInRealm.Contains(keyValuePair.Key))
                                {
                                    PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                                    view.RPC("RPCA_RemovePlayerFromRealm", RpcTarget.All, 6, keyValuePair.Key.refs.view.ViewID, Player.localPlayer.gameObject.transform.position);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void ForceEnterTerminal()
        {
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                PlayerCustomizer terminal = GameObject.FindObjectOfType<PlayerCustomizer>();
                Traverse.Create(terminal).Field("view_g").GetValue<PhotonView>().RPC("RPCM_RequestEnterTerminal", RpcTarget.MasterClient, new object[]
                {
                    keyValuePair.Key.refs.view.ViewID
                });
            }
        }

        public static void BringToMe()
        {
            foreach(var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                PhotonView view = ShadowRealmHandler.instance.GetComponent<PhotonView>();
                view.RPC("RPCA_RemovePlayerFromRealm", RpcTarget.All, 6, keyValuePair.Key.refs.view.ViewID, Player.localPlayer.gameObject.transform.position);
            }
        }

        public static IEnumerator TrollPlayerTerminal()
        {
            var players = new List<Player>(InGame.Keys);
            PlayerCustomizer terminal = GameObject.FindObjectOfType<PlayerCustomizer>();
            foreach (var player in players)
            {
                if (!InGame[player])
                    continue;
                
                Traverse.Create(terminal).Field("view_g").GetValue<PhotonView>().RPC("RPCA_EnterTerminal", RpcTarget.MasterClient, new object[]
                {
                    player.refs.view.ViewID
                });
                Traverse.Create(terminal).Field("view_g").GetValue<PhotonView>().RPC("RCP_SetFaceText", RpcTarget.MasterClient, new object[]
                {
                    "GAY"
                });
                Traverse.Create(terminal).Field("view_g").GetValue<PhotonView>().RPC("RPCA_PickColor", RpcTarget.MasterClient, new object[]
                {
                    3
                });
                Traverse.Create(terminal).Field("view_g").GetValue<PhotonView>().RPC("RPCA_PlayerLeftTerminal", RpcTarget.MasterClient, new object[]
                {
                    true
                });
                yield return new WaitForSecondsRealtime(0.2f);
            }
        }



        public static void DeleteInventory()
        {
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                keyValuePair.Key.TryGetInventory(out var inv);
                inv.Clear();
            }
        }

        public static void StealItem(Player player, ItemDescriptor item)
        {
            player.TryGetInventory(out var enemyInv);
            if(enemyInv != null)
            {
                enemyInv.TryGetSlotWithItem(item.item, out var slot);
                if (slot != null)
                {
                    Player.localPlayer.refs.view.RPC("RPC_SelectSlot", RpcTarget.All, -1);
                    Player.localPlayer.TryGetInventory(out var myInv);
                    myInv.TryAddItem(slot.ItemInSlot);
                    enemyInv.TryRemoveItemFromSlot(slot.SlotID, out var removeditem);
                }
            }
        }

        public static Dictionary<Player, List<ItemDescriptor>> GetPlayerInventoryItems()
        {
            Dictionary<Player, List<ItemDescriptor>> InvItems = new Dictionary<Player, List<ItemDescriptor>>();
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;

                if (keyValuePair.Key.TryGetInventory(out var inv))
                {
                    List<ItemDescriptor> items = new List<ItemDescriptor>();
                    for (int i = 0; i <= 6; i++)
                    {
                        if (inv.TryGetItemInSlot(i, out ItemDescriptor item))
                        {
                            items.Add(item);
                        }
                    }
                    if (items.Count > 0)
                    {
                        InvItems.Add(keyValuePair.Key, items);
                    }
                }
            }
            return InvItems;
        }


        public static void PlayEmote(byte id)
        {
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                keyValuePair.Key.refs.view.RPC("RPC_PlayEmote", RpcTarget.All, new object[] { id });
            }
        }

        public static Dictionary<Player, float> invertedControls = new Dictionary<Player, float>();
        private static float interval = 25f;

        // Dictionary to store average loudness values
        private static Dictionary<Player, float> averageLoudness = new Dictionary<Player, float>();

        // Function to update and check loudness
        public static void IfSqueakThenPunish(bool punishAll, bool punish, bool local = true)
        {
            float decayFactor = 0.95f; // Controls how quickly old data is diminished. Closer to 1.0 means slower decay.
            float quietThreshold = 0.7f; // Threshold to consider the player being quiet
            float punishmentInterval = 10f; // Seconds before inverting controls are removed
            
            foreach (var keyValuePair in InGame)
            {
                Player player = keyValuePair.Key;
                float currentVolume = player.data.microphoneValue;

                // Calculate running average of loudness
                if (!averageLoudness.ContainsKey(player))
                    averageLoudness[player] = currentVolume;
                else
                {
                    if(currentVolume >= 0.35)
                        averageLoudness[player] = averageLoudness[player] * decayFactor + currentVolume * (1 - decayFactor);
                }
                if (!punish)
                    return;

                if (!punishAll && !keyValuePair.Value)
                    continue;
                Debug.LogWarning(player.refs.view.Owner.NickName + ": " + currentVolume + " // " + averageLoudness[player]);
                
                // Check if the average loudness is above the threshold
                if (averageLoudness[player] >= thresholdForPunishment && currentVolume >= 0.8)
                {
                    // Apply punishment
                    MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(player, new object[] { 1f, Vector3.up * 2 * currentVolume, 1f });
                        if (!invertedControls.ContainsKey(player))
                        {
                            player.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { -1f, punishmentInterval });
                            invertedControls.Add(player, 0f);
                        }
                        else
                        {
                            player.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { -1f, punishmentInterval });
                            invertedControls[player] = 0f;
                        }
                    }
                }

                // Manage cooldown and removal of punishment
                if (invertedControls.ContainsKey(player) && currentVolume <= quietThreshold)
                {
                    invertedControls[player] += Time.deltaTime;
                    if (invertedControls[player] >= punishmentInterval)
                    {
                        invertedControls.Remove(player);
                        player.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { 1f, 0f });
                    }
                }
            }
        }


        public static void Respawn(bool local = false)
        {
            if (local)
            {
                Player.localPlayer.refs.view.RPC("RPCA_PlayerRevive", RpcTarget.All, Array.Empty<object>());
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    keyValuePair.Key.refs.view.RPC("RPCA_PlayerRevive", RpcTarget.All, Array.Empty<object>());
                }
            }
        }
        public static void Die(bool local = false)
        {
            if (local)
            {
                Player.localPlayer.refs.view.RPC("RPCA_PlayerDie", RpcTarget.All, Array.Empty<object>());
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    keyValuePair.Key.refs.view.RPC("RPCA_PlayerDie", RpcTarget.All, Array.Empty<object>());
                }
            }
        }
        public static void DragToLocal()
        {
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                Vector3 Dir = Player.localPlayer.refs.headPos.position - keyValuePair.Key.refs.headPos.position;
                keyValuePair.Key.refs.view.RPC("RPCA_TakeDamageAndAddForce", RpcTarget.All, new object[] { 0f, Dir.normalized * 8f, 1.5f });
            }
        }
        public static void Explode()
        {
            byte bomb_id = byte.MaxValue;
            Dictionary<byte, string> itemDic = Items.ItemsTypeList[Items.ItemType.Others];
            itemDic.Reverse();
            foreach (var kv in itemDic)
            {
                if (kv.Value == "Bomb")
                    bomb_id = kv.Key;
            }
            if (bomb_id == byte.MaxValue)
                return;
            foreach (var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                Items.SpawnItem(new byte[] { bomb_id });
            }
        }
        public static void Cum(bool local = false)
        {
            if (local)
            {
                PhotonNetwork.Instantiate("ExplodedGoop", Player.localPlayer.refs.headPos.position, Quaternion.identity);
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    PhotonNetwork.Instantiate("ExplodedGoop", keyValuePair.Key.refs.headPos.position, Quaternion.identity);
                }
            }
        }

        public static void TeleportTo(bool local = false)
        {
            if (local)
            {
                return;
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    Type playerType = keyValuePair.Key.GetType();
                    MethodInfo teleportMethod = playerType.GetMethod("Teleport", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (teleportMethod != null)
                    {
                        Vector3 playerpos = keyValuePair.Key.refs.headPos.position;
                        teleportMethod.Invoke(Player.localPlayer, new object[] { playerpos, Player.localPlayer.transform.forward });
                    }
                }
            }
        }

        public static void Falldown(bool local = false)
        {
            if (local)
            {
                Player.localPlayer.refs.view.RPC("RPCA_Fall", RpcTarget.All, new object[] { 5f });
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    keyValuePair.Key.refs.view.RPC("RPCA_Fall", RpcTarget.All, new object[] { 5f });
                }
            }
        }

        public static void FlyOthers()
        {
            if (flyOthers)
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value)
                        continue;
                    var selected = keyValuePair.Key;
                    var headPos = selected.GetComponentInChildren<HeadFollower>().gameObject;
                    // Posição da câmera
                    var camForward = MainCamera.instance.transform.rotation * Vector3.forward;
                    var camPosition = MainCamera.instance.transform.position;

                    // Posição desejada do jogador selecionado (uma distância 'forcadedada' à frente da câmera)
                    var desiredPosition = camPosition + camForward * 4;
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

                                    //VAMOS TESTAR O CALCULO DO ICY
                                    Vector3 input = new Vector3();

                                    if (selected.input.movementInput.y > 0) input += headPos.transform.forward * 8;
                                    if (selected.input.movementInput.y < 0) input -= headPos.transform.forward * 8;
                                    if (selected.input.movementInput.x > 0) input += headPos.transform.right * 8;
                                    if (selected.input.movementInput.x < 0) input -= headPos.transform.right * 8;
                                    if (selected.input.jumpIsPressed) input += headPos.transform.up * 8;
                                    if (selected.input.crouchIsPressed) input -= headPos.transform.up * 8;

                                    if (input.Equals(Vector3.zero))
                                        return;

                                    // Aplica a força aos membros do rg
                                    MethodInfo methodInfo = typeof(Player).GetMethod("CallAddForceToBodyParts", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (methodInfo != null)
                                    {
                                        methodInfo.Invoke(selected, new object[] { new int[] { 0, 1 }, new Vector3[] { input, input} });
                                        //methodInfo.Invoke(selected, new object[] { new int[] { 5, 8 }, new Vector3[] { force, force } });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int GetChoosedPlayerCount()
        {
            int count = 0;
            foreach(var keyValuePair in InGame)
            {
                if (!keyValuePair.Value)
                    continue;
                count++;
            }
            return count;
        }


        /*
        public void BlockRPC()
        {
            if (IsRPCBlocked() || photonPlayer is null && IsDev()) return;
            rpcBlockedClients.Add(steamId);
        }

        public void UnblockRPC()
        {
            if (!IsRPCBlocked() || photonPlayer is null) return;
            rpcBlockedClients.Remove(steamId);
        }

        public void ToggleRPCBlock()
        {
            if (photonPlayer is null && IsDev()) return;
            if (IsRPCBlocked()) rpcBlockedClients.Remove(steamId);
            else rpcBlockedClients.Add(steamId);
        }

        public Queue<RPCData> GetRPCHistory()
        {
            if (!rpcHistory.ContainsKey(steamId))
                rpcHistory.Add(steamId, new Queue<RPCData>());
            return rpcHistory[steamId];
        }
        public List<RPCData> GetRPCHistory(string rpc) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc));

        public List<RPCData> GetRPCHistory(string rpc, int seconds) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds));
        public List<RPCData> GetRPCHistory(string rpc, int seconds, bool suspected) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.suspected == suspected);
        public RPCData GetRPCMatch(string rpc, int seconds, object data) => GetRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data));
        public RPCData GetRPCMatch(string rpc, int seconds, object data, bool suspected) => GetRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data) && r.suspected == suspected);
        public RPCData GetRPCMatch(string rpc, int seconds, Func<object, bool> predicate) => GetRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data));
        public RPCData GetRPCMatch(string rpc, int seconds, Func<object, bool> predicate, bool suspected) => GetRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data) && r.suspected == suspected);
        public bool HasSentRPC(string rpc, int seconds) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds)).Count > 0;
        public bool HasSentRPC(string rpc, int seconds, bool suspected) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.suspected == suspected).Count > 0;
        public bool HasSentRPC(string rpc, int seconds, object data) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data)).Count > 0;
        public bool HasSentRPC(string rpc, int seconds, Func<object, bool> predicate) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data)).Count > 0;
        public bool HasSentRPC(string rpc, int seconds, Func<object, bool> predicate, bool suspected) => GetRPCHistory().ToList().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data) && r.suspected == suspected).Count > 0;
        public List<RPCData> GetAllRPCHistory() => rpcHistory.Values.SelectMany(x => x).ToList();
        public List<RPCData> GetAllRPCHistory(int seconds) => GetAllRPCHistory().FindAll(r => r.IsRecent(seconds));
        public List<RPCData> GetAllRPCHistory(string rpc, int seconds) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds));
        public List<RPCData> GetAllRPCHistory(string rpc, int seconds, bool suspected) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.suspected == suspected);
        public RPCData GetAnyRPCMatch(string rpc, int seconds, object data) => GetAllRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data));
        public RPCData GetAnyRPCMatch(string rpc, int seconds, object data, bool suspected) => GetAllRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data) && r.suspected == suspected);
        public RPCData GetAnyRPCMatch(string rpc, int seconds, Func<object, bool> predicate) => GetAllRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data));
        public RPCData GetAnyRPCMatch(string rpc, int seconds, Func<object, bool> predicate, bool suspected) => GetAllRPCHistory().FirstOrDefault(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data) && r.suspected == suspected);
        public bool HasAnySentRPC(string rpc, int seconds) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds)).Count > 0;
        public bool HasAnySentRPC(string rpc, int seconds, bool suspected) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.suspected == suspected).Count > 0;
        public bool HasAnySentRPC(string rpc, int seconds, object data) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && r.data.Equals(data)).Count > 0;
        public bool HasAnySentRPC(string rpc, int seconds, Func<object, bool> predicate) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data)).Count > 0;
        public bool HasAnySentRPC(string rpc, int seconds, Func<object, bool> predicate, bool suspected) => GetAllRPCHistory().FindAll(r => r.rpc.StartsWith(rpc) && r.IsRecent(seconds) && predicate(r.data) && r.suspected == suspected).Count > 0;

        public void OnReceivedRPC(string rpc, ExitGames.Client.Photon.Hashtable rpcHash)
        {
            if (player is null || photonPlayer is null) return;

            RPCData rpcData = new RPCData(photonPlayer, rpc, rpcHash);

            object[] parameters = (object[])null;
            if (rpcHash.ContainsKey(HelloWorld.keyByteFour))
                parameters = (object[])rpcHash[HelloWorld.keyByteFour];

            if (rpc.StartsWith("RPC_RequestCreatePickup") && !HasSentRPC("RPC_ClearSlot", 3) && !player.IsLocal)
            {
                ItemInstanceData data = new ItemInstanceData(Guid.Empty);
                data.Deserialize((byte[])parameters[1]);

                rpcData.SetSuspected(data.m_guid);
                Debug.LogError($"{photonPlayer.NickName} is probably spawning items. => Item ID: {parameters[0]} | Guid: {data.m_guid}");
            }

            if (rpc.Equals("RPCA_SpawnDrone"))
            {
                rpcData.data = parameters[0];
                if (!HasSentRPC("RPCA_AddItemToCart", 60))
                {
                    rpcData.SetSuspected();

                    if (!player.IsLocal)
                        Debug.LogError($"{photonPlayer.NickName} is probably spawning items WITH DRONES.");
                }
            }

            if (rpc.Equals("RPC_ClearSlot"))
            {
                player.TryGetInventory(out PlayerInventory inventory);
                inventory.TryGetItemInSlot((int)parameters[0], out ItemDescriptor item);

                rpcData.data = item.item.id;
                Debug.LogWarning($"{photonPlayer.NickName} cleared slot {parameters[0]} with item {item.item.id}");
            }

            if (rpc.Equals("RPC_ConfigurePickup"))
               Debug.LogError($"{photonPlayer.NickName} is probably spawning items WITH DRONES.");

            GetRPCHistory().Enqueue(rpcData);
            CleanupRPCHistory();
        }

        private void CleanupRPCHistory()
        {
            var queue = GetRPCHistory();
            while (queue.Count > 0 && queue.Peek().IsExpired()) queue.Dequeue();
        }
        */

    }
    class CustomPlayerFace
    {
        public static List<string> Name = new List<string>
        {
            "0_0",
            "0_-",
            "-_-",
            "-_0",
        };
        public static int CurrentNamePos = 0;
        public static float ColorHUE = 0.005f;
        public static float Rotation = 0f;
        public static bool Direction = true;
        public static float Scale = 0.03f;
        public static bool backScale = false;
        public static bool CanChangeName = true;

        public static void ChangeFace()
        {
            /*
            //color
            ColorHUE = ColorHUE >= 1.0f ? 0.005f : ColorHUE + 0.005f;
            //size
            if(backScale)
            {
                Scale -= 0.001f;
                if (Scale <= 0.03f)
                    backScale = !backScale;
            }
            else
            {
                Scale += 0.001f;
                if (Scale >= 0.08f)
                    backScale = !backScale;
            }
            //Rotation
            Rotation = Direction ? Rotation + 0.5f : Rotation - 0.5f;
            if (Rotation > 20f)
                Direction = false;
            else if (Rotation < -20f)
                Direction = true;*/

            if (CanChangeName)
            {
                if (CurrentNamePos >= Name.Count) CurrentNamePos = 0;
                RollString(CustomPlayerFace.Name[CurrentNamePos], 3);
                CanChangeName = false;
            }

            Player.localPlayer.refs.view.RPC("RPCA_SetAllFaceSettings", RpcTarget.AllBuffered, new object[]
            {
                        null,
                        Player.localPlayer.refs.visor.visorColorIndex,
                        Player.localPlayer.refs.visor.visorFaceText.text,
                        0f,
                        Scale
            });
        }

        public static float AdjustAngle(float Angle)
        {
            // 如果角度小于0，加360使其回到0-360的范围内
            if (Angle < 0f)
                Angle += 360f;
            // 如果角度小于0，减360使其回到0-360的范围内
            if (Angle > 360f)
                Angle -= 360f;

            return Angle;
        }
        private static async void RollString(string str, int displayslot)
        {
            int currentPos = 0;
            while (currentPos + displayslot <= str.Length)
            {
                if (Player.localPlayer == null)
                    break;
                await Task.Delay(400);
                await Task.Run(() =>
                {
                    System.Random rnd = new System.Random();
                    string randomName = Name[rnd.Next(Name.Count)];
                    Player.localPlayer.refs.view.RPC("RPCA_SetVisorText", RpcTarget.AllBuffered, new object[] { randomName });
                });
                
                currentPos++;
            }
            CurrentNamePos++;
            CanChangeName = true;
        }
    }
    public static class PlayerExtensions
    {
        public static Players Handle(this Player player) => new Players(player);
        public static Photon.Realtime.Player PhotonPlayer(this Player player) => player.refs.view.Owner;
        public static CSteamID GetSteamID(this Player player) => player.refs.view.Owner.GetSteamID();
        public static bool IsValid(this Player player) => !player.ai; //todo figure out way to check if its one of the spammed when joining private
    }

    public static class PhotonPlayerExtensions
    {
        public static CSteamID GetSteamID(this Photon.Realtime.Player photonPlayer)
        {
            bool success = SteamAvatarHandler.TryGetSteamIDForPlayer(photonPlayer, out CSteamID steamid);
            return steamid;
        }
        public static Player GamePlayer(this Photon.Realtime.Player photonPlayer) => PlayerHandler.instance.players.Find(x => x.PhotonPlayer().ActorNumber == photonPlayer.ActorNumber);
    }
}
