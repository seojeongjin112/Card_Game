using System;
using System.Collections.Generic;
using Core;
using Poker_Card;
using UnityEngine;
using Random = UnityEngine.Random;

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

            // 점수가 어떻게 만들어졌는지 보여 주려고 치명타 적용 전 계산 내역과 그 라운드에 쓴 카드를 함께 담는다.
            public EffectScoreCalculator.Result PlayerDetail { get; }
            public EffectScoreCalculator.Result EnemyDetail { get; }
            public IReadOnlyList<EffectCardData> PlayerUsedCards { get; }
            public IReadOnlyList<EffectCardData> EnemyUsedCards { get; }

            public SettleResult(int playerScore, int enemyScore, bool playerCrit, bool enemyCrit, BattleOutcome outcome,
                EffectScoreCalculator.Result playerDetail, EffectScoreCalculator.Result enemyDetail,
                IReadOnlyList<EffectCardData> playerUsedCards, IReadOnlyList<EffectCardData> enemyUsedCards)
            {
                PlayerScore = playerScore;
                EnemyScore = enemyScore;
                PlayerCrit = playerCrit;
                EnemyCrit = enemyCrit;
                Outcome = outcome;
                PlayerDetail = playerDetail;
                EnemyDetail = enemyDetail;
                PlayerUsedCards = playerUsedCards;
                EnemyUsedCards = enemyUsedCards;
            }
        }

        // 피해가 적용되기 전에 점수와 승패만 먼저 알린다. 연출이 "배너 → 충돌 → 피해" 순서로 이어 가는 데 쓴다.
        public readonly struct SettlePreview
        {
            public int PlayerScore { get; }
            public int EnemyScore { get; }
            public bool PlayerCrit { get; }
            public bool EnemyCrit { get; }
            public PokerHandResult PlayerHand { get; }
            public PokerHandResult EnemyHand { get; }
            public BattleWinner Winner { get; }
            public IReadOnlyList<EffectCardData> PlayerPassives { get; }
            public IReadOnlyList<EffectCardData> EnemyPassives { get; }

            // 치명타 배수를 곱하기 전의 점수. 점수판이 먼저 이 값까지 올라가고, 치명타가 터질 때 최종 점수로 바뀌는 데 쓴다.
            public int PlayerBaseScore { get; }
            public int EnemyBaseScore { get; }

            public SettlePreview(int playerScore, int enemyScore, bool playerCrit, bool enemyCrit,
                PokerHandResult playerHand, PokerHandResult enemyHand, BattleWinner winner,
                IReadOnlyList<EffectCardData> playerPassives, IReadOnlyList<EffectCardData> enemyPassives,
                int playerBaseScore, int enemyBaseScore)
            {
                PlayerBaseScore = playerBaseScore;
                EnemyBaseScore = enemyBaseScore;
                PlayerPassives = playerPassives;
                EnemyPassives = enemyPassives;
                PlayerScore = playerScore;
                EnemyScore = enemyScore;
                PlayerCrit = playerCrit;
                EnemyCrit = enemyCrit;
                PlayerHand = playerHand;
                EnemyHand = enemyHand;
                Winner = winner;
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

        // 정산이 끝난 뒤에는 다음 라운드가 시작될 때까지 액티브를 쓸 수 없다.
        private bool _roundSettled;

        public EffectSideContext Player { get; private set; }
        public EffectSideContext Enemy { get; private set; }
        public int Round { get; private set; }
        public bool IsRoundOpen => !_roundSettled;

        // 화면이 카드 설명에 "몇 라운드 사용 가능"을 쓰려고 규칙 수치를 읽을 통로다.
        public int ActiveUsableRounds => _ruleSet.ActiveUsableRounds;

        // 화면이 폴링하지 않고 갱신되도록, 장착·사용·정산 등 보이는 상태가 바뀔 때마다 알린다.
        public event Action Changed;

        // 정산 직후 한 번. 사용한 카드 목록은 다음 라운드에 비워지므로 이 시점의 값을 복사해 넘긴다.
        public event Action<SettleResult> Settled;

        // 점수·승패가 정해진 직후, 피해가 적용되기 전에 한 번 알린다.
        public event Action<SettlePreview> ScoresResolved;

        // 전투 로그용. 상태가 바뀐 그 순간의 사건을 한 줄 재료로 알린다.
        public event Action<EffectLogEntry> Logged;

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
            _roundSettled = false;
            Player = CreateSide(_state.Player);
            Enemy = CreateSide(_state.Enemy);
            Log(EffectLogKind.BattleStarted, true);
            Changed?.Invoke();
        }

        // 플레이어의 드래프트가 열렸으면 true. 선택은 UI가 Player.Draft로 이어 간다.
        public bool StartRound()
        {
            Round++;
            _roundSettled = false;
            StartRoundFor(Player);
            StartRoundFor(Enemy);

            CheckUndying(Player);
            CheckUndying(Enemy);

            _enemyDrafter.Draft(Enemy.Draft);
            bool draftOpen = Player.Draft.Open();
            Changed?.Invoke();
            return draftOpen;
        }

        public ActiveUseResult TryUseActive(bool isPlayer, EffectCardData card)
        {
            var self = isPlayer ? Player : Enemy;
            var opponent = isPlayer ? Enemy : Player;

            if (_roundSettled || !self.Tracker.CanUse(card))
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
                Log(EffectLogKind.ActiveNullified, isPlayer, effectiveCard);
                Changed?.Invoke();
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
            Log(EffectLogKind.ActiveUsed, isPlayer, effectiveCard);
            Changed?.Invoke();
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

            ScoresResolved?.Invoke(new SettlePreview(playerScore, enemyScore, playerCrit, enemyCrit, playerHand, enemyHand, winner,
                playerCalc.ContributingPassives, enemyCalc.ContributingPassives, playerCalc.FinalScore, enemyCalc.FinalScore));

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

            CheckUndying(Player);
            CheckUndying(Enemy);

            // 치명타 줄이 결과 줄보다 먼저 쌓여야 최신순으로 볼 때 결과가 위에 온다.
            if (playerCrit)
            {
                Log(EffectLogKind.CriticalHit, true);
            }

            if (enemyCrit)
            {
                Log(EffectLogKind.CriticalHit, false);
            }

            Log(EffectLogKind.Settled, true, null, winner, playerScore, enemyScore, totalDamage);

            _roundSettled = true;
            Changed?.Invoke();

            var result = new SettleResult(playerScore, enemyScore, playerCrit, enemyCrit,
                new BattleOutcome(winner, totalDamage), playerCalc, enemyCalc,
                Player.UsedThisRound.ToArray(), Enemy.UsedThisRound.ToArray());
            Settled?.Invoke(result);
            return result;
        }

        // 라운드가 끝났을 때 한 번 부른다. 액티브의 남은 라운드를 줄이고 0이 되면 삭제한다.
        public void EndRound()
        {
            Player.Tracker.EndRound();
            Enemy.Tracker.EndRound();
            Changed?.Invoke();
        }

        private EffectSideContext CreateSide(CombatantState state)
        {
            var deck = _deckRules.BuildRandomDeck(_database.Cards);
            var inventory = new EffectInventory(deck, _deckRules);
            inventory.DealStarters(_ruleSet.StarterCount);

            // 인벤토리는 이 조정자와 수명이 같아서 구독을 따로 해제하지 않는다. 드래프트로 장착이 바뀔 때도 화면이 갱신된다.
            inventory.Changed += RaiseChanged;

            var side = new EffectSideContext(state, inventory, new EffectActiveTracker(inventory, _ruleSet),
                new EffectDraft(inventory, _ruleSet));

            // 드래프트가 닫힌 것도 화면이 알아야 한다. 드래프트도 조정자와 수명이 같아 구독을 해제하지 않는다.
            side.Draft.Closed += RaiseChanged;

            // 한 번 쓴 액티브는 정체가 드러난다. 트래커도 조정자와 수명이 같아서 구독을 해제하지 않는다.
            side.Tracker.Used += card => side.RevealedActives.Add(card);

            // 이쪽이 플레이어인지는 연결된 전투 상태로 가린다. Player/Enemy 프로퍼티는 아직 대입되기 전이다.
            bool isPlayer = state == _state.Player;
            side.Draft.Picked += card => Log(EffectLogKind.Drafted, isPlayer, card, BattleWinner.Draw, inventory.Equipped.Count);
            side.Tracker.Expired += card => Log(EffectLogKind.ActiveExpired, isPlayer, card);
            return side;
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

            int dotDamage = side.State.TickDots();
            if (dotDamage > 0)
            {
                Log(EffectLogKind.DotDamage, side == Player, null, BattleWinner.Draw, dotDamage);
            }

            side.ExtraRerolls = _drawModifiers.GetExtraRerolls(passives);
        }

        private void CheckUndying(EffectSideContext side)
        {
            if (_defense.TryUndying(side.State, side.GetActivePassives()))
            {
                Log(EffectLogKind.UndyingTriggered, side == Player);
            }
        }

        private void Log(EffectLogKind kind, bool isPlayer, EffectCardData card = null,
            BattleWinner winner = BattleWinner.Draw, int value1 = 0, int value2 = 0, int value3 = 0)
        {
            Logged?.Invoke(new EffectLogEntry(kind, Round, isPlayer, card, winner, value1, value2, value3));
        }

        private EffectScoreCalculator.Result Calculate(EffectSideContext side, EffectSideContext opponent, PokerHandResult hand)
        {
            return _calculator.Calculate(new EffectScoreCalculator.Input(hand.Score, hand.HandRank, side.UsedThisRound,
                side.GetActivePassives(), side.State.Hp, side.State.MaxHp, side.SmeltStack, side.CounterBonus,
                opponent.State.Shield, opponent.UtilityUses));
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }

        private static bool RollCrit(float chancePercent)
        {
            return chancePercent > 0f && Random.Range(0f, 100f) < chancePercent;
        }
    }
}
