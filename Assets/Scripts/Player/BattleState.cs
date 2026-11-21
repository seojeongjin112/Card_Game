using System;
using Core;
using Effect_Card;

namespace Poker_Card
{
    public class BattleState
    {
        public int MaxHp { get; }

        // 효과 카드 처리기는 HP 숫자가 아니라 이 상태 객체를 직접 다룬다.
        public CombatantState Player { get; }
        public CombatantState Enemy { get; }

        // 효과 쪽에서는 HP가 0 아래로 내려갈 수 있어서, 기존 화면과 판정이 보는 값은 0에서 멈춘다.
        public int PlayerHp => Math.Max(0, Player.Hp);
        public int EnemyHp => Math.Max(0, Enemy.Hp);

        public event Action Changed;

        public bool IsOver => PlayerHp <= 0 || EnemyHp <= 0;

        // 두 상태 객체는 이 클래스와 수명이 같아서 구독을 따로 해제하지 않는다.
        public BattleState(int maxHp)
        {
            MaxHp = maxHp;
            Player = new CombatantState(maxHp);
            Enemy = new CombatantState(maxHp);
            Player.Changed += OnCombatantChanged;
            Enemy.Changed += OnCombatantChanged;
        }

        public void Apply(BattleOutcome outcome)
        {
            if (outcome.Winner == BattleWinner.Player)
            {
                Enemy.LoseHp(outcome.Damage);
            }
            else if (outcome.Winner == BattleWinner.Enemy)
            {
                Player.LoseHp(outcome.Damage);
            }
            else
            {
                Changed?.Invoke();
            }
        }

        public void Reset()
        {
            Player.Reset();
            Enemy.Reset();
        }

        // 전투가 끝나지 않았으면 Draw를 돌려준다.
        public BattleWinner GetWinner()
        {
            if (EnemyHp <= 0 && PlayerHp > 0)
            {
                return BattleWinner.Player;
            }

            if (PlayerHp <= 0 && EnemyHp > 0)
            {
                return BattleWinner.Enemy;
            }

            return BattleWinner.Draw;
        }

        private void OnCombatantChanged()
        {
            Changed?.Invoke();
        }
    }
}
