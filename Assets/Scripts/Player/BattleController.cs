using System.Collections.Generic;
using Core;
using Effect_Card;
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

        // 플레이어가 효과 카드를 고르고 쓰는 화면이 생기기 전에 켜면 상대만 효과를 쓰게 되므로 기본은 꺼 둔다.
        [SerializeField] private bool _useEffects;
        [SerializeField] private EffectCardDatabase _effectDatabase;
        [SerializeField] private EffectRuleSet _effectRuleSet;
        [SerializeField] private EnemyAiSettings _enemyAiSettings;
        [SerializeField] private EnemyDifficulty _difficulty = EnemyDifficulty.Normal;
        [SerializeField] private EffectHandView _effectHandView;
        [SerializeField] private EffectDraftView _effectDraftView;
        [SerializeField] private float _dealDelayAfterDraft = 0.6f;
        [SerializeField] private EnemyEffectView _enemyEffectView;
        [SerializeField] private EffectSettleSummaryView _settleSummaryView;
        [SerializeField] private EffectPileView _effectPileView;
        [SerializeField] private BattleProgressView _progressView;
        [SerializeField] private EffectBattleLogView _effectLogView;

        private int _round;
        private PokerHandEvaluator _evaluator;
        private EnemyPokerHand _enemy;
        private BattleState _state;
        private EffectBattleCoordinator _coordinator;
        private EnemyActiveAi _enemyAi;
        private PokerHandResult? _playerResult;
        private bool _isSettled;

        // 효과 카드 화면이 드래프트와 액티브 사용을 이어 가는 통로다. 효과를 끄면 null이다.
        public EffectBattleCoordinator Coordinator => _coordinator;

        // 연출이 피해·회복 사건을 직접 받아 갈 수 있게 전투 상태를 읽기 전용으로 내보낸다.
        public BattleState State => _state;
        public int Round => _round;

        // 라운드가 시작될 때마다 알린다. 늦게 켜진 쪽은 Round를 읽어 현재 라운드를 알 수 있다.
        public event System.Action<int> RoundStarted;

        // 전투가 끝났을 때(HP가 0이 되었을 때) 승자와 마지막 라운드 번호를 알린다. 무승부로 끝나면 Draw다.
        public event System.Action<BattleWinner, int> BattleEnded;

        private void Awake()
        {
            _evaluator = new PokerHandEvaluator(_scoreTable);
            _enemy = new EnemyPokerHand(_database.Cards, _evaluator, _handCount);
            _state = new BattleState(_maxHp);

            // 시작 화면에서 난이도를 골라 왔으면 그 값을 쓰고, 전투 씬만 바로 실행했으면 인스펙터 값을 그대로 쓴다.
            if (GameSession.HasDifficulty)
            {
                _difficulty = GameSession.Difficulty;
            }

            SetUpEffects();
        }

        public EnemyDifficulty Difficulty => _difficulty;

        private void SetUpEffects()
        {
            // 효과를 끈 전투에서는 빈 효과 카드 상자가 보이지 않게 숨긴다.
            if (_effectHandView != null)
            {
                _effectHandView.gameObject.SetActive(false);
            }

            if (_enemyEffectView != null)
            {
                _enemyEffectView.gameObject.SetActive(false);
            }

            if (_settleSummaryView != null)
            {
                _settleSummaryView.gameObject.SetActive(false);
            }

            if (_effectPileView != null)
            {
                _effectPileView.gameObject.SetActive(false);
            }

            if (_effectLogView != null)
            {
                _effectLogView.gameObject.SetActive(false);
            }

            if (!_useEffects)
            {
                return;
            }

            if (_effectDatabase == null || _effectRuleSet == null || _enemyAiSettings == null)
            {
                Debug.LogWarning("효과 카드 설정 에셋이 비어 있어 효과 없이 진행한다.", this);
                return;
            }

            var profile = _enemyAiSettings.GetProfile(_difficulty);
            _coordinator = new EffectBattleCoordinator(_state, _effectDatabase, _effectRuleSet, profile.PrefersHighGradeDraft);
            _enemyAi = new EnemyActiveAi(_enemyAiSettings, _difficulty);

            if (_effectHandView != null)
            {
                _effectHandView.gameObject.SetActive(true);
                _effectHandView.Bind(_coordinator);
            }

            // 드래프트 창은 스스로 열고 닫으므로 오브젝트는 항상 켜 두고 조정자만 연결한다.
            if (_effectDraftView != null)
            {
                _effectDraftView.Bind(_coordinator);
            }

            if (_enemyEffectView != null)
            {
                _enemyEffectView.gameObject.SetActive(true);
                _enemyEffectView.Bind(_coordinator);
            }

            if (_settleSummaryView != null)
            {
                _settleSummaryView.gameObject.SetActive(true);
                _settleSummaryView.Bind(_coordinator);
            }

            if (_effectPileView != null)
            {
                _effectPileView.gameObject.SetActive(true);
                _effectPileView.Bind(_coordinator);
            }

            if (_effectLogView != null)
            {
                _effectLogView.gameObject.SetActive(true);
                _effectLogView.Bind(_coordinator);
            }
        }

        private void OnEnable()
        {
            _handView.HandSubmitted += OnHandSubmitted;
            _handView.SubmitCancelled += OnSubmitCancelled;
            _resultView.SettleClicked += OnSettleClicked;
            _resultView.NextClicked += OnNextClicked;
            _state.Changed += RefreshHp;

            // 드래프트가 닫히면 단계가 "전투 패 구성"으로 넘어가야 하므로 조정자의 변경 알림도 받는다.
            if (_coordinator != null)
            {
                _coordinator.Changed += RefreshProgress;
                _coordinator.Player.Draft.Closed += OnDraftClosed;
            }
        }

        // 프로토타입은 효과 카드를 고른 뒤에 패를 나눠 준다. 드래프트 창에 가려진 채 끝난 나눔 연출을 창이 닫힐 때 다시 보여 준다.
        private void OnDraftClosed()
        {
            _handView.ReplayDeal(_dealDelayAfterDraft);
            _enemyHandView.ShowBacks(_dealDelayAfterDraft);
        }

        private void OnDisable()
        {
            _handView.HandSubmitted -= OnHandSubmitted;
            _handView.SubmitCancelled -= OnSubmitCancelled;
            _resultView.SettleClicked -= OnSettleClicked;
            _resultView.NextClicked -= OnNextClicked;
            _state.Changed -= RefreshHp;

            if (_coordinator != null)
            {
                _coordinator.Changed -= RefreshProgress;
                _coordinator.Player.Draft.Closed -= OnDraftClosed;
            }
        }

        // 플레이어의 첫 패는 PokerHandView가 직접 나누므로 여기서는 상대 패만 나눈다.
        private void Start()
        {
            _enemy.Deal();
            _enemyHandView.ShowBacks();
            _resultView.ShowRoundStart();
            RefreshHp();
            _round = 1;
            StartEffectRound();
            RefreshProgress();
            RoundStarted?.Invoke(_round);
        }

        // 정산 때 가운데 점수판이 상대가 낸 5장을 보여 주는 데 쓴다.
        public IReadOnlyList<PokerCardData> EnemyBestCards => _enemy.Best.Cards;

        private void OnHandSubmitted(IReadOnlyList<PokerCardData> cards)
        {
            _playerResult = _evaluator.Evaluate(cards);
            _resultView.SetSettleAvailable(true);
            RefreshProgress();
        }

        private void OnSubmitCancelled()
        {
            _playerResult = null;
            _resultView.SetSettleAvailable(false);
            RefreshProgress();
        }

        private void OnSettleClicked()
        {
            if (_isSettled || _playerResult == null)
            {
                return;
            }

            var enemyHand = _enemy.Best.Result;
            BattleOutcome outcome;

            if (_coordinator != null)
            {
                // 효과 쪽 정산은 피해를 직접 적용하므로 Apply를 다시 부르면 이중으로 깎인다.
                _enemyAi.UseActives(_coordinator, enemyHand);
                var settled = _coordinator.Settle(_playerResult.Value, enemyHand);
                outcome = settled.Outcome;

                // 결과 문구가 상대의 최종 점수(효과 반영)를 보여 주도록 점수만 바꿔 넘긴다.
                enemyHand = new PokerHandResult(enemyHand.HandRank, enemyHand.Name, settled.EnemyScore);
            }
            else
            {
                outcome = BattleResolver.Resolve(_playerResult.Value.Score, enemyHand.Score);
                _state.Apply(outcome);
            }

            _isSettled = true;
            _handView.SetLocked(true);
            _enemyHandView.Reveal(_enemy.Hand, _enemy.Best.Cards);
            _resultView.ShowRoundResult(outcome, enemyHand, _state.IsOver, _state.GetWinner());
            _coordinator?.EndRound();
            RefreshProgress();

            if (_state.IsOver)
            {
                BattleEnded?.Invoke(_state.GetWinner(), _round);
            }
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
                _coordinator?.StartNewBattle();
                _round = 0;
            }

            StartRound();
        }

        private void StartRound()
        {
            _round++;
            _isSettled = false;
            _playerResult = null;
            _enemy.Deal();
            _enemyHandView.ShowBacks();
            _handView.Deal();
            _resultView.ShowRoundStart();
            StartEffectRound();
            RefreshProgress();
            RoundStarted?.Invoke(_round);
        }

        // 단계는 따로 저장하지 않고 지금 상태에서 매번 계산해서, 호출 순서가 꼬여도 화면이 어긋나지 않게 한다.
        private void RefreshProgress()
        {
            if (_progressView == null)
            {
                return;
            }

            BattlePhase phase;
            if (_isSettled)
            {
                phase = BattlePhase.Settled;
            }
            else if (_playerResult != null)
            {
                phase = BattlePhase.Ready;
            }
            else if (_coordinator != null && _coordinator.Player.Draft.IsOpen)
            {
                phase = BattlePhase.Draft;
            }
            else
            {
                phase = BattlePhase.Compose;
            }

            _progressView.Show(_round, phase);
        }

        private void StartEffectRound()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.StartRound();

            // 라운드 시작의 지속 피해로 전투가 끝났다면 정산 없이 바로 결과를 보여 준다.
            if (_state.IsOver)
            {
                var winner = _state.GetWinner();
                _isSettled = true;
                _handView.SetLocked(true);
                _resultView.ShowRoundResult(new BattleOutcome(winner, 0), _enemy.Best.Result, true, winner);
                BattleEnded?.Invoke(winner, _round);
            }
        }

        private void RefreshHp()
        {
            _playerHpBar.SetHp(_state.PlayerHp, _state.MaxHp);
            _enemyHpBar.SetHp(_state.EnemyHp, _state.MaxHp);
        }
    }
}
