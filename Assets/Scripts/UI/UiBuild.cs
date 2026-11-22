using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 시작 화면과 카드 화면처럼 코드로 UI를 만드는 곳이 함께 쓰는 도구 모음. 좌표는 모두 1920x1080 무대의 왼쪽 위가 원점이다.
    public static class UiBuild
    {
        public const float StageWidth = 1920f;
        public const float StageHeight = 1080f;

        public static Image AddImage(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text AddText(string name, RectTransform parent, Font font, int fontSize, Color color, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.raycastTarget = false;
            return text;
        }

        // 한 가지 색의 평평한 버튼. 올림·눌림 색은 색조로 낸다. 글자는 버튼 가운데에 놓인다.
        public static Image AddButton(string name, RectTransform parent, Font font, Color color, string label, int fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var image = AddImage(name, parent, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            SetButtonColors(button, color);
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var text = AddText("Label", image.rectTransform, font, fontSize, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform);
            return image;
        }

        public static void SetButtonColors(Button button, Color color)
        {
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = color;
            colors.disabledColor = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.7f);
            button.colors = colors;
        }

        public static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        public static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
        }

        // 부모의 왼쪽 위를 원점으로 하는 자리. 부모가 1920x1080 무대면 화면 좌표가 된다.
        public static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void Center(RectTransform rect, Vector2 size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        // 화면 위에 가득 차는 캔버스(1920x1080 기준, 비율이 달라도 같은 크기로 맞춘다)를 만든다.
        public static RectTransform CreateCanvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(StageWidth, StageHeight);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        // 가운데가 밝은 초록 그라데이션 위에 문양이 떠다니고, 가는 가로줄(CRT 느낌)이 덮인다. 모든 로비 화면의 배경이다.
        public static void BuildBackdrop(RectTransform root, Font font, Color inner, Color outer)
        {
            var back = AddImage("Background", root, Color.white);
            Stretch(back.rectTransform);
            back.sprite = CreateRadialSprite(inner, outer);
            back.raycastTarget = false;

            UiFxBackground.Create(root, font);

            var linesGo = new GameObject("Scanlines", typeof(RectTransform));
            linesGo.transform.SetParent(root, false);
            Stretch((RectTransform)linesGo.transform);
            var lines = linesGo.AddComponent<RawImage>();
            lines.texture = CreateScanlineTexture();
            lines.uvRect = new Rect(0f, 0f, 1f, StageHeight / 4f);
            lines.color = new Color(1f, 1f, 1f, 0.55f);
            lines.raycastTarget = false;
        }

        // 왼쪽 가운데가 밝고 가장자리가 어두운 원형 그라데이션.
        public static Sprite CreateRadialSprite(Color inner, Color outer)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = new Vector2(0.4f, 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float distance = Mathf.Clamp01(Vector2.Distance(point, center) / 0.85f);
                    texture.SetPixel(x, y, Color.Lerp(inner, outer, Mathf.SmoothStep(0f, 1f, distance)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // 네 줄마다 한 줄만 어둡게 칠한 가로줄. uvRect로 화면 높이만큼 반복해서 깐다.
        public static Texture2D CreateScanlineTexture()
        {
            var texture = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Point;
            for (int y = 0; y < 4; y++)
            {
                texture.SetPixel(0, y, y == 0 ? new Color(0f, 0f, 0f, 0.14f) : Color.clear);
            }

            texture.Apply();
            return texture;
        }
    }
}
