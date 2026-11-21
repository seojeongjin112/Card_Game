using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 전투가 끝났을 때 화면을 덮는 결과 화면(프로토타입의 endov): 어두운 배경, 돌아가는 광선, 한 글자씩 떨어지는 VICTORY / DEFEAT,
    // 이어서 요약 글과 버튼이 차례로 나타난다. 씬에 오브젝트를 두지 않으려고 감독이 코드로 만들고, 움직임은 감독의 Tick이 돌린다.
    public class UiEndOverlay : MonoBehaviour
    {
        private const int RayTextureSize = 512;

        [SerializeField] private Color _dimColor = new Color(0.012f, 0.055f, 0.039f, 0.85f);
        [SerializeField] private float _dimSeconds = 0.5f;
        [SerializeField] private Color _winRayColor = new Color(0.165f, 0.42f, 0.741f, 0.25f);
        [SerializeField] private Color _loseRayColor = new Color(1f, 0.353f, 0.235f, 0.18f);
        [SerializeField] private float _raySize = 2400f;
        [SerializeField] private float _rayTurnSeconds = 18f;
        [SerializeField] private float _rayCenterY = 216f;
        [SerializeField] private float _rayWedgeDegrees = 8f;
        [SerializeField] private float _rayPeriodDegrees = 20f;
        [SerializeField] private Color _winTitleColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _winShadowColor = new Color(0.165f, 0.42f, 0.741f, 1f);
        [SerializeField] private Color _loseTitleColor = new Color(1f, 0.353f, 0.235f, 1f);
        [SerializeField] private Color _loseShadowColor = new Color(0.227f, 0.039f, 0.016f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.72f, 0.67f, 1f);
        [SerializeField] private int _titleFontSize = 200;
        [SerializeField] private float _letterWidth = 140f;
        [SerializeField] private float _titleY = 60f;
        [SerializeField] private float _letterStagger = 0.07f;
        [SerializeField] private float _letterSeconds = 0.6f;
        [SerializeField] private float _letterDropHeight = 160f;
        [SerializeField] private float _letterStartScale = 2f;
        [SerializeField] private float _letterStartRotation = 10f;
        [SerializeField] private Vector2 _shadowOffset = new Vector2(10.7f, -10.7f);
        [SerializeField] private int _subtitleFontSize = 30;
        [SerializeField] private float _subtitleY = -80f;
        [SerializeField] private float _subtitleDelay = 0.7f;
        [SerializeField] private int _hpFontSize = 36;
        [SerializeField] private float _hpY = -140f;
        [SerializeField] private float _hpDelay = 0.85f;
        [SerializeField] private Vector2 _buttonSize = new Vector2(360f, 80f);
        [SerializeField] private float _buttonY = -250f;
        [SerializeField] private float _buttonDelay = 1f;
        [SerializeField] private Color _buttonColor = new Color(0.96f, 0.38f, 0.27f, 1f);
        [SerializeField] private int _buttonFontSize = 32;
        [SerializeField] private float _riseSeconds = 0.5f;
        [SerializeField] private float _riseHeight = 16f;

        private Image _dim;
        private RectTransform _rays;
        private Image _rayImage;
        private Text[] _letters = new Text[0];
        private Shadow[] _shadows = new Shadow[0];
        private Text _subtitle;
        private Text _hp;
        private Button _button;
        private CanvasGroup _buttonGroup;
        private Font _font;
        private Sprite _raySprite;
        private float _clock = -1f;
        private bool _win;

        public bool IsShown => gameObject.activeSelf;

        public static UiEndOverlay Create(RectTransform parent, Font font)
        {
            var go = new GameObject("EndOverlay", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var overlay = go.AddComponent<UiEndOverlay>();
            overlay._font = font;
            overlay.Build();
            go.SetActive(false);
            return overlay;
        }

        // 버튼을 눌렀을 때 새 게임을 시작하게 하는 쪽이 연결한다.
        public void SetButtonAction(Action action, string label)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => action());
            _button.GetComponentInChildren<Text>().text = label;
        }

        public void Show(bool win, int round, int myHp, int enemyHp, string winFormat, string loseFormat, string hpFormat)
        {
            _win = win;
            gameObject.SetActive(true);
            _clock = 0f;

            var rayColor = win ? _winRayColor : _loseRayColor;
            _rayImage.color = rayColor;

            BuildLetters(win ? "VICTORY" : "DEFEAT", win ? _winTitleColor : _loseTitleColor, win ? _winShadowColor : _loseShadowColor);
            _subtitle.text = string.Format(win ? winFormat : loseFormat, round);
            _hp.text = string.Format(hpFormat, Mathf.Max(0, myHp), Mathf.Max(0, enemyHp));
            Apply();
        }

        public void Hide()
        {
            _clock = -1f;
            gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (_clock < 0f)
            {
                return;
            }

            _clock += dt;
            Apply();
        }

        private void Build()
        {
            _dim = gameObject.AddComponent<Image>();
            _dim.color = Color.clear;
            _dim.raycastTarget = true;

            _raySprite = CreateRaySprite();
            var rayGo = new GameObject("Rays", typeof(RectTransform));
            rayGo.layer = gameObject.layer;
            rayGo.transform.SetParent(transform, false);
            _rays = (RectTransform)rayGo.transform;
            _rays.anchorMin = _rays.anchorMax = _rays.pivot = new Vector2(0.5f, 0.5f);
            _rays.sizeDelta = new Vector2(_raySize, _raySize);
            _rays.anchoredPosition = new Vector2(0f, _rayCenterY);
            _rayImage = rayGo.AddComponent<Image>();
            _rayImage.sprite = _raySprite;
            _rayImage.raycastTarget = false;

            _subtitle = AddText("Subtitle", new Vector2(0f, _subtitleY), new Vector2(1400f, 50f), _subtitleFontSize, Color.white, FontStyle.Bold);
            _hp = AddText("Hp", new Vector2(0f, _hpY), new Vector2(1400f, 60f), _hpFontSize, _mutedColor, FontStyle.Normal);
            BuildButton();
        }

        private void BuildButton()
        {
            var go = new GameObject("RestartButton", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, _buttonY);
            rect.sizeDelta = _buttonSize;
            var image = go.AddComponent<Image>();
            image.color = _buttonColor;
            _button = go.AddComponent<Button>();
            _button.targetGraphic = image;
            _buttonGroup = go.AddComponent<CanvasGroup>();
            go.AddComponent<UiButtonFx>();

            var label = AddText("Label", Vector2.zero, _buttonSize, _buttonFontSize, Color.white, FontStyle.Bold);
            label.transform.SetParent(rect, false);
            label.rectTransform.anchoredPosition = Vector2.zero;
        }

        // 글자 수가 바뀌면 글자 오브젝트를 다시 만든다. 가운데 정렬로 한 줄에 늘어놓는다.
        private void BuildLetters(string word, Color color, Color shadowColor)
        {
            for (int i = 0; i < _letters.Length; i++)
            {
                Destroy(_letters[i].gameObject);
            }

            _letters = new Text[word.Length];
            _shadows = new Shadow[word.Length];
            float startX = -(word.Length - 1) * _letterWidth * 0.5f;
            for (int i = 0; i < word.Length; i++)
            {
                var text = AddText("Letter" + i, new Vector2(startX + i * _letterWidth, _titleY), new Vector2(_letterWidth * 1.4f, _titleFontSize * 1.3f), _titleFontSize, color, FontStyle.Bold);
                text.text = word[i].ToString();
                var shadow = text.gameObject.AddComponent<Shadow>();
                shadow.effectColor = shadowColor;
                shadow.effectDistance = _shadowOffset;
                _letters[i] = text;
                _shadows[i] = shadow;
            }
        }

        // 시계 하나로 모든 단계를 계산한다. 단계마다 시작 시각이 달라 겹쳐서 진행된다.
        private void Apply()
        {
            float dimK = Mathf.Clamp01(_clock / _dimSeconds);
            _dim.color = new Color(_dimColor.r, _dimColor.g, _dimColor.b, _dimColor.a * dimK);
            var rayColor = _win ? _winRayColor : _loseRayColor;
            _rayImage.color = new Color(rayColor.r, rayColor.g, rayColor.b, rayColor.a * dimK);
            _rays.localRotation = Quaternion.Euler(0f, 0f, -_clock / _rayTurnSeconds * 360f);

            for (int i = 0; i < _letters.Length; i++)
            {
                float k = Mathf.Clamp01((_clock - i * _letterStagger) / _letterSeconds);
                float eased = UiEase.CubicBezier(0.2f, 1.6f, 0.4f, 1f, k);
                float remain = 1f - eased;
                var rect = _letters[i].rectTransform;
                float startX = -(_letters.Length - 1) * _letterWidth * 0.5f;
                rect.anchoredPosition = new Vector2(startX + i * _letterWidth, _titleY + _letterDropHeight * remain);
                rect.localScale = Vector3.one * Mathf.LerpUnclamped(1f, _letterStartScale, remain);
                rect.localRotation = Quaternion.Euler(0f, 0f, _letterStartRotation * remain);
                var color = _letters[i].color;
                color.a = k <= 0f ? 0f : 1f;
                _letters[i].color = color;
                var shadowColor = _shadows[i].effectColor;
                shadowColor.a = color.a;
                _shadows[i].effectColor = shadowColor;
            }

            ApplyRise(_subtitle, _subtitleY, _subtitleDelay, Color.white);
            ApplyRise(_hp, _hpY, _hpDelay, _mutedColor);

            float buttonK = Mathf.Clamp01((_clock - _buttonDelay) / _riseSeconds);
            _buttonGroup.alpha = buttonK;
            _buttonGroup.blocksRaycasts = buttonK >= 1f;
        }

        // 프로토타입의 evIn: 투명한 채 아래에 있다가 위로 올라오며 나타난다.
        private void ApplyRise(Text text, float restY, float delay, Color color)
        {
            float k = Mathf.Clamp01((_clock - delay) / _riseSeconds);
            text.color = new Color(color.r, color.g, color.b, k);
            text.rectTransform.anchoredPosition = new Vector2(0f, restY - _riseHeight * (1f - k));
        }

        // 쐐기 모양 광선이 일정 간격으로 반복되는 원형 무늬. 색은 Image 색으로 입힌다.
        private Sprite CreateRaySprite()
        {
            var texture = new Texture2D(RayTextureSize, RayTextureSize, TextureFormat.RGBA32, false);
            float center = (RayTextureSize - 1) / 2f;
            for (int y = 0; y < RayTextureSize; y++)
            {
                for (int x = 0; x < RayTextureSize; x++)
                {
                    float angle = Mathf.Atan2(y - center, x - center) * Mathf.Rad2Deg;
                    if (angle < 0f)
                    {
                        angle += 360f;
                    }

                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    bool inside = angle % _rayPeriodDegrees < _rayWedgeDegrees && distance <= 1f;
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, RayTextureSize, RayTextureSize), new Vector2(0.5f, 0.5f), 100f);
        }

        private Text AddText(string name, Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
