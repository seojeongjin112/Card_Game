using System;
using Core;

namespace Poker_Card
{
    public class BattleState
    {
        public int MaxHp { get; }
        public int PlayerHp { get; private set; }
        public int EnemyHp { get; private set; }

        public event Action Changed;

        public bool IsOver => PlayerHp <= 0 || EnemyHp <= 0;

        public BattleState(int maxHp)
        {
            MaxHp = maxHp;
            PlayerHp = maxHp;
            EnemyHp = maxHp;
        }

        public void Apply(BattleOutcome outcome)
        {
            if (outcome.Winner == BattleWinner.Player)
            {
                EnemyHp = Math.Max(0, EnemyHp - outcome.Damage);
            }
            else if (outcome.Winner == BattleWinner.Enemy)
            {
                PlayerHp = Math.Max(0, PlayerHp - outcome.Damage);
            }

            Changed?.Invoke();
        }

        public void Reset()
        {
            PlayerHp = MaxHp;
            EnemyHp = MaxHp;
            Changed?.Invoke();
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
    }
}
