using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TestUnityPlugin
{
    internal class MainMenuViewLobbiesPage : MainMenuPage
    {
        private GameObject canvasGo;
        private Canvas canvas;
        private float screenWidth;
        private float screenHeight;
        private GameObject layoutGroupGO;
        private Button backBtn;

        void Awake()
        {
            canvasGo = FindAnyObjectByType<MainMenuHandler>().gameObject;
            canvas = FindObjectOfType<Canvas>();
            screenHeight = canvas.pixelRect.height;
            screenWidth = canvas.pixelRect.width;

            //anchor the transform to top center
            GetComponent<RectTransform>().anchoredPosition = new Vector2(0.5f, 0);

            // Lets add a background image that fits the whole screen with a color setting of 0 0 0 0.9412
            GameObject backgroundImage = new GameObject("BackgroundImage");
            backgroundImage.transform.SetParent(this.transform, false);
            Image bgImage = backgroundImage.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.9412f);
            RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
            bgRect.localScale = new Vector2(screenWidth, screenHeight);
            bgRect.anchoredPosition = Vector2.zero;

            SetLayoutGroup();

            //add a button to go back to MainMenuMainPage
            this.gameObject.AddComponent<LobbyMenu>();
            backBtn = MainMenu.CreateBtnCopy("backBtn", "Back", 100, transform).GetComponent<Button>();
            backBtn.gameObject.transform.position = new Vector3(314.6961f, 80f, 0f);
            backBtn.onClick.AddListener(() =>
            {
                MyPageUI.TransitionToPage<MainMenuMainPage>();
            });
            RectTransform backbtnrt = backBtn.GetComponent<RectTransform>();
            backbtnrt.anchoredPosition = new Vector2(-700, - 400);  // Position it


            LobbyMenu.allBtn = MainMenu.CreateBtnCopy("allBtn", "ALL", 100, transform).GetComponent<Button>();
            LobbyMenu.allBtn.gameObject.transform.position = new Vector3(314.6961f, 900f, 0f);
            LobbyMenu.allBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.all;
            });
            LobbyMenu.allBtn.gameObject.transform.SetParent(layoutGroupGO.transform);
            //Create a button for the different categories.
            LobbyMenu.euBtn = MainMenu.CreateBtnCopy("euBtn", "EU", 100, transform).GetComponent<Button>();
            LobbyMenu.euBtn.gameObject.transform.position = new Vector3(314.6961f, 750f, 0f);
            LobbyMenu.euBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.eu;
            });
            LobbyMenu.euBtn.gameObject.transform.SetParent(layoutGroupGO.transform);

            LobbyMenu.usBtn = MainMenu.CreateBtnCopy("usBtn", "US", 100, transform).GetComponent<Button>();
            LobbyMenu.usBtn.gameObject.transform.position = new Vector3(314.6961f, 700f, 0f);
            LobbyMenu.usBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.us;
            });
            LobbyMenu.usBtn.gameObject.transform.SetParent(layoutGroupGO.transform);

            LobbyMenu.uswBtn = MainMenu.CreateBtnCopy("uswBtn", "USW", 100, transform).GetComponent<Button>();
            LobbyMenu.uswBtn.gameObject.transform.position = new Vector3(314.6961f, 650f, 0f);
            LobbyMenu.uswBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.usw;
            });
            LobbyMenu.uswBtn.gameObject.transform.SetParent(layoutGroupGO.transform);

            LobbyMenu.saBtn = MainMenu.CreateBtnCopy("saBtn", "SA", 100, transform).GetComponent<Button>();
            LobbyMenu.saBtn.gameObject.transform.position = new Vector3(314.6961f, 600f, 0f);
            LobbyMenu.saBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.sa;
            });
            LobbyMenu.saBtn.gameObject.transform.SetParent(layoutGroupGO.transform);

            LobbyMenu.asiaBtn = MainMenu.CreateBtnCopy("asiaBtn", "ASIA", 100, transform).GetComponent<Button>();
            LobbyMenu.asiaBtn.gameObject.transform.position = new Vector3(314.6961f, 550f, 0f);
            LobbyMenu.asiaBtn.onClick.AddListener(() =>
            {
                LobbyMenu.currentCategory = LobbyCategory.asia;
            });
            LobbyMenu.asiaBtn.gameObject.transform.SetParent(layoutGroupGO.transform);


            HelloWorld.LobbiesPage = this.gameObject;

            //Find the settings background and copy it


            this.gameObject.SetActive(false);
        }

        private void SetLayoutGroup()
        {
            layoutGroupGO = new GameObject("LayoutGroup");
            layoutGroupGO.transform.SetParent(this.transform, false);  // Set parent to the current menu page
            VerticalLayoutGroup layoutGroup = layoutGroupGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 50;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;

            //Setup recttransform
            RectTransform rectTransform = layoutGroupGO.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);  // Anchor top-left
            rectTransform.anchorMax = new Vector2(0, 1);  // Anchor top-left
            rectTransform.pivot = new Vector2(0, 1);  // Pivot top-left
            rectTransform.anchoredPosition = new Vector2(-850, 300);  // Position it 10 pixels from top and left
            rectTransform.sizeDelta = new Vector2(200, 0);  // Width of 200px and flexible height

            
        }

    }
}
