using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    [RequireComponent(typeof(Button))]
    public class PokerSubmitButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private string _submitLabel = "제출 확인";
        [SerializeField] private string _cancelLabel = "제출 취소";

        public event Action Clicked;

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        // 제출 후에는 같은 버튼이 취소 버튼 역할을 하므로, 취소는 조건 없이 항상 누를 수 있어야 한다.
        public void SetState(bool canSubmit, bool isSubmitted)
        {
            _label.text = isSubmitted ? _cancelLabel : _submitLabel;
            _button.interactable = isSubmitted || canSubmit;
        }

        private void HandleClick()
        {
            Clicked?.Invoke();
        }
    }
}
