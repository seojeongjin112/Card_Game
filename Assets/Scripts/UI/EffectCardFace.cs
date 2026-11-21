using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 프로토타입의 효과 카드(프레임, 문양·숫자, 등급 글자, 이름, 분류, 아이콘, 설명, 하단 문구)를 같은 구조로 그리는 카드 얼굴.
    // 디자인 에셋이 없어서 아이콘은 분류 색의 마름모와 글자로 대신한다. 구조를 코드가 만들어서 씬에는 빈 오브젝트만 있으면 된다.
    public class EffectCardFace : MonoBehaviour
    {
        private const string SuitGlyphs = "♠♥♦♣";

        private Text _suitText;
        private Text _rankText;
        private Text _gradeText;
        private Text _nameText;
        private Text _typeText;
        private Image _iconImage;
        private Text _iconText;
        private Text _descriptionText;
        private Text _footerText;
        private Image _border;
        private Image _paper;

        public RectTransform Rect => (RectTransform)transform;

        // 카드 크기에 비례해 모든 요소를 배치한다. 같은 모양을 크고 작게 재사용하려는 것이다.
        public static EffectCardFace Create(RectTransform parent, Font font, Vector2 size)
        {
            var go = new GameObject("EffectCardFace", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var face = go.AddComponent<EffectCardFace>();
            face.Build(font, size);
            return face;
        }

        private void Build(Font font, Vector2 size)
        {
            var root = Rect;
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = size;

            float h = size.y;
            float w = size.x;
            _border = AddImage("Border", root, Vector2.zero, size, new Color(0.1f, 0.12f, 0.12f, 1f));
            float inset = h * 0.022f;
            _paper = AddImage("Paper", root, Vector2.zero, size - new Vector2(inset * 2f, inset * 2f), new Color(0.97f, 0.94f, 0.86f, 1f));

            _suitText = AddText("Suit", root, font, new Vector2(-w * 0.34f, h * 0.38f), new Vector2(w * 0.2f, h * 0.12f), Mathf.RoundToInt(h * 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _rankText = AddText("Rank", root, font, new Vector2(-w * 0.14f, h * 0.38f), new Vector2(w * 0.24f, h * 0.14f), Mathf.RoundToInt(h * 0.13f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _gradeText = AddText("Grade", root, font, new Vector2(w * 0.3f, h * 0.38f), new Vector2(w * 0.3f, h * 0.16f), Mathf.RoundToInt(h * 0.14f), TextAnchor.MiddleRight, FontStyle.Bold);
            _nameText = AddText("Name", root, font, new Vector2(0f, h * 0.22f), new Vector2(w * 0.86f, h * 0.12f), Mathf.RoundToInt(h * 0.1f), TextAnchor.MiddleLeft, FontStyle.Bold);
            _typeText = AddText("Type", root, font, new Vector2(0f, h * 0.13f), new Vector2(w * 0.86f, h * 0.06f), Mathf.RoundToInt(h * 0.048f), TextAnchor.MiddleLeft, FontStyle.Normal);

            // 아이콘 자리: 분류 색 마름모와 분류 첫 글자
            _iconImage = AddImage("Icon", root, new Vector2(0f, -h * 0.02f), new Vector2(h * 0.17f, h * 0.17f), Color.gray);
            _iconImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _iconText = AddText("IconLetter", root, font, new Vector2(0f, -h * 0.02f), new Vector2(h * 0.2f, h * 0.2f), Mathf.RoundToInt(h * 0.09f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _iconText.color = Color.white;

            AddImage("DescriptionBox", root, new Vector2(0f, -h * 0.25f), new Vector2(w * 0.86f, h * 0.2f), new Color(0.85f, 0.84f, 0.8f, 1f));
            _descriptionText = AddText("Description", root, font, new Vector2(0f, -h * 0.25f), new Vector2(w * 0.8f, h * 0.19f), Mathf.RoundToInt(h * 0.046f), TextAnchor.MiddleLeft, FontStyle.Normal);
            _footerText = AddText("Footer", root, font, new Vector2(0f, -h * 0.4f), new Vector2(w * 0.86f, h * 0.06f), Mathf.RoundToInt(h * 0.048f), TextAnchor.MiddleLeft, FontStyle.Bold);
        }

        // "6♠"처럼 숫자와 문양을 붙인 카드 코드. 확정 버튼과 미리보기에서 카드를 가리킬 때 쓴다.
        public static string GetCodeText(EffectCardData card)
        {
            return GetRankText(card.Rank) + SuitGlyphs[(int)card.Suit];
        }

        // 프로토타입의 등급 색(일반 회색, 희귀 파랑, 영웅 보라, 전설 금색).
        public static Color GetGradeColor(EffectGrade grade)
        {
            switch (grade)
            {
                case EffectGrade.Legend: return new Color(1f, 0.82f, 0.25f, 1f);
                case EffectGrade.Epic: return new Color(0.69f, 0.42f, 1f, 1f);
                case EffectGrade.Rare: return new Color(0.3f, 0.55f, 0.95f, 1f);
                default: return new Color(0.62f, 0.62f, 0.62f, 1f);
            }
        }

        // 분류는 한 카드가 둘을 가질 수 있어서("공격/방해") 켜진 비트를 모두 이어 붙인다.
        public static string GetCategoryLabel(EffectCategory category)
        {
            string[] labels = { "공격", "방어", "유틸", "방해", "강화", "함정", "와일드", "특수" };
            var parts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < labels.Length; i++)
            {
                if (((int)category & (1 << i)) != 0)
                {
                    parts.Add(labels[i]);
                }
            }

            return string.Join("/", parts);
        }

        // "액티브 & 공격"처럼 종류와 분류를 한 줄로 보여 주고, 하단에는 사용 기간 같은 문구를 받는다.
        public void Show(EffectCardData card, string activeLabel, string passiveLabel, string footer,
            Color gradeColor, string gradeLetter)
        {
            string categoryLabel = GetCategoryLabel(card.Category);
            _border.color = gradeColor;
            _suitText.text = SuitGlyphs[(int)card.Suit].ToString();
            _suitText.color = card.Suit == Suit.Heart || card.Suit == Suit.Diamond ? new Color(0.78f, 0.12f, 0.1f, 1f) : new Color(0.1f, 0.1f, 0.1f, 1f);
            _rankText.text = GetRankText(card.Rank);
            _rankText.color = _suitText.color;
            _gradeText.text = gradeLetter;
            _gradeText.color = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.75f);
            _nameText.text = card.DisplayName;
            _nameText.color = card.Grade == EffectGrade.Legend
                ? new Color(0.55f, 0.35f, 0f, 1f)
                : card.Grade == EffectGrade.Epic ? new Color(0.36f, 0.16f, 0.6f, 1f) : new Color(0.12f, 0.12f, 0.12f, 1f);
            _typeText.text = (card.IsActive ? activeLabel : passiveLabel) + " & " + categoryLabel;
            _typeText.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            _iconImage.color = new Color(gradeColor.r * 0.8f, gradeColor.g * 0.8f, gradeColor.b * 0.8f, 1f);
            _iconText.text = categoryLabel.Length > 0 ? categoryLabel.Substring(0, 1) : string.Empty;
            _descriptionText.text = card.Description;
            _descriptionText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            _footerText.text = footer;
            _footerText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        private static string GetRankText(Rank rank)
        {
            int value = (int)rank;
            if (value <= 10)
            {
                return value.ToString();
            }

            switch (rank)
            {
                case Rank.Jack: return "J";
                case Rank.Queen: return "Q";
                case Rank.King: return "K";
                default: return "A";
            }
        }

        private static Image AddImage(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text AddText(string name, RectTransform parent, Font font, Vector2 position, Vector2 size, int fontSize,
            TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
