using Core;

namespace Poker_Card
{
    public static class BattleResolver
    {
        // 점수가 높은 쪽이 이기고, 진 쪽은 점수 차이만큼 피해를 입는다. 동점이면 피해가 없다.
        public static BattleOutcome Resolve(int playerScore, int enemyScore)
        {
            if (playerScore > enemyScore)
            {
                return new BattleOutcome(BattleWinner.Player, playerScore - enemyScore);
            }

            if (enemyScore > playerScore)
            {
                return new BattleOutcome(BattleWinner.Enemy, enemyScore - playerScore);
            }

            return new BattleOutcome(BattleWinner.Draw, 0);
        }
    }
}
