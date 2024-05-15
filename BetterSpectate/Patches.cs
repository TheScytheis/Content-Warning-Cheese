using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TestUnityPlugin.BetterSpectate
{
    internal class SpectatePatch
    {
        // Token: 0x06000006 RID: 6 RVA: 0x00002144 File Offset: 0x00000344
        private static void AddPlayerToDeadPanel(Player player)
        {
            Transform transform = SpectatePatch.panel.transform.Find("allDead");
            Transform transform2 = transform.Find("template");
            Transform transform3 = UnityEngine.Object.Instantiate<Transform>(transform2, transform);
            transform3.gameObject.SetActive(true);
            transform3.GetComponent<Image>().sprite = Utils.GetAvatar(player);
            bool flag = (double)player.data.microphoneValue >= 0.275;
            if (flag)
            {
                Transform transform4 = transform3.Find("speak");
                transform4.gameObject.SetActive(true);
            }
        }

        // Token: 0x06000007 RID: 7 RVA: 0x000021E4 File Offset: 0x000003E4
        private static List<Player> getDeadPlayers()
        {
            List<Player> list = new List<Player>();
            List<Player> players = PlayerHandler.instance.players;
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                bool dead = player.data.dead;
                if (dead)
                {
                    list.Add(player);
                }
            }
            return list;
        }

        // Token: 0x06000008 RID: 8 RVA: 0x00002248 File Offset: 0x00000448
        private static void RefreshPlrList()
        {
            Transform transform = SpectatePatch.panel.transform.Find("allDead");
            foreach (object obj in transform.transform)
            {
                Transform transform2 = (Transform)obj;
                bool flag = transform2.gameObject.name == "template";
                if (!flag)
                {
                    UnityEngine.Object.Destroy(transform2.gameObject);
                }
            }
            for (int i = 0; i < SpectatePatch.getDeadPlayers().Count; i++)
            {
                Player player = SpectatePatch.getDeadPlayers()[i];
                SpectatePatch.AddPlayerToDeadPanel(player);
            }
        }

        // Token: 0x06000009 RID: 9 RVA: 0x00002318 File Offset: 0x00000518
        [HarmonyPatch(typeof(Spectate), "StartSpectate")]
        public static class PatchStartSpectate
        {
            [HarmonyPostfix]
            private static void StartPatch()
            {
                GameObject gameObject = HelloWorld.assets.LoadAsset<GameObject>("SpectateUI");
                bool flag = gameObject == null;
                if (flag)
                {
                    HelloWorld.Log.LogFatal("SpectateUI Panel is non-existant.");
                }
                else
                {
                    SpectatePatch.panel = UnityEngine.Object.Instantiate<GameObject>(gameObject, GameObject.Find("GAME").transform);
                    SpectatePatch.panel = SpectatePatch.panel.transform.Find("Panel").gameObject;
                    for (int i = 0; i < SpectatePatch.getDeadPlayers().Count; i++)
                    {
                        Player player = SpectatePatch.getDeadPlayers()[i];
                        SpectatePatch.AddPlayerToDeadPanel(player);
                    }
                }
            }
        }


        // Token: 0x0600000A RID: 10 RVA: 0x000023BC File Offset: 0x000005BC
        [HarmonyPatch(typeof(Spectate), "Update")]
        public static class PatchUpdateSpectate
        {
            [HarmonyPostfix]
            private static void UpdatePatch(Spectate __instance)
            {
                bool flag = SpectatePatch.panel == null || !Spectate.spectating;
                if (!flag)
                {
                    SpectatePatch.RefreshPlrList();
                    Camera camera = __instance.gameObject.GetComponent<Camera>();
                    bool flag2 = !camera;
                    if (flag2)
                    {
                        camera = __instance.gameObject.GetComponentInChildren<Camera>();
                    }
                    bool flag3 = Input.mouseScrollDelta.y > 0f;
                    if (flag3)
                    {
                        camera.fieldOfView = Mathf.Clamp(camera.fieldOfView -= 5f, 25f, 180f);
                    }
                    bool flag4 = Input.mouseScrollDelta.y < 0f;
                    if (flag4)
                    {
                        camera.fieldOfView = Mathf.Clamp(camera.fieldOfView += 5f, 25f, 180f);
                    }
                }
            }
        }

        // Token: 0x04000009 RID: 9
        private static GameObject panel;
    }
}
