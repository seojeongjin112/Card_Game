using System;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Poker_Card
{
    // 효과 카드 한 장을 글자로 보여 주는 임시 슬롯. 디자인이 나오면 이 클래스만 이미지 기반으로 바꾼다.
    // 위치·크기 연출(흔들림, 호버, 날아오기, 강조)은 모두 이 클래스가 한곳에서 합쳐서 적용한다.
    [RequireComponent(typeof(Button))]
    public class EffectCardSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Text _label;
        [SerializeField] private string _cardFormat = "[{0}] {1}\n{2}";
        [SerializeField] private string _emptyText = "빈 슬롯";
        [SerializeField] private string[] _gradeLabels = { "일반", "희귀", "영웅", "전설" };
        [SerializeField] private Color[] _gradeColors =
        {
            new Color(0.75f, 0.75f, 0.75f, 1f),
            new Color(0.45f, 0.7f, 1f, 1f),
            new Color(0.75f, 0.5f, 1f, 1f),
            new Color(1f, 0.8f, 0.3f, 1f)
        };
        [SerializeField] private Color _emptyColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Color _dimmedTint = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private float _selectedScale = 1.08f;

        [SerializeField] private float _spotlightDimAlpha = 0.3f;
        [SerializeField] private float _spotlightScale = 1.12f;

        [SerializeField] private float _spotlightGrayAmount = 0.7f;

        [Header("장착해서 날아오는 연출 (프로토타입 equip / newF)")]
        [SerializeField] private float _arriveDuration = 0.52f;
        [SerializeField] private float _arriveStartScale = 1.1f;
        [SerializeField] private float _arriveStartAlpha = 0.4f;
        [SerializeField] private Vector4 _arriveEase = new Vector4(0.2f, 1.1f, 0.4f, 1f);
        [SerializeField] private float _glowDuration = 1f;
        [SerializeField, Range(0f, 1f)] private float _glowHoldUntil = 0.6f;
        [SerializeField] private Color _glowColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Vector2 _glowDistance = new Vector2(5f, 5f);

        [Header("흔들림과 호버 (프로토타입 sway / hover, 1440x810 값을 1920x1080으로 환산)")]
        [SerializeField] private float _swayAmplitude = 6.7f;
        [SerializeField] private float _swayPeriod = 3.6f;
        [SerializeField] private float _swayPhasePerIndex = 0.3f;
        [SerializeField] private float _hoverSeconds = 0.15f;
        [SerializeField] private int _hoverSortingOrder = 1;

        private int _index;
        private CanvasGroup _group;
        private Outline _glow;
        private Canvas _hoverCanvas;
        private Vector3 _arriveOffset;
        private float _arriveTime;
        private bool _arriving;
        private bool _grayed;
        private Color _baseBackground;
        private Color _labelColor;

        private bool _selected;
        private SpotlightState _spotlight;
        private bool _hasCard;
        private bool _hovered;
        private float _hoverK;
        private bool _swayEnabled;
        private float _swayClock;
        private float _hoverLift;
        private float _hoverScale = 1f;

        private Vector3 _restPosition;
        private Vector3 _lastAppliedPosition;
        private bool _hasApplied;

        public enum SpotlightState
        {
            Normal,
            Lit,
            Dimmed
        }

        public event Action<int> Clicked;

        private void Awake()
        {
            // 호버로 커진 카드가 이웃 카드 위로 올라오도록 따로 정렬할 수 있는 캔버스를 둔다. 평소에는 아무것도 바꾸지 않는다.
            _hoverCanvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
            _hovered = false;
            _hoverK = 0f;
        }

        public void Initialize(int index)
        {
            _index = index;
        }

        // 손패와 상대 칸이 같은 슬롯을 쓰되 프로토타입처럼 다른 세기로 움직이게 한다.
        public void ConfigureMotion(bool sway, float hoverLift, float hoverScale)
        {
            _swayEnabled = sway;
            _hoverLift = hoverLift;
            _hoverScale = hoverScale;
        }

        public void Show(EffectCardData card, string status, bool dimmed)
        {
            int grade = (int)card.Grade;
            _hasCard = true;
            _label.text = string.Format(_cardFormat, _gradeLabels[grade], card.DisplayName, status);
            SetBackground(dimmed ? _gradeColors[grade] * _dimmedTint : _gradeColors[grade]);
        }

        public void ShowEmpty()
        {
            _hasCard = false;
            _label.text = _emptyText;
            SetBackground(_emptyColor);
        }

        // 카드 얼굴을 따로 그리는 쪽이 이 슬롯을 클릭 영역으로만 쓰도록 자기 그림과 글자를 숨긴다.
        public void HideVisuals()
        {
            SetBackground(Color.clear);
            _label.enabled = false;
        }

        // 상대의 아직 쓰지 않은 액티브처럼 정체를 숨겨야 하는 카드에 쓴다.
        public void ShowHidden(string text)
        {
            _hasCard = true;
            _label.text = text;
            SetBackground(_emptyColor);
        }

        // 눌러서 쓸 수 있는 카드만 버튼이 반응하게 해서 패시브나 사용한 카드를 눌러도 아무 일이 없음을 보여 준다.
        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        // 레이아웃 그룹은 자식의 scale을 무시하므로, 선택 강조는 크기만 키워도 다른 슬롯 위치가 밀리지 않는다.
        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
        }

        // 카드가 있는 슬롯만 반응한다. 비어 있는 칸이 커지면 오히려 눌러 볼 수 있는 것처럼 보인다.
        public void SetHovered(bool hovered)
        {
            _hovered = hovered && _hasCard;
            _hoverCanvas.overrideSorting = _hovered;
            _hoverCanvas.sortingOrder = _hoverSortingOrder;
        }

        // 프로토타입의 equip: 고른 카드가 있던 자리에서 손패 슬롯으로 날아와 앉고, 금색 테두리가 잠깐 빛난다.
        public void PlayArrive(Vector3 worldFrom)
        {
            // 새 슬롯이 켜진 직후라 레이아웃이 아직 자리를 안 잡았을 수 있어서 먼저 확정한다.
            if (transform.parent is RectTransform parentRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            EnsureGroup();
            EnsureGlow();
            _hasApplied = false;
            _arriveOffset = transform.parent.InverseTransformVector(worldFrom - transform.position);
            _arriveTime = 0f;
            _arriving = true;
            ApplyVisuals();
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            if (_arriving)
            {
                _arriveTime += dt;
            }

            _swayClock += dt;
            _hoverK = Mathf.MoveTowards(_hoverK, _hovered ? 1f : 0f, dt / _hoverSeconds);
            ApplyVisuals();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 날아오기·흔들림·호버·강조를 합쳐 위치와 크기를 한 번에 정한다.
        private void ApplyVisuals()
        {
            float arriveRemain = 0f;
            if (_arriving)
            {
                float moveK = Mathf.Clamp01(_arriveTime / _arriveDuration);
                arriveRemain = 1f - UiEase.CubicBezier(_arriveEase.x, _arriveEase.y, _arriveEase.z, _arriveEase.w, moveK);
                _group.alpha = Mathf.Lerp(1f, _arriveStartAlpha, arriveRemain);

                // 글로우는 처음 60%는 그대로 켜 두고 나머지에서 꺼진다(프로토타입 newF).
                float glowK = Mathf.Clamp01(_arriveTime / _glowDuration);
                float glowAlpha = glowK < _glowHoldUntil ? 1f : 1f - Mathf.InverseLerp(_glowHoldUntil, 1f, glowK);
                var glowColor = _glowColor;
                glowColor.a *= glowAlpha;
                _glow.effectColor = glowColor;

                if (_arriveTime >= Mathf.Max(_arriveDuration, _glowDuration))
                {
                    _arriving = false;
                    arriveRemain = 0f;
                    _glow.effectColor = Color.clear;
                    _group.alpha = _spotlight == SpotlightState.Dimmed ? _spotlightDimAlpha : 1f;
                }
            }

            // 강조 중이거나 날아오는 중에는 흔들리지 않는다(프로토타입: pflash, new 카드 제외).
            bool swaying = _swayEnabled && _hasCard && _spotlight == SpotlightState.Normal && !_arriving;
            float sway = 0f;
            if (swaying)
            {
                float phase = (_swayClock + _index * _swayPhasePerIndex) / _swayPeriod;
                sway = _swayAmplitude * (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f;
            }

            float hover = Mathf.SmoothStep(0f, 1f, _hoverK);
            Vector3 offset = _arriveOffset * arriveRemain + Vector3.up * (sway + _hoverLift * hover);

            // 레이아웃이 위치를 다시 잡으면 그 자리를 새 기준으로 삼고, 아니면 지난번 기준에 오프셋만 더한다.
            if (!_hasApplied || (transform.localPosition - _lastAppliedPosition).sqrMagnitude > 0.0001f)
            {
                _restPosition = transform.localPosition;
            }

            transform.localPosition = _restPosition + offset;
            _lastAppliedPosition = transform.localPosition;
            _hasApplied = true;

            float baseScale = _selected ? _selectedScale : _spotlight == SpotlightState.Lit ? _spotlightScale : 1f;
            float scale = baseScale * Mathf.Lerp(1f, _hoverScale, hover) * Mathf.Lerp(1f, _arriveStartScale, arriveRemain);
            transform.localScale = Vector3.one * scale;
        }

        private void EnsureGroup()
        {
            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                {
                    _group = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void EnsureGlow()
        {
            if (_glow == null)
            {
                _glow = _background.gameObject.GetComponent<Outline>();
                if (_glow == null)
                {
                    _glow = _background.gameObject.AddComponent<Outline>();
                }

                _glow.effectDistance = _glowDistance;
                _glow.effectColor = Color.clear;
            }
        }

        // 패시브 발동 연출: 발동하는 카드만 밝고 크게, 나머지는 흐리게 해서 어느 카드가 점수에 더해지는지 눈에 띄게 한다.
        public void SetSpotlight(SpotlightState state)
        {
            EnsureGroup();

            bool gray = state == SpotlightState.Dimmed;
            if (gray != _grayed)
            {
                if (gray)
                {
                    _labelColor = _label.color;
                }
                else
                {
                    _label.color = _labelColor;
                }

                _grayed = gray;
                SetBackground(_baseBackground);
                if (gray)
                {
                    _label.color = Desaturate(_labelColor);
                }
            }

            _spotlight = state;
            _group.alpha = gray ? _spotlightDimAlpha : 1f;
            ApplyVisuals();
        }

        // 배경색을 바꾸는 모든 곳이 이 함수를 지나게 해서, 흐려진 동안에도 새 색이 흑백으로 적용되게 한다.
        private void SetBackground(Color color)
        {
            _baseBackground = color;
            _background.color = _grayed ? Desaturate(color) : color;
        }

        private Color Desaturate(Color color)
        {
            float luminance = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            return Color.Lerp(color, new Color(luminance, luminance, luminance, color.a), _spotlightGrayAmount);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(_index);
        }
    }
}
