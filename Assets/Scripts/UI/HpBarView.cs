using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform _fill;
        [SerializeField] private Text _label;
        [SerializeField] private string _format = "HP {0} / {1}";

        // 채움 막대는 스프라이트 없이도 그려지도록 앵커의 오른쪽 끝을 비율대로 옮겨 길이를 줄인다.
        public void SetHp(int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            _fill.anchorMax = new Vector2(ratio, _fill.anchorMax.y);
            _label.text = string.Format(_format, current, max);
        }
    }
}
