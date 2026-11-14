using Core;

namespace Poker_Card
{
    public readonly struct BattleOutcome
    {
        public BattleWinner Winner { get; }
        public int Damage { get; }

        public BattleOutcome(BattleWinner winner, int damage)
        {
            Winner = winner;
            Damage = damage;
        }
    }
}
