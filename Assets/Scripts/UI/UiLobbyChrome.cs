using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 로비 화면(시작 화면, 카드 화면)이 함께 쓰는 위쪽 줄과 아래쪽 탭 줄. 프로토타입의 topbar / tabbar를 따랐다.
    public static class UiLobbyChrome
    {
        public const float TopBarHeight = 84f;
        public const float TabBarHeight = 96f;
        private const float Overflow = 1500f;
        private const float TabWidth = 384f;

        private static readonly Color BarColor = new Color(0.008f, 0.045f, 0.035f, 0.94f);
        private static readonly Color LineColor = new Color(0.18f, 0.34f, 0.29f, 1f);
        private static readonly Color LogoColor = new Color(0.56f, 0.63f, 0.6f, 1f);
        private static readonly Color MutedColor = new Color(0.62f, 0.76f, 0.7f, 1f);
        private static readonly Color GoldColor = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color GrayButton = new Color(0.247f, 0.298f, 0.278f, 1f);

        public static string[] TabIds { get; } = { "home", "cards", "deck" };
        public static string[] TabLabels { get; } = { "홈", "카드", "덱" };
        private static readonly string[] TabScenes = { "Title", "Cards", "Deck" };

        // 탭 번호에 해당하는 씬 이름. 모든 로비 화면이 같은 표를 쓰므로 한곳에 둔다.
        public static string GetSceneName(string tabId)
        {
            int index = System.Array.IndexOf(TabIds, tabId);
            return index >= 0 ? TabScenes[index] : null;
        }

        // 왼쪽에 RUNE DUEL 로고와 화면 이름(있으면 금색), 오른쪽에 버튼 하나(있으면)를 둔 위쪽 줄을 만든다.
        public static void BuildTopBar(RectTransform stage, Font font, string logoWord, string logoSub, string pageLabel, string buttonLabel, UnityEngine.Events.UnityAction onButton)
        {
            var bar = UiBuild.AddImage("TopBar", stage, BarColor);
            UiBuild.Place(bar.rectTransform, -Overflow, 0f, UiBuild.StageWidth + Overflow * 2f, TopBarHeight);
            var line = UiBuild.AddImage("TopLine", stage, LineColor);
            UiBuild.Place(line.rectTransform, -Overflow, TopBarHeight, UiBuild.StageWidth + Overflow * 2f, 3f);

            float x = 56f;
            foreach (char letter in logoWord)
            {
                if (letter != ' ')
                {
                    var text = UiBuild.AddText("Logo", stage, font, 40, LogoColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                    text.text = letter.ToString();
                    UiBuild.Place(text.rectTransform, x, 14f, 36f, 56f);
                    UiBuild.AddShadow(text, new Color(0f, 0f, 0f, 0.6f), new Vector2(3f, -3f));
                }

                x += letter == ' ' ? 18f : 34f;
            }

            var sub = UiBuild.AddText("LogoSub", stage, font, 20, MutedColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            sub.text = logoSub;
            UiBuild.Place(sub.rectTransform, x + 12f, 30f, 100f, 28f);
            x += 12f + sub.preferredWidth + 24f;

            if (!string.IsNullOrEmpty(pageLabel))
            {
                var page = UiBuild.AddText("PageLabel", stage, font, 22, GoldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
                page.text = pageLabel;
                UiBuild.Place(page.rectTransform, x, 28f, 200f, 30f);
            }

            if (!string.IsNullOrEmpty(buttonLabel))
            {
                var button = UiBuild.AddButton("TopButton", stage, font, GrayButton, buttonLabel, 22, onButton);
                UiBuild.Place(button.rectTransform, UiBuild.StageWidth - 56f - 120f, 20f, 120f, 46f);
            }
        }

        // 아래쪽 탭 줄. 지금 화면의 탭은 금색 글자와 위쪽 금색 띠로 표시하고, 다른 탭을 누르면 onSelect가 불린다.
        public static void BuildTabBar(RectTransform stage, Font font, string activeId, Action<string> onSelect)
        {
            float top = UiBuild.StageHeight - TabBarHeight;
            var bar = UiBuild.AddImage("TabBar", stage, BarColor);
            UiBuild.Place(bar.rectTransform, -Overflow, top, UiBuild.StageWidth + Overflow * 2f, TabBarHeight + Overflow);
            var line = UiBuild.AddImage("TabLine", stage, LineColor);
            UiBuild.Place(line.rectTransform, -Overflow, top, UiBuild.StageWidth + Overflow * 2f, 3f);

            float groupWidth = TabIds.Length * TabWidth;
            float left = (UiBuild.StageWidth - groupWidth) / 2f;
            for (int i = 0; i < TabIds.Length; i++)
            {
                string id = TabIds[i];
                bool active = id == activeId;
                var tab = UiBuild.AddImage("Tab_" + id, stage, Color.clear);
                UiBuild.Place(tab.rectTransform, left + i * TabWidth, top, TabWidth, TabBarHeight);
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = tab;
                var colors = button.colors;
                colors.normalColor = Color.clear;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
                colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
                colors.selectedColor = Color.clear;
                button.colors = colors;
                if (!active)
                {
                    button.onClick.AddListener(() => onSelect(id));
                }

                var label = UiBuild.AddText("Label", tab.rectTransform, font, 26, active ? GoldColor : MutedColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.text = TabLabels[i];
                UiBuild.Stretch(label.rectTransform);

                if (active)
                {
                    var marker = UiBuild.AddImage("ActiveLine", tab.rectTransform, GoldColor);
                    marker.raycastTarget = false;
                    marker.rectTransform.anchorMin = new Vector2(0f, 1f);
                    marker.rectTransform.anchorMax = new Vector2(1f, 1f);
                    marker.rectTransform.pivot = new Vector2(0.5f, 1f);
                    marker.rectTransform.sizeDelta = new Vector2(-48f, 5f);
                    marker.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }
    }
}
