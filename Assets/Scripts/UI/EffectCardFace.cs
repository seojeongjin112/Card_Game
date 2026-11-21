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
        private GameObject _holoMask;
        private Image _holoBand;
        private float _holoTime;

        // 홀로 스프라이트는 모든 카드가 같은 것을 쓴다.
        private static Sprite _epicHolo;
        private static Sprite _legendHolo;

        [SerializeField] private float _holoPeriod = 3.2f;
        [SerializeField] private float _holoBandWidthFactor = 1.6f;
        [SerializeField] private float _holoBandHeightFactor = 1.8f;
        [SerializeField] private float _holoTravelFactor = 1.1f;
        [SerializeField] private float _holoTiltDegrees = 25f;
        [SerializeField] private float _epicHoloOpacity = 0.9f;
        [SerializeField] private float _legendHoloOpacity = 1f;
        [SerializeField] private int _holoTextureWidth = 128;

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

            // 홀로 효과는 맨 위에 덮는다. 카드 종이 모양 밖으로 번지지 않도록 마스크 안에서만 띠가 움직인다.
            var holoMask = AddImage("Holo", root, Vector2.zero, size - new Vector2(inset * 2f, inset * 2f), Color.clear);
            holoMask.gameObject.AddComponent<RectMask2D>();
            _holoMask = holoMask.gameObject;
            _holoBand = AddImage("Band", holoMask.rectTransform, Vector2.zero, new Vector2(w * _holoBandWidthFactor, h * _holoBandHeightFactor), Color.white);
            _holoBand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _holoTiltDegrees);
            _holoMask.SetActive(false);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 프로토타입의 holo: 3.2초마다 빛 띠가 카드를 가로질러 흐른다. 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            if (!_holoMask.activeSelf)
            {
                return;
            }

            _holoTime = (_holoTime + dt) % _holoPeriod;
            float travel = Rect.sizeDelta.x * _holoTravelFactor;
            _holoBand.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(travel, -travel, _holoTime / _holoPeriod), 0f);
        }

        private void SetHolo(EffectGrade grade)
        {
            bool shiny = grade == EffectGrade.Epic || grade == EffectGrade.Legend;
            _holoMask.SetActive(shiny);
            if (!shiny)
            {
                return;
            }

            bool legend = grade == EffectGrade.Legend;
            if (legend)
            {
                _legendHolo = _legendHolo != null ? _legendHolo : CreateHoloSprite(LegendHoloStops);
            }
            else
            {
                _epicHolo = _epicHolo != null ? _epicHolo : CreateHoloSprite(EpicHoloStops);
            }

            _holoBand.sprite = legend ? _legendHolo : _epicHolo;
            _holoBand.color = new Color(1f, 1f, 1f, legend ? _legendHoloOpacity : _epicHoloOpacity);
        }

        // 색 정지점(위치 0~1, 색) 목록으로 가로 그라디언트 한 줄을 만들어 늘려서 쓴다.
        private Sprite CreateHoloSprite(HoloStop[] stops)
        {
            int width = _holoTextureWidth;
            var texture = new Texture2D(width, 2, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < width; x++)
            {
                float position = (float)x / (width - 1);
                Color color = Color.clear;
                for (int i = 0; i < stops.Length - 1; i++)
                {
                    if (position >= stops[i].Position && position <= stops[i + 1].Position)
                    {
                        float t = Mathf.InverseLerp(stops[i].Position, stops[i + 1].Position, position);
                        color = Color.Lerp(stops[i].Color, stops[i + 1].Color, t);
                        break;
                    }
                }

                texture.SetPixel(x, 0, color);
                texture.SetPixel(x, 1, color);
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, 2), new Vector2(0.5f, 0.5f), 100f);
        }

        private readonly struct HoloStop
        {
            public readonly float Position;
            public readonly Color Color;

            public HoloStop(float position, Color color)
            {
                Position = position;
                Color = color;
            }
        }

        // 프로토타입 CSS의 linear-gradient 정지점. 끝은 투명으로 둔다.
        private static readonly HoloStop[] EpicHoloStops =
        {
            new HoloStop(0.00f, new Color(0.69f, 0.42f, 1f, 0f)), new HoloStop(0.20f, new Color(0.69f, 0.42f, 1f, 0f)),
            new HoloStop(0.32f, new Color(0.69f, 0.42f, 1f, 0.55f)), new HoloStop(0.38f, new Color(1f, 1f, 1f, 0.7f)),
            new HoloStop(0.44f, new Color(0.69f, 0.42f, 1f, 0.5f)), new HoloStop(0.58f, new Color(0.69f, 0.42f, 1f, 0f)),
            new HoloStop(1.00f, new Color(0.69f, 0.42f, 1f, 0f))
        };

        private static readonly HoloStop[] LegendHoloStops =
        {
            new HoloStop(0.00f, new Color(1f, 0.35f, 0.24f, 0f)), new HoloStop(0.12f, new Color(1f, 0.35f, 0.24f, 0f)),
            new HoloStop(0.22f, new Color(1f, 0.35f, 0.24f, 0.45f)), new HoloStop(0.30f, new Color(1f, 0.88f, 0.54f, 0.75f)),
            new HoloStop(0.35f, new Color(1f, 1f, 1f, 0.9f)), new HoloStop(0.42f, new Color(0.24f, 1f, 0.58f, 0.45f)),
            new HoloStop(0.50f, new Color(0.16f, 0.42f, 0.74f, 0.5f)), new HoloStop(0.58f, new Color(0.69f, 0.42f, 1f, 0.5f)),
            new HoloStop(0.70f, new Color(0.69f, 0.42f, 1f, 0f)), new HoloStop(1.00f, new Color(0.69f, 0.42f, 1f, 0f))
        };

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
            SetHolo(card.Grade);
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
