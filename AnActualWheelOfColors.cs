using UnityEngine;
using System;
using BepInEx;

namespace TestUnityPlugin
{
    public class ColorWheelWindow
    {
        public Rect windowRect = new Rect(20, 20, 200, 250);
        public Color selectedColor = Color.white;
        public bool showColorWheel = false;
        public Texture2D colorWheelTexture;
        public Texture2D cursorTexture;
        public Vector2 cursorSize = new Vector2(20, 20);
        private bool firstLaunch = true;
        private Vector2 lastMousePosition;
        private bool capturingColor = false;
        private Rect cursorRect;
        private Rect colorRect;
        public event Action<Color> OnColorSelected;

        public ColorWheelWindow()
        {
            colorRect = new Rect(0, 0, 0, 0);
            cursorRect = new Rect(0, 0, cursorSize.x, cursorSize.y);
        }

        public void DrawWindow()
        {
            if (colorWheelTexture == null)
            {
                GenerateColorWheelTexture();
            }

            if (cursorTexture == null)
            {
                GenerateCursorTexture();
            }

            if (showColorWheel)
            {
                windowRect = GUILayout.Window(2, windowRect, ColorWheelGUI, "Color Picker by Ron");
            }
        }

        private void ColorWheelGUI(int windowID)
        {
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Select with Right Mouse Button", style);
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            colorRect = GUILayoutUtility.GetRect(colorWheelTexture.width, colorWheelTexture.height);
            Vector2 cursorPosition = Event.current.mousePosition;

            if (capturingColor)
            {
                cursorRect.position = new Vector2(cursorPosition.x - cursorSize.x / 2, cursorPosition.y - cursorSize.y / 2);
                cursorRect.x = Mathf.Clamp(cursorRect.x, colorRect.x, colorRect.xMax - cursorRect.width);
                cursorRect.y = Mathf.Clamp(cursorRect.y, colorRect.y, colorRect.yMax - cursorRect.height);
                selectedColor = GetColorUnderMouse(colorRect);
                UpdateCursorTexture(selectedColor);
                ColorUpdate();
                //SelectColor(selectedColor);
            }

            GUI.DrawTexture(colorRect, colorWheelTexture);

            if (colorRect.Contains(Event.current.mousePosition))
            {
                if (Input.GetMouseButton(0))
                {
                    capturingColor = true;
                    lastMousePosition = Event.current.mousePosition;
                }
                else
                {
                    capturingColor = false;
                }
            }

            GUI.DrawTexture(cursorRect, cursorTexture);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK"))
            {
                showColorWheel = false;
                SelectColor(selectedColor);
            }
            if (GUILayout.Button("Cancel"))
            {
                showColorWheel = false;
            }
            GUILayout.EndHorizontal();
        }

        public void SelectColor(Color color)
        {
            Debug.Log("Cor selecionada: " + color);
            OnColorSelected?.Invoke(color);
        }

        private void GenerateColorWheelTexture()
        {
            int texSize = 256;
            colorWheelTexture = new Texture2D(texSize, texSize);

            for (int y = 0; y < texSize; y++)
            {
                for (int x = 0; x < texSize; x++)
                {
                    float hue = Mathf.Atan2(y - texSize / 2, x - texSize / 2);
                    hue = ((hue / Mathf.PI) + 1.0f) * 0.5f;
                    float saturation = Mathf.Sqrt((x - texSize / 2) * (x - texSize / 2) + (y - texSize / 2) * (y - texSize / 2)) / (texSize / 2);
                    colorWheelTexture.SetPixel(x, y, Color.HSVToRGB(hue, saturation, 1));
                }
            }

            colorWheelTexture.Apply();
        }

        private void GenerateCursorTexture()
        {
            cursorTexture = new Texture2D((int)cursorSize.x, (int)cursorSize.y);

            float centerX = cursorSize.x / 2f;
            float centerY = cursorSize.y / 2f;
            float radius = cursorSize.x / 2f;

            UpdateCursorTexture(selectedColor);

            cursorTexture.Apply();
        }

        public Color ColorUpdate() // metodo de update pra atualizar a cor em tempo real sem clicar em ok, usado na instancia de outra classe
        {
            return selectedColor;
        }

        private void UpdateCursorTexture(Color color)
        {
            float centerX = cursorSize.x / 2f;
            float centerY = cursorSize.y / 2f;
            float radius = cursorSize.x / 2f;

            for (int y = 0; y < cursorSize.y; y++)
            {
                for (int x = 0; x < cursorSize.x; x++)
                {
                    float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));

                    if (distance > radius)
                        cursorTexture.SetPixel(x, y, Color.clear); // deixa transparente fora do círculo
                    else if (distance > radius * 0.7f)
                    {
                        cursorTexture.SetPixel(x, y, Color.black); // anel preto ao redor
                    }
                    else
                    {
                        cursorTexture.SetPixel(x, y, color); // cor selecionada dentro do círculo
                    }
                }
            }

            cursorTexture.Apply();
        }

        private Color GetColorUnderMouse(Rect colorRect)
        {
            Vector2 mousePos = Event.current.mousePosition;
            Vector2 pos = cursorRect.position;
            Vector2 localPoint = pos - new Vector2(colorRect.x, colorRect.y); //mousePos - new Vector2(colorRect.x, colorRect.y); codigo antigo caso dê erro
            Vector2 uv = new Vector2(localPoint.x / colorRect.width, 1f - (localPoint.y / colorRect.height));
            Color color = colorWheelTexture.GetPixelBilinear(uv.x, uv.y);
            return color;
        }
    }
}