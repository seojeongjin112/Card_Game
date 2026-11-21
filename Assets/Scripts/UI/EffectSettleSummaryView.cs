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

        private EffectBattleCoordinator _coordinator;

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
                _text.text = string.Empty;
            }
        }

        private void OnSettled(EffectBattleCoordinator.SettleResult result)
        {
            _text.text = BuildSide(_playerLabel, result.PlayerScore, result.PlayerDetail, result.PlayerCrit, result.PlayerUsedCards)
                + "\n" + BuildSide(_enemyLabel, result.EnemyScore, result.EnemyDetail, result.EnemyCrit, result.EnemyUsedCards);
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
