using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    [RequireComponent(typeof(Button))]
    public class PokerCardSlot : MonoBehaviour
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

        private RectTransform _faceRect;
        private CanvasGroup _group;
        private Vector2 _restPosition;
        private int _index;
        private bool _selected;
        private bool _dealing;
        private float _dealTime;

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
            ApplyPosition(_dealing ? 1f - UiEase.CubicBezier(_dealEase.x, _dealEase.y, _dealEase.z, _dealEase.w, _dealTime / _dealDuration) : 0f);
        }

        // 프로토타입의 deal: 오른쪽 아래에서 기울어진 채 작게 날아와 자리를 잡는다. 카드마다 시간차를 둔다.
        public void PlayDeal(float delay)
        {
            _dealTime = -delay;
            _dealing = true;
            ApplyDeal(0f);
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            if (!_dealing)
            {
                return;
            }

            _dealTime += dt;
            float k = Mathf.Clamp01(_dealTime / _dealDuration);
            ApplyDeal(UiEase.CubicBezier(_dealEase.x, _dealEase.y, _dealEase.z, _dealEase.w, k));
            if (k >= 1f)
            {
                _dealing = false;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // progress 0은 출발 모습, 1은 도착 모습이다. 선택으로 올라간 높이는 도착 위치에 더해서, 날아오는 중에 눌러도 어긋나지 않게 한다.
        private void ApplyDeal(float progress)
        {
            if (_dealTime < 0f)
            {
                progress = 0f;
            }

            float remain = 1f - progress;
            _faceRect.localRotation = Quaternion.Euler(0f, 0f, _dealStartRotation * remain);
            _faceRect.localScale = Vector3.one * Mathf.LerpUnclamped(1f, _dealStartScale, remain);
            if (_group != null)
            {
                _group.alpha = _dealing && _dealTime < 0f ? 0f : Mathf.Clamp01(progress * _dealFadeSpeed);
            }

            ApplyPosition(remain);
        }

        private void ApplyPosition(float remain)
        {
            Vector2 position = _selected ? _restPosition + Vector2.up * _selectedOffsetY : _restPosition;
            _faceRect.anchoredPosition = position + _dealOffset * remain;
        }

        public void SetDimmed(bool dimmed)
        {
            _face.color = dimmed ? _dimmedColor : Color.white;
        }

        private void HandleClick()
        {
            Clicked?.Invoke(_index);
        }
    }
}
