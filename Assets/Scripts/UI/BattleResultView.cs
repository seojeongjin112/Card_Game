using System;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    public class BattleResultView : MonoBehaviour
    {
        [SerializeField] private Button _settleButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Text _nextLabel;
        [SerializeField] private Text _message;
        [SerializeField] private string _winFormat = "승리! 상대에게 {0} 피해 (상대: {1} · {2}점)";
        [SerializeField] private string _loseFormat = "패배... {0} 피해를 받았다 (상대: {1} · {2}점)";
        [SerializeField] private string _drawFormat = "무승부 (상대: {0} · {1}점)";
        [SerializeField] private string _victoryMessage = "전투 승리!";
        [SerializeField] private string _defeatMessage = "전투 패배";
        [SerializeField] private string _nextRoundLabel = "다음 라운드";
        [SerializeField] private string _restartLabel = "새 게임";

        public event Action SettleClicked;
        public event Action NextClicked;

        private void OnEnable()
        {
            _settleButton.onClick.AddListener(HandleSettleClicked);
            _nextButton.onClick.AddListener(HandleNextClicked);
        }

        private void OnDisable()
        {
            _settleButton.onClick.RemoveListener(HandleSettleClicked);
            _nextButton.onClick.RemoveListener(HandleNextClicked);
        }

        public void ShowRoundStart()
        {
            _message.text = string.Empty;
            _settleButton.gameObject.SetActive(true);
            _settleButton.interactable = false;
            _nextButton.gameObject.SetActive(false);
        }

        public void SetSettleAvailable(bool available)
        {
            _settleButton.interactable = available;
        }

        public void ShowRoundResult(BattleOutcome outcome, PokerHandResult enemyHand, bool isOver, BattleWinner finalWinner)
        {
            _message.text = BuildRoundMessage(outcome, enemyHand);
            if (isOver)
            {
                _message.text += "\n" + (finalWinner == BattleWinner.Player ? _victoryMessage : _defeatMessage);
            }

            _settleButton.gameObject.SetActive(false);
            _nextButton.gameObject.SetActive(true);
            _nextLabel.text = isOver ? _restartLabel : _nextRoundLabel;
        }

        // 결과 화면의 버튼처럼 다른 곳에서 "다음"과 같은 동작을 시켜야 할 때 쓴다.
        public void TriggerNext()
        {
            HandleNextClicked();
        }

        private string BuildRoundMessage(BattleOutcome outcome, PokerHandResult enemyHand)
        {
            switch (outcome.Winner)
            {
                case BattleWinner.Player:
                    return string.Format(_winFormat, outcome.Damage, enemyHand.Name, enemyHand.Score);
                case BattleWinner.Enemy:
                    return string.Format(_loseFormat, outcome.Damage, enemyHand.Name, enemyHand.Score);
                default:
                    return string.Format(_drawFormat, enemyHand.Name, enemyHand.Score);
            }
        }

        private void HandleSettleClicked()
        {
            SettleClicked?.Invoke();
        }

        private void HandleNextClicked()
        {
            NextClicked?.Invoke();
        }
    }
}
