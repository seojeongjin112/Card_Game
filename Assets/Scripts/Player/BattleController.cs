using System.Collections.Generic;
using UnityEngine;

namespace Poker_Card
{
    public class BattleController : MonoBehaviour
    {
        [SerializeField] private PokerHandView _handView;
        [SerializeField] private PokerCardDatabase _database;
        [SerializeField] private PokerHandScoreTable _scoreTable;
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private HpBarView _playerHpBar;
        [SerializeField] private HpBarView _enemyHpBar;
        [SerializeField] private EnemyHandView _enemyHandView;
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _handCount = 7;

        private PokerHandEvaluator _evaluator;
        private EnemyPokerHand _enemy;
        private BattleState _state;
        private PokerHandResult? _playerResult;
        private bool _isSettled;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_scoreTable);
            _enemy = new EnemyPokerHand(_database.Cards, _evaluator, _handCount);
            _state = new BattleState(_maxHp);
        }

        private void OnEnable()
        {
            _handView.HandSubmitted += OnHandSubmitted;
            _handView.SubmitCancelled += OnSubmitCancelled;
            _resultView.SettleClicked += OnSettleClicked;
            _resultView.NextClicked += OnNextClicked;
            _state.Changed += RefreshHp;
        }

        private void OnDisable()
        {
            _handView.HandSubmitted -= OnHandSubmitted;
            _handView.SubmitCancelled -= OnSubmitCancelled;
            _resultView.SettleClicked -= OnSettleClicked;
            _resultView.NextClicked -= OnNextClicked;
            _state.Changed -= RefreshHp;
        }

        // 플레이어의 첫 패는 PokerHandView가 직접 나누므로 여기서는 상대 패만 나눈다.
        private void Start()
        {
            _enemy.Deal();
            _enemyHandView.ShowBacks();
            _resultView.ShowRoundStart();
            RefreshHp();
        }

        private void OnHandSubmitted(IReadOnlyList<PokerCardData> cards)
        {
            _playerResult = _evaluator.Evaluate(cards);
            _resultView.SetSettleAvailable(true);
        }

        private void OnSubmitCancelled()
        {
            _playerResult = null;
            _resultView.SetSettleAvailable(false);
        }

        private void OnSettleClicked()
        {
            if (_isSettled || _playerResult == null)
            {
                return;
            }

            var enemyHand = _enemy.Best.Result;
            var outcome = BattleResolver.Resolve(_playerResult.Value.Score, enemyHand.Score);

            _isSettled = true;
            _state.Apply(outcome);
            _handView.SetLocked(true);
            _enemyHandView.Reveal(_enemy.Hand, _enemy.Best.Cards);
            _resultView.ShowRoundResult(outcome, enemyHand, _state.IsOver, _state.GetWinner());
        }

        private void OnNextClicked()
        {
            if (!_isSettled)
            {
                return;
            }

            if (_state.IsOver)
            {
                _state.Reset();
            }

            StartRound();
        }

        private void StartRound()
        {
            _isSettled = false;
            _playerResult = null;
            _enemy.Deal();
            _enemyHandView.ShowBacks();
            _handView.Deal();
            _resultView.ShowRoundStart();
        }

        private void RefreshHp()
        {
            _playerHpBar.SetHp(_state.PlayerHp, _state.MaxHp);
            _enemyHpBar.SetHp(_state.EnemyHp, _state.MaxHp);
        }
    }
}
