using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    public class PokerHandResultView : MonoBehaviour
    {
        [SerializeField] private PokerHandView _handView;
        [SerializeField] private PokerHandScoreTable _scoreTable;
        [SerializeField] private Text _label;
        [SerializeField] private string _format = "{0} · {1}점";

        private PokerHandEvaluator _evaluator;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_scoreTable);
            _label.text = string.Empty;
        }

        private void OnEnable()
        {
            _handView.HandSubmitted += OnHandSubmitted;
            _handView.SubmitCancelled += OnSubmitCancelled;
        }

        private void OnDisable()
        {
            _handView.HandSubmitted -= OnHandSubmitted;
            _handView.SubmitCancelled -= OnSubmitCancelled;
        }

        private void OnHandSubmitted(IReadOnlyList<PokerCardData> cards)
        {
            var result = _evaluator.Evaluate(cards);
            _label.text = string.Format(_format, result.Name, result.Score);
        }

        private void OnSubmitCancelled()
        {
            _label.text = string.Empty;
        }
    }
}
