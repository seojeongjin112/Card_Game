using System.Collections.Generic;
using System.Globalization;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 정산 뒤에 양쪽 최종 점수가 어떻게 나왔는지 풀어서 보여 준다. 다음 라운드가 시작되면 지운다.
    public class EffectSettleSummaryView : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private string _playerLabel = "나";
        [SerializeField] private string _enemyLabel = "상대";
        [SerializeField] private string _scoreFormat = "{0} {1}점 (기본 {2} + 보너스 {3}) × {4}";
        [SerializeField] private string _critSuffix = " · 치명타!";
        [SerializeField] private string _usedFormat = "사용: {0}";
        [SerializeField] private string _noneText = "없음";

        [SerializeField] private float _countDuration = 0.8f;
        [SerializeField] private float _pulseScale = 0.04f;

        private EffectBattleCoordinator _coordinator;
        private EffectBattleCoordinator.SettleResult _result;
        private float _elapsed;
        private bool _counting;
        private bool _directed;
        private bool _released;
        private bool _hasPending;
        private int _shownPlayer = -1;
        private int _shownEnemy = -1;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // BattleController가 조정자를 만든 뒤에 한 번 연결해 준다.
        public void Bind(EffectBattleCoordinator coordinator)
        {
            Unsubscribe();
            _coordinator = coordinator;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            _text.text = string.Empty;
        }

        // Bind와 OnEnable 중 무엇이 먼저 불려도 한 번만 구독되도록 먼저 해제하고 구독한다.
        private void Subscribe()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.Settled -= OnSettled;
            _coordinator.Settled += OnSettled;
            _coordinator.Changed -= OnChanged;
            _coordinator.Changed += OnChanged;
        }

        private void Unsubscribe()
        {
            if (_coordinator == null)
            {
                return;
            }

            _coordinator.Settled -= OnSettled;
            _coordinator.Changed -= OnChanged;
        }

        // 라운드가 다시 열렸을 때만 지운다. 정산 중에도 Changed가 오지만 그때는 라운드가 닫혀 있다.
        private void OnChanged()
        {
            if (_coordinator.IsRoundOpen)
            {
                _counting = false;
                _hasPending = false;
                _released = false;
                transform.localScale = Vector3.one;
                _text.text = string.Empty;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 프로토타입처럼 처음엔 0에서 시작해 빠르게 올라가다 천천히 멈추는(ease-out) 숫자 연출. 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            if (!_counting)
            {
                return;
            }

            _elapsed += dt;
            float k = Mathf.Min(1f, _elapsed / _countDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            int player = Mathf.RoundToInt(_result.PlayerScore * eased);
            int enemy = Mathf.RoundToInt(_result.EnemyScore * eased);
            if (k >= 1f)
            {
                _counting = false;
                transform.localScale = Vector3.one;
            }
            else
            {
                transform.localScale = Vector3.one * (1f + _pulseScale * (1f - k));
            }

            // 값이 바뀐 프레임에만 글자를 다시 만들어서 문자열 할당을 줄인다.
            if (player != _shownPlayer || enemy != _shownEnemy || !_counting)
            {
                Render(player, enemy);
            }
        }

        public float CountDuration => _countDuration;

        // 연출 감독이 있으면 패시브 발동 연출이 끝난 뒤에 숫자를 올리도록 시작을 맡긴다. 감독이 없으면 정산 즉시 올린다.
        public void SetDirected(bool directed)
        {
            _directed = directed;
        }

        // 감독이 호출하는 시작 신호. 결과가 아직 안 왔으면(같은 정산 안에서 신호가 먼저 올 수 있다) 도착하는 즉시 시작한다.
        public void BeginCount()
        {
            _released = true;
            if (_hasPending)
            {
                StartCount();
            }
        }

        private void OnSettled(EffectBattleCoordinator.SettleResult result)
        {
            _result = result;
            _hasPending = true;
            _text.text = string.Empty;
            if (!_directed || _released)
            {
                StartCount();
            }
        }

        private void StartCount()
        {
            _hasPending = false;
            _released = false;
            _elapsed = 0f;
            _counting = _countDuration > 0f;
            if (_counting)
            {
                Render(0, 0);
            }
            else
            {
                Render(_result.PlayerScore, _result.EnemyScore);
            }
        }

        private void Render(int playerScore, int enemyScore)
        {
            _shownPlayer = playerScore;
            _shownEnemy = enemyScore;
            _text.text = BuildSide(_playerLabel, playerScore, _result.PlayerDetail, _result.PlayerCrit, _result.PlayerUsedCards)
                + "\n" + BuildSide(_enemyLabel, enemyScore, _result.EnemyDetail, _result.EnemyCrit, _result.EnemyUsedCards);
        }

        private string BuildSide(string label, int finalScore, EffectScoreCalculator.Result detail, bool crit,
            IReadOnlyList<EffectCardData> used)
        {
            string multiplier = detail.Multiplier.ToString("0.##", CultureInfo.InvariantCulture);
            string line = string.Format(_scoreFormat, label, finalScore, detail.BaseScore, detail.Bonus, multiplier);
            if (crit)
            {
                line += _critSuffix;
            }

            return line + "\n" + string.Format(_usedFormat, JoinNames(used));
        }

        private string JoinNames(IReadOnlyList<EffectCardData> cards)
        {
            if (cards.Count == 0)
            {
                return _noneText;
            }

            var names = new string[cards.Count];
            for (int i = 0; i < cards.Count; i++)
            {
                names[i] = cards[i].DisplayName;
            }

            return string.Join(", ", names);
        }
    }
}
