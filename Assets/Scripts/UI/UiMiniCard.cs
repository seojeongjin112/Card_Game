using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 카드 화면과 덱 화면이 함께 쓰는 작은 효과 카드(프로토타입의 .mc): 등급 색 테두리 안에 숫자·문양, 이름, 분류 색 아이콘.
    // 눌림 처리는 부르는 쪽이 붙인다. 기준 크기 135x157에 맞춰 글자와 아이콘을 비율로 줄이거나 늘린다.
    public static class UiMiniCard
    {
        public const float BaseWidth = 135f;
        public const float BaseHeight = 157f;

        private static readonly Color Paper = new Color(0.97f, 0.96f, 0.92f, 1f);
        private static readonly Color Ink = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color RedSuit = new Color(0.78f, 0.12f, 0.1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color TagColor = new Color(1f, 0.353f, 0.235f, 1f);

        // 분류 색: 공격은 빨강, 방해는 보라, 방어·유틸은 파랑, 와일드는 노랑, 나머지는 초록.
        public static Color GetCategoryColor(EffectCardData card)
        {
            var category = card.Category;
            if (category.HasFlag(EffectCategory.Attack))
            {
                return new Color(1f, 0.35f, 0.24f, 1f);
            }

            if (category.HasFlag(EffectCategory.Disrupt))
            {
                return new Color(0.49f, 0.3f, 0.77f, 1f);
            }

            if (category.HasFlag(EffectCategory.Defense) || category.HasFlag(EffectCategory.Utility))
            {
                return new Color(0.16f, 0.42f, 0.74f, 1f);
            }

            return category.HasFlag(EffectCategory.Wild) ? new Color(0.9f, 0.64f, 0.24f, 1f) : new Color(0.18f, 0.64f, 0.42f, 1f);
        }

        // size는 실제로 그릴 크기, tagText가 있으면 오른쪽 위에 붉은 작은 표를 단다(예: "덱").
        public static Image Create(RectTransform parent, Font font, EffectCardData data, Vector2 size, bool selected, string tagText)
        {
            float scale = size.x / BaseWidth;
            var border = UiBuild.AddImage("Card_" + data.DisplayName, parent, selected ? Gold : EffectCardFace.GetGradeColor(data.Grade));

            var paper = UiBuild.AddImage("Paper", border.rectTransform, Paper);
            paper.raycastTarget = false;
            UiBuild.Stretch(paper.rectTransform);
            float inset = 5f * scale;
            paper.rectTransform.offsetMin = new Vector2(inset, inset);
            paper.rectTransform.offsetMax = new Vector2(-inset, -inset);

            bool red = data.Suit == Suit.Heart || data.Suit == Suit.Diamond;
            var code = UiBuild.AddText("Code", paper.rectTransform, font, Mathf.RoundToInt(26f * scale), red ? RedSuit : Ink, FontStyle.Bold, TextAnchor.UpperLeft);
            code.text = EffectCardFace.GetCodeText(data);
            UiBuild.Place(code.rectTransform, 8f * scale, 6f * scale, 110f * scale, 34f * scale);

            var name = UiBuild.AddText("Name", paper.rectTransform, font, Mathf.RoundToInt(20f * scale), Ink, FontStyle.Bold, TextAnchor.UpperLeft);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.text = data.DisplayName;
            UiBuild.Place(name.rectTransform, 8f * scale, 48f * scale, 112f * scale, 56f * scale);

            var icon = UiBuild.AddImage("Icon", paper.rectTransform, GetCategoryColor(data));
            icon.raycastTarget = false;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(22f, 22f) * scale;
            icon.rectTransform.anchoredPosition = new Vector2(24f, 16f) * scale;
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            if (!string.IsNullOrEmpty(tagText))
            {
                var tag = UiBuild.AddImage("Tag", paper.rectTransform, TagColor);
                tag.raycastTarget = false;
                tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = tag.rectTransform.pivot = new Vector2(1f, 1f);
                tag.rectTransform.sizeDelta = new Vector2(38f, 22f) * scale;
                tag.rectTransform.anchoredPosition = new Vector2(-6f, -6f) * scale;
                var label = UiBuild.AddText("Text", tag.rectTransform, font, Mathf.RoundToInt(15f * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                label.text = tagText;
                UiBuild.Stretch(label.rectTransform);
            }

            return border;
        }
    }
}
