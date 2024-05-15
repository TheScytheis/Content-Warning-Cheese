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
            Debug.Log("ATTEMPTING TO ADD A PAGE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            MyPageUI.TryRegisterPage(page);

            hasAddedPages = true;
            Debug.Log("ADDED PAGES!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
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
            Debug.LogWarning("TRYING TO REGISTER PAGE");
            var handler = FindObjectOfType<UIPageHandler>();
            if (handler == null) return;
            Debug.LogWarning("Handler passed!");

            // Accessing the private or protected field "_pages"
            var pages = handler.Reflect().GetValue("_pages") as Dictionary<Type, UIPage>;

            if (pages == null) {
                Debug.Log("PAGES WAS NULL");
                return;
            }
            Debug.LogWarning("adding pages!!");

            pages.Add(page.GetType(), page);
            Debug.LogWarning("pages added!!!");

        }

        public static void TransitionToPage<T>() where T : UIPage
        {
            FindObjectOfType<UIPageHandler>().TransistionToPage<T>();
        }
    }
}
