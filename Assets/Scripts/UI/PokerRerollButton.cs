using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    [RequireComponent(typeof(Button))]
    public class PokerRerollButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private string _labelFormat = "리롤 {0}/{1}";
        [SerializeField] private string _cancelLabel = "리롤 취소";

        public event Action Clicked;

        private void OnEnable()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        // 리롤 모드 중에는 같은 버튼이 취소 버튼이 되므로, 횟수가 있든 없든 취소는 눌러야 한다.
        public void SetState(int remaining, int maxCount, bool isRerollMode, bool canReroll)
        {
            _label.text = isRerollMode ? _cancelLabel : string.Format(_labelFormat, remaining, maxCount);
            _button.interactable = isRerollMode || canReroll;
        }

        private void HandleClick()
        {
            Clicked?.Invoke();
        }
    }
}
