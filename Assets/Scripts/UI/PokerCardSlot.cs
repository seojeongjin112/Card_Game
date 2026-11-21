using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Poker_Card
{
    // 손패 카드 한 장. 나눠 줄 때의 날아오기, 평소의 흔들림, 호버, 선택으로 올라가기를 모두 Face 하나에 합쳐서 적용한다.
    [RequireComponent(typeof(Button))]
    public class PokerCardSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _face;
        [SerializeField] private float _selectedOffsetY = 30f;
        [SerializeField] private Color _dimmedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("나눠 줄 때의 연출 (프로토타입 deal, 1440x810 값을 1920x1080으로 환산)")]
        [SerializeField] private Vector2 _dealOffset = new Vector2(400f, -240f);
        [SerializeField] private float _dealStartRotation = -30f;
        [SerializeField] private float _dealStartScale = 0.6f;
        [SerializeField] private float _dealDuration = 0.5f;
        [SerializeField] private float _dealFadeSpeed = 1f;
        [SerializeField] private Vector4 _dealEase = new Vector4(0.2f, 1.1f, 0.4f, 1f);

        [Header("흔들림과 호버 (프로토타입 sway / hover)")]
        [SerializeField] private float _swayAmplitude = 6.7f;
        [SerializeField] private float _swayPeriod = 3.2f;
        [SerializeField] private float _swayPhasePerIndex = 0.28f;
        [SerializeField] private float _hoverLift = 16f;
        [SerializeField] private float _hoverRotation = 2f;
        [SerializeField] private float _hoverSeconds = 0.15f;

        [Header("리롤 (프로토타입 rr / flipout / flipin)")]
        [SerializeField] private Color _rerollColor = new Color(1f, 0.949f, 0.69f, 1f);
        [SerializeField] private float _rerollOffsetY = 8f;
        [SerializeField] private float _rerollRotation = 3f;
        [SerializeField] private float _flipOutSeconds = 0.26f;
        [SerializeField] private float _flipInSeconds = 0.35f;

        [Header("족보 때 튀기 (프로토타입 hop)")]
        [SerializeField] private float _hopHeight = 21f;
        [SerializeField] private float _hopSeconds = 0.38f;
        [SerializeField] private Vector4 _hopEase = new Vector4(0.2f, 1.6f, 0.4f, 1f);

        private float _hopClock = float.PositiveInfinity;
        private bool _rerollMode;
        private float _flipClock = -1f;
        private Sprite _pendingSprite;
        private RectTransform _faceRect;
        private CanvasGroup _group;
        private Vector2 _restPosition;
        private int _index;
        private bool _selected;
        private bool _dimmed;
        private bool _dealing;
        private float _dealTime;
        private bool _hovered;
        private float _hoverK;
        private float _swayClock;

        public event Action<int> Clicked;

        private void Awake()
        {
            // 레이아웃 그룹이 슬롯 루트의 위치를 덮어쓰므로, 올리는 연출은 자식(Face)에만 적용한다.
            _faceRect = _face.rectTransform;
            _restPosition = _faceRect.anchoredPosition;
            _group = _face.GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = _face.gameObject.AddComponent<CanvasGroup>();
            }
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

        public void SetSprite(Sprite sprite)
        {
            _face.sprite = sprite;
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyFace();
        }

        public void SetDimmed(bool dimmed)
        {
            _dimmed = dimmed;
            RefreshColor();
            if (dimmed)
            {
                _hovered = false;
            }
        }

        // 프로토타입의 rr: 리롤할 카드를 고르는 동안 모든 카드가 노랗게 물들고 살짝 기울어져 떠오른다.
        public void SetRerollMode(bool rerollMode)
        {
            _rerollMode = rerollMode;
            RefreshColor();
            ApplyFace();
        }

        // 족보 배너 때 카드가 위로 한 번 튀었다 내려온다(프로토타입의 translateY 0 → -16px → 0, 0.38초).
        public void PlayHop(float delay)
        {
            _hopClock = -delay;
        }

        // 올라갈 때와 내려올 때 각각 튕기는 곡선을 쓴다.
        private float GetHopOffset()
        {
            if (_hopClock <= 0f || _hopClock >= _hopSeconds)
            {
                return 0f;
            }

            float k = _hopClock / _hopSeconds;
            float ease = k < 0.5f
                ? UiEase.CubicBezier(_hopEase.x, _hopEase.y, _hopEase.z, _hopEase.w, k * 2f)
                : 1f - UiEase.CubicBezier(_hopEase.x, _hopEase.y, _hopEase.z, _hopEase.w, (k - 0.5f) * 2f);
            return _hopHeight * ease;
        }

        // 프로토타입의 flipout / flipin: 카드가 옆으로 좁아져 사라지고, 새 그림으로 바뀌어 다시 펴진다.
        public void PlayFlipTo(Sprite sprite)
        {
            _pendingSprite = sprite;
            _flipClock = 0f;
        }

        private void RefreshColor()
        {
            _face.color = _rerollMode ? _rerollColor : _dimmed ? _dimmedColor : Color.white;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHovered(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
        }

        // 제출 중이어서 흐려진 카드는 눌러도 아무 일이 없으므로 호버로 반응하지 않는다.
        public void SetHovered(bool hovered)
        {
            _hovered = hovered && !_dimmed && _button.interactable;
        }

        // 프로토타입의 deal: 오른쪽 아래에서 기울어진 채 작게 날아와 자리를 잡는다. 카드마다 시간차를 둔다.
        public void PlayDeal(float delay)
        {
            _dealTime = -delay;
            _dealing = true;
            ApplyFace();
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            _swayClock += dt;
            if (!float.IsPositiveInfinity(_hopClock))
            {
                _hopClock += dt;
                if (_hopClock >= _hopSeconds)
                {
                    _hopClock = float.PositiveInfinity;
                }
            }

            _hoverK = Mathf.MoveTowards(_hoverK, _hovered ? 1f : 0f, dt / _hoverSeconds);
            if (_flipClock >= 0f)
            {
                float before = _flipClock;
                _flipClock += dt;

                // 좁아진 순간에 그림을 바꿔서, 낡은 그림이 펴지는 것처럼 보이지 않게 한다.
                if (before < _flipOutSeconds && _flipClock >= _flipOutSeconds && _pendingSprite != null)
                {
                    _face.sprite = _pendingSprite;
                    _pendingSprite = null;
                }

                if (_flipClock >= _flipOutSeconds + _flipInSeconds)
                {
                    _flipClock = -1f;
                }
            }

            if (_dealing)
            {
                _dealTime += dt;
                if (_dealTime >= _dealDuration)
                {
                    _dealing = false;
                }
            }

            ApplyFace();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 날아오기·흔들림·호버·선택을 한 번에 합쳐 위치·기울기·크기·투명도를 정한다.
        private void ApplyFace()
        {
            float dealRemain = 0f;
            float alpha = 1f;
            if (_dealing)
            {
                float k = Mathf.Clamp01(_dealTime / _dealDuration);
                float progress = UiEase.CubicBezier(_dealEase.x, _dealEase.y, _dealEase.z, _dealEase.w, k);
                dealRemain = 1f - progress;
                alpha = _dealTime < 0f ? 0f : Mathf.Clamp01(progress * _dealFadeSpeed);
            }

            // 날아오는 중에는 흔들리지 않는다(프로토타입: .deal 중 제외).
            float sway = 0f;
            if (!_dealing)
            {
                float phase = (_swayClock + _index * _swayPhasePerIndex) / _swayPeriod;
                sway = _swayAmplitude * (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f;
            }

            float hover = Mathf.SmoothStep(0f, 1f, _hoverK);
            Vector2 position = _selected ? _restPosition + Vector2.up * _selectedOffsetY : _restPosition;
            float rerollLift = _rerollMode ? _rerollOffsetY : 0f;
            float rerollTilt = _rerollMode ? _rerollRotation : 0f;
            _faceRect.anchoredPosition = position + _dealOffset * dealRemain + Vector2.up * (sway + _hoverLift * hover + rerollLift + GetHopOffset());
            _faceRect.localRotation = Quaternion.Euler(0f, 0f, _dealStartRotation * dealRemain + _hoverRotation * hover + rerollTilt);

            float scale = Mathf.LerpUnclamped(1f, _dealStartScale, dealRemain);
            _faceRect.localScale = new Vector3(scale * GetFlipScaleX(), scale, scale);
            _group.alpha = alpha;
        }

        // 뒤집는 중에는 가로 폭만 줄었다 늘어난다. 나갈 때는 빠르게, 들어올 때는 부드럽게 퍼진다.
        private float GetFlipScaleX()
        {
            if (_flipClock < 0f)
            {
                return 1f;
            }

            if (_flipClock < _flipOutSeconds)
            {
                return Mathf.Cos(_flipClock / _flipOutSeconds * Mathf.PI * 0.5f);
            }

            float k = Mathf.Clamp01((_flipClock - _flipOutSeconds) / _flipInSeconds);
            return Mathf.Sin(k * Mathf.PI * 0.5f);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(_index);
        }
    }
}
