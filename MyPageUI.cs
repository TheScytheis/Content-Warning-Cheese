using System;
using System.Collections.Generic;
using UnityEngine;
using Zorro.UI;
using Object = UnityEngine.Object;

namespace TestUnityPlugin
{
    public class MyPageUI : MonoBehaviour
    {
        private UIPageHandler pageHandler;
        private bool hasAddedPages = false;

        private void Awake()
        {
            pageHandler = FindObjectOfType<UIPageHandler>();
        }

        private void Update()
        {
            if (!(pageHandler.currentPage is MainMenuMainPage main)) return;

            if (main.GetComponent<MainMenu>() == null)
                main.gameObject.AddComponent<MainMenu>();

            AddPages();
        }

        private void AddPages()
        {
            if (hasAddedPages) return;

            // Create a new page (MainMenuViewLobbiesPage) and add it to the canvas
            GameObject go = new GameObject("MainMenuViewLobbiesPage", typeof(RectTransform));
            go.transform.SetParent(FindObjectOfType<UIPageHandler>().transform, false);
            UIPage page = go.AddComponent<MainMenuViewLobbiesPage>();

            TryRegisterPage(page);

            hasAddedPages = true;
        }

        public static void TryAttachToPageHandler()
        {
            Debug.LogWarning("Attempting To Attach MyPageUI to UIPageHandler");
            UIPageHandler h = Object.FindObjectOfType<UIPageHandler>();

            if (h == null || h.gameObject.GetComponent<MyPageUI>() != null) return;

            h.gameObject.AddComponent<MyPageUI>();
        }

        public static void TryRegisterPage(UIPage page)
        {
            var handler = FindObjectOfType<UIPageHandler>();
            var pages = handler.GetType().GetMethod("Reflect").Invoke(handler, null).GetType().GetProperty("_pages").GetValue(handler, null) as Dictionary<Type, UIPage>;
            pages.Add(page.GetType(), page);
        }

        public static void TransitionToPage<T>() where T : UIPage
        {
            FindObjectOfType<UIPageHandler>().GetType().GetMethod("TransistionToPage").MakeGenericMethod(typeof(T)).Invoke(null, null);
        }
    }
}
