using System;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 효과 카드 한 장을 글자로 보여 주는 임시 슬롯. 디자인이 나오면 이 클래스만 이미지 기반으로 바꾼다.
    [RequireComponent(typeof(Button))]
    public class EffectCardSlotView : MonoBehaviour
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

        private int _index;

        public event Action<int> Clicked;

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

        public void Show(EffectCardData card, string status, bool dimmed)
        {
            int grade = (int)card.Grade;
            _label.text = string.Format(_cardFormat, _gradeLabels[grade], card.DisplayName, status);
            _background.color = dimmed ? _gradeColors[grade] * _dimmedTint : _gradeColors[grade];
        }

        public void ShowEmpty()
        {
            _label.text = _emptyText;
            _background.color = _emptyColor;
        }

        // 카드 얼굴을 따로 그리는 쪽이 이 슬롯을 클릭 영역으로만 쓰도록 자기 그림과 글자를 숨긴다.
        public void HideVisuals()
        {
            _background.color = Color.clear;
            _label.enabled = false;
        }

        // 상대의 아직 쓰지 않은 액티브처럼 정체를 숨겨야 하는 카드에 쓴다.
        public void ShowHidden(string text)
        {
            _label.text = text;
            _background.color = _emptyColor;
        }

        // 눌러서 쓸 수 있는 카드만 버튼이 반응하게 해서 패시브나 사용한 카드를 눌러도 아무 일이 없음을 보여 준다.
        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        // 레이아웃 그룹은 자식의 scale을 무시하므로, 선택 강조는 크기만 키워도 다른 슬롯 위치가 밀리지 않는다.
        public void SetSelected(bool selected)
        {
            transform.localScale = Vector3.one * (selected ? _selectedScale : 1f);
        }

        private void HandleClick()
        {
            Clicked?.Invoke(_index);
        }
    }
}
