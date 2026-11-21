using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 정산 전에 "내 족보 점수 + 효과 카드 보정"을 한 번에 계산하는 처리기. 상태는 들고 있지 않고 입력만 본다.
    public class EffectScoreCalculator
    {
        // 원 페어 이상일 때만 조커가 발동한다는 카드 설명을 코드로 옮긴 기준이다.
        private const HandRank JokerMinimumHand = HandRank.OnePair;

        public readonly struct Input
        {
            public int BaseScore { get; }
            public HandRank HandRank { get; }
            public IReadOnlyList<EffectCardData> UsedActives { get; }
            public IReadOnlyList<EffectCardData> ActivePassives { get; }
            public int Hp { get; }
            public int MaxHp { get; }

            // 제련 패시브가 라운드마다 쌓아 온 보너스. 누적은 호출한 쪽이 관리한다.
            public int SmeltStack { get; }
            public int CounterBonus { get; }
            public int OpponentShield { get; }
            public int OpponentUtilityUses { get; }

            public Input(int baseScore, HandRank handRank, IReadOnlyList<EffectCardData> usedActives,
                IReadOnlyList<EffectCardData> activePassives, int hp, int maxHp, int smeltStack,
                int counterBonus, int opponentShield, int opponentUtilityUses)
            {
                BaseScore = baseScore;
                HandRank = handRank;
                UsedActives = usedActives;
                ActivePassives = activePassives;
                Hp = hp;
                MaxHp = maxHp;
                SmeltStack = smeltStack;
                CounterBonus = counterBonus;
                OpponentShield = opponentShield;
                OpponentUtilityUses = opponentUtilityUses;
            }
        }

        public readonly struct Result
        {
            public int BaseScore { get; }
            public int Bonus { get; }
            public float Multiplier { get; }
            public float CritChance { get; }
            public int FinalScore { get; }

            public Result(int baseScore, int bonus, float multiplier, float critChance, int finalScore)
            {
                BaseScore = baseScore;
                Bonus = bonus;
                Multiplier = multiplier;
                CritChance = critChance;
                FinalScore = finalScore;
            }
        }

        private readonly EffectRuleSet _ruleSet;

        public EffectScoreCalculator(EffectRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
        }

        public Result Calculate(Input input)
        {
            int bonus = input.CounterBonus;
            float multiplier = 1f;
            float critChance = 0f;

            foreach (var card in input.UsedActives)
            {
                foreach (var effect in card.Effects)
                {
                    switch (effect.Kind)
                    {
                        case EffectKind.BonusPercent:
                            // 낮은 족보에서 비율 보너스가 0에 가까워지지 않도록 하한(Limit)을 둔다.
                            bonus += Mathf.Max((int)effect.Limit, RoundHalfUp(input.BaseScore * effect.Value / 100f));
                            break;
                        case EffectKind.Multiplier:
                            multiplier *= effect.Value;
                            break;
                        case EffectKind.CritChance:
                            critChance += effect.Value;
                            break;
                    }
                }
            }

            foreach (var card in input.ActivePassives)
            {
                foreach (var effect in card.Effects)
                {
                    bonus += GetPassiveBonus(effect, input);
                }
            }

            int finalScore = RoundHalfUp((input.BaseScore + bonus) * multiplier);
            return new Result(input.BaseScore, bonus, multiplier, Mathf.Min(critChance, _ruleSet.CritChanceCap), finalScore);
        }

        public int ApplyCrit(int score)
        {
            return RoundHalfUp(score * _ruleSet.CritMultiplier);
        }

        private static int GetPassiveBonus(EffectEntry effect, Input input)
        {
            switch (effect.Kind)
            {
                case EffectKind.Smelt:
                    return input.SmeltStack;

                case EffectKind.Revenge:
                    // Limit이 "몇 % 줄 때마다"의 단위다. 0이면 계산할 수 없어 보너스를 주지 않는다.
                    if (effect.Limit <= 0f || input.MaxHp <= 0)
                    {
                        return 0;
                    }

                    float lostPercent = (input.MaxHp - input.Hp) * 100f / input.MaxHp;
                    return Mathf.FloorToInt(lostPercent / effect.Limit) * (int)effect.Value;

                case EffectKind.Link:
                    return CountAttackActives(input.UsedActives) >= (int)effect.Limit ? (int)effect.Value : 0;

                case EffectKind.ShieldBreakerBonus:
                    return input.OpponentShield > 0 ? (int)effect.Value : 0;

                case EffectKind.Awaken:
                    return input.OpponentUtilityUses * (int)effect.Value;

                case EffectKind.JokerPercent:
                    return input.HandRank >= JokerMinimumHand ? RoundHalfUp(input.BaseScore * effect.Value / 100f) : 0;

                default:
                    return 0;
            }
        }

        private static int CountAttackActives(IReadOnlyList<EffectCardData> used)
        {
            int count = 0;
            for (int i = 0; i < used.Count; i++)
            {
                if (used[i].Category.HasFlag(EffectCategory.Attack))
                {
                    count++;
                }
            }

            return count;
        }

        // 프로토타입(JS Math.round)과 점수가 같게 나오도록 0.5는 항상 올린다.
        private static int RoundHalfUp(float value)
        {
            return Mathf.FloorToInt(value + 0.5f);
        }
    }
}
