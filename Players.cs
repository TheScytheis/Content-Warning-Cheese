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
using Steamworks;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using static HelperFunctions;
using static UnityEngine.GraphicsBuffer;

namespace TestUnityPlugin
{
    internal class Players
    {
        public static bool ifScreamingThenPunish = false;
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
        public static void JoinRealm(bool local = false)
        {
            if (local)
            {
                ShadowRealmHandler.instance.TeleportPlayerToRandomRealm(Player.localPlayer);
            }
            else
            {
                foreach (var keyValuePair in InGame)
                {
                    if (!keyValuePair.Value || keyValuePair.Key.data.playerIsInRealm)
                        continue;
                    ShadowRealmHandler.instance.TeleportPlayerToRandomRealm(keyValuePair.Key);
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
                                    // Access the private method 'PlayerLeaveRealm'
                                    MethodInfo methodInfo = typeof(ShadowRealmHandler).GetMethod("PlayerLeaveRealm", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (methodInfo != null)
                                    {
                                        methodInfo.Invoke(ShadowRealmHandler.instance, new object[] { Player.localPlayer, realm });
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

                                if (realmData != null && realmData.playersInRealm.Contains(keyValuePair.Key))
                                {
                                    // Access the private method 'PlayerLeaveRealm'
                                    MethodInfo methodInfo = typeof(ShadowRealmHandler).GetMethod("PlayerLeaveRealm", BindingFlags.NonPublic | BindingFlags.Instance);
                                    if (methodInfo != null)
                                    {
                                        methodInfo.Invoke(ShadowRealmHandler.instance, new object[] { keyValuePair.Key, realm });
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

        public static void TrollPlayerTerminal()
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

        public static void IfSqueakThenPunish(bool local = true)
        {
            foreach (var keyValuePair in InGame)
            {
                if (keyValuePair.Key.data.microphoneValue >= 0.97f)
                {
                    MethodInfo methodInfo = typeof(Player).GetMethod("CallTakeDamageAndAddForceAndFall", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (methodInfo != null)
                    {
                        methodInfo.Invoke(keyValuePair.Key, new object[] { 1f, Vector3.up * 10 * keyValuePair.Key.data.microphoneValue, 1f });
                        if (!invertedControls.ContainsKey(keyValuePair.Key))
                        {
                            keyValuePair.Key.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { -1f, 10f });
                            invertedControls.Add(keyValuePair.Key, 0f);
                        }
                        else if (invertedControls.ContainsKey(keyValuePair.Key))
                        {
                            keyValuePair.Key.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { -1f, 10f });
                            invertedControls[keyValuePair.Key] = 0f;
                        }
                    }
                }
                if(invertedControls.ContainsKey(keyValuePair.Key) && keyValuePair.Key.data.microphoneValue <= 0.7)
                {
                    invertedControls[keyValuePair.Key] += Time.deltaTime;
                }
                if(invertedControls.ContainsKey(keyValuePair.Key) && invertedControls[keyValuePair.Key] >= interval)
                {
                    invertedControls.Remove(keyValuePair.Key);
                    keyValuePair.Key.refs.view.RPC("RPCA_SlowFor", RpcTarget.All, new object[] { 1f, 0f });
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
}
