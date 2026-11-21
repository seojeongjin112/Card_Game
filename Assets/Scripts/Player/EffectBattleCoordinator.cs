using Core;
using Poker_Card;
using UnityEngine;

namespace Effect_Card
{
    // 효과 처리기들을 라운드 흐름(시작, 액티브 사용, 정산, 종료)에 맞춰 호출하는 조정자.
    // BattleController는 이 네 시점에서만 이 클래스를 부르면 된다.
    public class EffectBattleCoordinator
    {
        public enum ActiveUseResult
        {
            Used,
            Nullified,
            Unusable,
            NoMirrorTarget
        }

        public readonly struct SettleResult
        {
            public int PlayerScore { get; }
            public int EnemyScore { get; }
            public bool PlayerCrit { get; }
            public bool EnemyCrit { get; }
            public BattleOutcome Outcome { get; }

            public SettleResult(int playerScore, int enemyScore, bool playerCrit, bool enemyCrit, BattleOutcome outcome)
            {
                PlayerScore = playerScore;
                EnemyScore = enemyScore;
                PlayerCrit = playerCrit;
                EnemyCrit = enemyCrit;
                Outcome = outcome;
            }
        }

        private readonly BattleState _state;
        private readonly EffectCardDatabase _database;
        private readonly EffectRuleSet _ruleSet;
        private readonly EffectDeckRules _deckRules;
        private readonly EffectScoreCalculator _calculator;
        private readonly EffectDefenseHandler _defense;
        private readonly EffectDisruptHandler _disrupt;
        private readonly EffectUtilityHandler _utility;
        private readonly EffectSettlementHandler _settlement;
        private readonly EffectDrawModifiers _drawModifiers;
        private readonly EnemyEffectDrafter _enemyDrafter;

        public EffectSideContext Player { get; private set; }
        public EffectSideContext Enemy { get; private set; }
        public int Round { get; private set; }

        public EffectBattleCoordinator(BattleState state, EffectCardDatabase database, EffectRuleSet ruleSet, bool enemyPrefersHighGrade)
        {
            _state = state;
            _database = database;
            _ruleSet = ruleSet;
            _deckRules = new EffectDeckRules(ruleSet);
            _calculator = new EffectScoreCalculator(ruleSet);
            _defense = new EffectDefenseHandler(ruleSet);
            _disrupt = new EffectDisruptHandler();
            _utility = new EffectUtilityHandler();
            _settlement = new EffectSettlementHandler(_defense);
            _drawModifiers = new EffectDrawModifiers(ruleSet);
            _enemyDrafter = new EnemyEffectDrafter(enemyPrefersHighGrade);

            StartNewBattle();
        }

        // 새 게임을 시작할 때마다 덱을 다시 뽑고 시작 카드를 나눠 준다. BattleState.Reset과 함께 부른다.
        public void StartNewBattle()
        {
            Round = 0;
            Player = CreateSide(_state.Player);
            Enemy = CreateSide(_state.Enemy);
        }

        // 플레이어의 드래프트가 열렸으면 true. 선택은 UI가 Player.Draft로 이어 간다.
        public bool StartRound()
        {
            Round++;
            StartRoundFor(Player);
            StartRoundFor(Enemy);

            _defense.TryUndying(Player.State, Player.GetActivePassives());
            _defense.TryUndying(Enemy.State, Enemy.GetActivePassives());

            _enemyDrafter.Draft(Enemy.Draft);
            return Player.Draft.Open();
        }

        public ActiveUseResult TryUseActive(bool isPlayer, EffectCardData card)
        {
            var self = isPlayer ? Player : Enemy;
            var opponent = isPlayer ? Enemy : Player;

            if (!self.Tracker.CanUse(card))
            {
                return ActiveUseResult.Unusable;
            }

            // 복제할 카드가 없으면 거울을 쓴 것으로 치지 않아야 라운드 사용 기회를 잃지 않는다.
            var effectiveCard = card;
            if (card.HasEffect(EffectKind.Mirror))
            {
                effectiveCard = _utility.GetMirrorTarget(self.Tracker);
                if (effectiveCard == null)
                {
                    return ActiveUseResult.NoMirrorTarget;
                }
            }

            self.Tracker.TryUse(card);

            var trap = _disrupt.ResolveActiveUse(effectiveCard, self.State, opponent.State, opponent.GetActivePassives());
            if (trap.Nullified)
            {
                return ActiveUseResult.Nullified;
            }

            _defense.ApplyActive(effectiveCard, self.State);
            _utility.ApplyRefit(effectiveCard, opponent.Tracker);
            self.UsedThisRound.Add(effectiveCard);

            if (effectiveCard.Category.HasFlag(EffectCategory.Utility) || effectiveCard.Category.HasFlag(EffectCategory.Defense))
            {
                self.UtilityUses++;
            }

            _utility.TryRecycle(card, self.GetActivePassives(), self.Tracker);
            return ActiveUseResult.Used;
        }

        public SettleResult Settle(PokerHandResult playerHand, PokerHandResult enemyHand)
        {
            var playerCalc = Calculate(Player, Enemy, playerHand);
            var enemyCalc = Calculate(Enemy, Player, enemyHand);

            bool playerCrit = RollCrit(playerCalc.CritChance);
            bool enemyCrit = RollCrit(enemyCalc.CritChance);
            int playerScore = playerCrit ? _calculator.ApplyCrit(playerCalc.FinalScore) : playerCalc.FinalScore;
            int enemyScore = enemyCrit ? _calculator.ApplyCrit(enemyCalc.FinalScore) : enemyCalc.FinalScore;

            var winner = playerScore > enemyScore ? BattleWinner.Player
                : enemyScore > playerScore ? BattleWinner.Enemy
                : BattleWinner.Draw;

            int totalDamage = 0;
            if (winner != BattleWinner.Draw)
            {
                bool playerWon = winner == BattleWinner.Player;
                var winnerSide = playerWon ? Player : Enemy;
                var loserSide = playerWon ? Enemy : Player;
                var win = _settlement.ResolveWin(loserSide.State, Mathf.Abs(playerScore - enemyScore),
                    winnerSide.UsedThisRound, loserSide.UsedThisRound);
                totalDamage = win.TotalDamage;
            }

            // 반격 보너스는 "이번 라운드에 피해를 입었는가"로 정해지므로 피해를 입은 쪽만 0으로 돌아간다.
            Player.CounterBonus = _settlement.ResolveCounterBonus(Player.UsedThisRound, winner == BattleWinner.Enemy ? totalDamage : 0);
            Enemy.CounterBonus = _settlement.ResolveCounterBonus(Enemy.UsedThisRound, winner == BattleWinner.Player ? totalDamage : 0);

            _settlement.ApplyPassiveDots(Player.State, Enemy.State, Player.GetActivePassives());
            _settlement.ApplyPassiveDots(Enemy.State, Player.State, Enemy.GetActivePassives());

            _defense.TryUndying(Player.State, Player.GetActivePassives());
            _defense.TryUndying(Enemy.State, Enemy.GetActivePassives());

            return new SettleResult(playerScore, enemyScore, playerCrit, enemyCrit, new BattleOutcome(winner, totalDamage));
        }

        // 라운드가 끝났을 때 한 번 부른다. 액티브의 남은 라운드를 줄이고 0이 되면 삭제한다.
        public void EndRound()
        {
            Player.Tracker.EndRound();
            Enemy.Tracker.EndRound();
        }

        private EffectSideContext CreateSide(CombatantState state)
        {
            var deck = _deckRules.BuildRandomDeck(_database.Cards);
            var inventory = new EffectInventory(deck, _deckRules);
            inventory.DealStarters(_ruleSet.StarterCount);
            return new EffectSideContext(state, inventory, new EffectActiveTracker(inventory, _ruleSet),
                new EffectDraft(inventory, _ruleSet));
        }

        // 폭풍 감소, 실드 소멸은 두 번째 라운드부터다. 첫 라운드는 아직 아무것도 걸려 있지 않다.
        private void StartRoundFor(EffectSideContext side)
        {
            side.Tracker.StartRound();
            side.UsedThisRound.Clear();
            side.UtilityUses = 0;
            side.State.SetImmune(false);
            side.State.SetPendingConfuse(false);

            if (Round > 1)
            {
                if (side.State.StormRounds > 0)
                {
                    side.State.TickStorm();
                }

                if (side.State.Shield > 0)
                {
                    side.State.ClearShield();
                }
            }

            var passives = side.GetActivePassives();
            foreach (var passive in passives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind == EffectKind.Smelt)
                    {
                        side.SmeltStack += (int)effect.Value;
                    }
                }
            }

            if (Round > 1)
            {
                _utility.ApplyHaste(passives, side.Tracker);
            }

            side.State.TickDots();
            side.ExtraRerolls = _drawModifiers.GetExtraRerolls(passives);
        }

        private EffectScoreCalculator.Result Calculate(EffectSideContext side, EffectSideContext opponent, PokerHandResult hand)
        {
            return _calculator.Calculate(new EffectScoreCalculator.Input(hand.Score, hand.HandRank, side.UsedThisRound,
                side.GetActivePassives(), side.State.Hp, side.State.MaxHp, side.SmeltStack, side.CounterBonus,
                opponent.State.Shield, opponent.UtilityUses));
        }

        private static bool RollCrit(float chancePercent)
        {
            return chancePercent > 0f && Random.Range(0f, 100f) < chancePercent;
        }
    }
}
