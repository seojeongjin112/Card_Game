using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 족보 점수표 줄 위에 마우스를 올리면 뜨는 설명 상자(프로토타입의 .tip): 이름과 점수, 만드는 법, 예시 카드 5장.
    // 족보를 이루는 카드는 또렷하게, 나머지(키커)는 흐리게 보여 준다. 씬에 오브젝트를 두지 않으려고 코드로 만든다.
    public class UiHandTip : MonoBehaviour
    {
        private const int CardCount = 5;

        [SerializeField] private Vector2 _size = new Vector2(430f, 150f);
        [SerializeField] private float _border = 4f;
        [SerializeField] private Color _paperColor = Color.white;
        [SerializeField] private Color _inkColor = new Color(0.1f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color _redColor = new Color(0.847f, 0.145f, 0.165f, 1f);
        [SerializeField] private Color _hitOutlineColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private float _kickerAlpha = 0.45f;
        [SerializeField] private int _titleFontSize = 24;
        [SerializeField] private int _bodyFontSize = 20;
        [SerializeField] private int _cardFontSize = 22;
        [SerializeField] private Vector2 _cardSize = new Vector2(64f, 44f);
        [SerializeField] private float _cardGap = 6f;
        [SerializeField] private float _liftAboveRow = 14f;
        [SerializeField] private float _fadeSeconds = 0.15f;
        [SerializeField] private float _edgeMargin = 8f;

        private RectTransform _rect;
        private CanvasGroup _group;
        private Text _title;
        private Text _body;
        private readonly Text[] _cardTexts = new Text[CardCount];
        private readonly Image[] _cardPapers = new Image[CardCount];
        private readonly Outline[] _cardOutlines = new Outline[CardCount];
        private bool _visible;

        public static UiHandTip Create(RectTransform parent, Font font)
        {
            var go = new GameObject("HandTip", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var tip = go.AddComponent<UiHandTip>();
            tip.Build(font);
            return tip;
        }

        private void Build(Font font)
        {
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
            _rect.pivot = new Vector2(0.5f, 0f);
            _rect.sizeDelta = _size;

            // 바깥은 진한 테두리, 안쪽은 흰 종이. 오른쪽 아래에 그림자를 둔다.
            var border = gameObject.AddComponent<Image>();
            border.color = _inkColor;
            border.raycastTarget = false;
            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
            shadow.effectDistance = new Vector2(6f, -6f);
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var paper = AddImage("Paper", _rect, Vector2.zero, _size - new Vector2(_border * 2f, _border * 2f), _paperColor);
            paper.rectTransform.anchoredPosition = Vector2.zero;
            _title = AddText("Title", _rect, font, new Vector2(0f, _size.y / 2f - 34f), new Vector2(_size.x - 32f, 36f), _titleFontSize, FontStyle.Bold);
            _body = AddText("Body", _rect, font, new Vector2(0f, _size.y / 2f - 74f), new Vector2(_size.x - 32f, 32f), _bodyFontSize, FontStyle.Normal);

            float rowWidth = CardCount * _cardSize.x + (CardCount - 1) * _cardGap;
            float startX = -rowWidth / 2f + _cardSize.x / 2f;
            for (int i = 0; i < CardCount; i++)
            {
                var cardPaper = AddImage("Card" + i, _rect, new Vector2(startX + i * (_cardSize.x + _cardGap), -_size.y / 2f + 40f), _cardSize, _paperColor);
                var cardBorder = cardPaper.gameObject.AddComponent<Outline>();
                cardBorder.effectColor = _inkColor;
                cardBorder.effectDistance = new Vector2(2f, 2f);
                var text = AddText("Text", cardPaper.rectTransform, font, Vector2.zero, _cardSize, _cardFontSize, FontStyle.Bold);
                _cardPapers[i] = cardPaper;
                _cardTexts[i] = text;
                _cardOutlines[i] = cardBorder;
            }
        }

        // anchor 줄의 위쪽 가운데에 상자를 띄운다. 화면 밖으로 나가지 않도록 부모 영역 안으로 밀어 넣는다.
        public void Show(RectTransform anchor, string title, string how, string[] cards, int hitCount)
        {
            _title.text = title;
            _body.text = how;
            for (int i = 0; i < CardCount; i++)
            {
                string code = i < cards.Length ? cards[i] : string.Empty;
                bool hit = i < hitCount;
                _cardTexts[i].text = code;
                _cardTexts[i].color = IsRed(code) ? _redColor : _inkColor;
                _cardPapers[i].color = new Color(_paperColor.r, _paperColor.g, _paperColor.b, hit ? 1f : _kickerAlpha);
                _cardTexts[i].canvasRenderer.SetAlpha(hit ? 1f : _kickerAlpha);
                _cardOutlines[i].effectColor = hit ? _hitOutlineColor : new Color(_inkColor.r, _inkColor.g, _inkColor.b, _kickerAlpha);
                _cardOutlines[i].effectDistance = hit ? new Vector2(3f, 3f) : new Vector2(2f, 2f);
            }

            var parent = (RectTransform)transform.parent;
            Vector3 world = anchor.TransformPoint(new Vector3(anchor.rect.center.x, anchor.rect.yMax, 0f));
            Vector2 local = parent.InverseTransformPoint(world);
            float halfWidth = _size.x / 2f;
            float minX = parent.rect.xMin + halfWidth + _edgeMargin;
            float maxX = parent.rect.xMax - halfWidth - _edgeMargin;
            local.x = Mathf.Clamp(local.x, minX, maxX);
            local.y += _liftAboveRow;
            _rect.anchoredPosition = local;
            transform.SetAsLastSibling();
            _visible = true;
        }

        public void Hide()
        {
            _visible = false;
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, Time.unscaledDeltaTime / _fadeSeconds);
        }

        private static bool IsRed(string code)
        {
            return code.Contains("♥") || code.Contains("♦");
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

        private Text AddText(string name, RectTransform parent, Font font, Vector2 position, Vector2 size, int fontSize, FontStyle style)
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
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = _inkColor;
            text.raycastTarget = false;
            return text;
        }
    }
}
