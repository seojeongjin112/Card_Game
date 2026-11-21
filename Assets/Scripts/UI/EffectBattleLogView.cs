using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 효과 카드와 관련해 일어난 일을 최신순으로 보여 주는 전투 로그. 사건이 올 때만 갱신한다.
    public class EffectBattleLogView : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private int _maxLines = 40;
        [SerializeField] private string _lineFormat = "R{0}  {1}";
        [SerializeField] private string _playerLabel = "나";
        [SerializeField] private string _enemyLabel = "상대";
        [SerializeField] private string _battleStartedText = "전투 시작";
        [SerializeField] private string _playerDraftFormat = "획득 · {0}";
        [SerializeField] private string _enemyDraftFormat = "상대가 효과 카드를 획득 · 장착 {0}";
        [SerializeField] private string _enemyEpicText = "상대가 ◆영웅 카드를 획득";
        [SerializeField] private string _enemyLegendText = "상대가 ★전설 카드를 획득했습니다!";
        [SerializeField] private string _usedFormat = "{0} · {1} 사용";
        [SerializeField] private string _nullifiedFormat = "{0} · {1} 무효 (함정)";
        [SerializeField] private string _dotFormat = "{0} · 지속 피해 -{1}";
        [SerializeField] private string _critFormat = "{0} 치명타!";
        [SerializeField] private string _playerWinFormat = "내 승리 · {0} - {1} = {2} → 상대 HP -{3}";
        [SerializeField] private string _enemyWinFormat = "상대 승리 · {0} - {1} = {2} → 나 HP -{3}";
        [SerializeField] private string _drawText = "무승부 · 피해 없음";
        [SerializeField] private string _expiredFormat = "{0} · {1} 효과 종료";
        [SerializeField] private string _undyingFormat = "{0} · 불사 발동 · HP 1";

        private readonly List<string> _lines = new List<string>();
        private EffectBattleCoordinator _coordinator;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // BattleController가 조정자를 만든 뒤에 한 번 연결해 준다. 첫 전투 시작 사건은 연결 전에 지나갔으므로 직접 채운다.
        public void Bind(EffectBattleCoordinator coordinator)
        {
            Unsubscribe();
            _coordinator = coordinator;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            _lines.Clear();
            Add(_battleStartedText);
        }

        // Bind와 OnEnable 중 무엇이 먼저 불려도 한 번만 구독되도록 먼저 해제하고 구독한다.
        private void Subscribe()
        {
            if (_coordinator != null)
            {
                _coordinator.Logged -= OnLogged;
                _coordinator.Logged += OnLogged;
            }
        }

        private void Unsubscribe()
        {
            if (_coordinator != null)
            {
                _coordinator.Logged -= OnLogged;
            }
        }

        private void OnLogged(EffectLogEntry entry)
        {
            // 새 게임이 시작되면 지난 전투의 기록은 지운다.
            if (entry.Kind == EffectLogKind.BattleStarted)
            {
                _lines.Clear();
                Add(_battleStartedText);
                return;
            }

            Add(string.Format(_lineFormat, entry.Round, Describe(entry)));
        }

        private string Describe(EffectLogEntry entry)
        {
            string who = entry.IsPlayer ? _playerLabel : _enemyLabel;
            switch (entry.Kind)
            {
                case EffectLogKind.Drafted:
                    return DescribeDraft(entry);
                case EffectLogKind.ActiveUsed:
                    return string.Format(_usedFormat, who, entry.Card.DisplayName);
                case EffectLogKind.ActiveNullified:
                    return string.Format(_nullifiedFormat, who, entry.Card.DisplayName);
                case EffectLogKind.DotDamage:
                    return string.Format(_dotFormat, who, entry.Value1);
                case EffectLogKind.CriticalHit:
                    return string.Format(_critFormat, who);
                case EffectLogKind.Settled:
                    return DescribeSettle(entry);
                case EffectLogKind.ActiveExpired:
                    return string.Format(_expiredFormat, who, entry.Card.DisplayName);
                case EffectLogKind.UndyingTriggered:
                    return string.Format(_undyingFormat, who);
                default:
                    return string.Empty;
            }
        }

        // 상대가 뭘 골랐는지는 숨기고, 높은 등급을 얻었을 때만 경고하듯 알린다(프로토타입 규칙).
        private string DescribeDraft(EffectLogEntry entry)
        {
            if (entry.IsPlayer)
            {
                return string.Format(_playerDraftFormat, entry.Card.DisplayName);
            }

            if (entry.Card.Grade == EffectGrade.Legend)
            {
                return _enemyLegendText;
            }

            return entry.Card.Grade == EffectGrade.Epic
                ? _enemyEpicText
                : string.Format(_enemyDraftFormat, entry.Value1);
        }

        private string DescribeSettle(EffectLogEntry entry)
        {
            int playerScore = entry.Value1;
            int enemyScore = entry.Value2;
            int diff = Mathf.Abs(playerScore - enemyScore);

            switch (entry.Winner)
            {
                case BattleWinner.Player:
                    return string.Format(_playerWinFormat, playerScore, enemyScore, diff, entry.Value3);
                case BattleWinner.Enemy:
                    return string.Format(_enemyWinFormat, enemyScore, playerScore, diff, entry.Value3);
                default:
                    return _drawText;
            }
        }

        private void Add(string line)
        {
            _lines.Insert(0, line);
            if (_lines.Count > _maxLines)
            {
                _lines.RemoveAt(_lines.Count - 1);
            }

            _text.text = string.Join("\n", _lines);
        }
    }
}
