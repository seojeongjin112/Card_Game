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

        private RectTransform _faceRect;
        private Vector2 _restPosition;
        private int _index;

        public event Action<int> Clicked;

        private void Awake()
        {
            // 레이아웃 그룹이 슬롯 루트의 위치를 덮어쓰므로, 올리는 연출은 자식(Face)에만 적용한다.
            _faceRect = _face.rectTransform;
            _restPosition = _faceRect.anchoredPosition;
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
            _faceRect.anchoredPosition = selected
                ? _restPosition + Vector2.up * _selectedOffsetY
                : _restPosition;
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
