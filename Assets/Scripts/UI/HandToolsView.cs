using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 손패 위 도구 줄: 선택한 장수와, 5장을 골랐을 때의 현재 족보를 실시간으로 보여 준다.
    public class HandToolsView : MonoBehaviour
    {
        [SerializeField] private PokerHandView _handView;
        [SerializeField] private PokerHandScoreTable _scoreTable;
        [SerializeField] private Text _summaryText;
        [SerializeField] private Text _rankText;
        [SerializeField] private string _summaryFormat = "전투 패 {0}장 · 선택 {1} / {2}";
        [SerializeField] private string _rankFormat = "현재 족보  {0}";
        [SerializeField] private string _rankEmptyText = "—";
        [SerializeField] private string _rankValueFormat = "{0} · {1}점";

        private PokerHandEvaluator _evaluator;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_scoreTable);
        }

        private void OnEnable()
        {
            _handView.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            _handView.Changed -= Refresh;
        }

        private void Refresh()
        {
            _summaryText.text = string.Format(_summaryFormat, _handView.HandCount, _handView.SelectedCount, _handView.MaxSelectCount);

            // 족보 판정은 정확히 5장이 있어야 하므로, 다 고르기 전에는 빈 값으로 둔다.
            string rank = _rankEmptyText;
            if (_handView.SelectedCount == _handView.MaxSelectCount)
            {
                var result = _evaluator.Evaluate(_handView.GetSelectedCards());
                rank = string.Format(_rankValueFormat, result.Name, result.Score);
            }

            _rankText.text = string.Format(_rankFormat, rank);
        }
    }
}
