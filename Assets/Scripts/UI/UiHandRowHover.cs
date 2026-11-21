using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Poker_Card
{
    // 족보 점수표의 한 줄 위에 마우스가 올라오고 나가는 것을 점수표에 알려 준다.
    public class UiHandRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private int _index;
        private Action<int> _entered;
        private Action<int> _exited;

        public void Initialize(int index, Action<int> entered, Action<int> exited)
        {
            _index = index;
            _entered = entered;
            _exited = exited;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _entered?.Invoke(_index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _exited?.Invoke(_index);
        }
    }
}
