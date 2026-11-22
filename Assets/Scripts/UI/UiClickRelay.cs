using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Poker_Card
{
    // 눌림에 클릭 횟수를 붙여 알려 준다. 더블클릭으로 덱에 넣고 빼는 데 쓴다(Button은 횟수를 모른다).
    public class UiClickRelay : MonoBehaviour, IPointerClickHandler
    {
        private Action<int> _clicked;

        public void Initialize(Action<int> clicked)
        {
            _clicked = clicked;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _clicked?.Invoke(eventData.clickCount);
        }
    }
}
