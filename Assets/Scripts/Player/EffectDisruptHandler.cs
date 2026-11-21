using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 방해·유틸 액티브를 쓸 때의 상태 변화와 상대 함정 패시브의 반응을 처리한다.
    public class EffectDisruptHandler
    {
        public readonly struct TrapResult
        {
            public bool Nullified { get; }
            public bool Reflected { get; }
            public bool Punished { get; }

            public TrapResult(bool nullified, bool reflected, bool punished)
            {
                Nullified = nullified;
                Reflected = reflected;
                Punished = punished;
            }
        }

        // opponentPassives에는 폭풍으로 봉인된 상태라면 빈 목록을 넘겨야 함정이 반응하지 않는다.
        public TrapResult ResolveActiveUse(EffectCardData card, CombatantState user, CombatantState opponent,
            IReadOnlyList<EffectCardData> opponentPassives)
        {
            bool isDisrupt = card.Category.HasFlag(EffectCategory.Disrupt);
            bool isUtility = card.Category.HasFlag(EffectCategory.Utility);

            if (isDisrupt && TriggerTrap(opponentPassives, EffectKind.TrapNullifyDisrupt, out _))
            {
                return new TrapResult(true, false, false);
            }

            bool reflected = isDisrupt && TriggerTrap(opponentPassives, EffectKind.TrapReflectDisrupt, out _);

            bool punished = false;
            if (isUtility && TriggerTrap(opponentPassives, EffectKind.TrapPunishUtility, out var punish))
            {
                user.AddDot((int)punish.Value, punish.Turns, null);
                punished = true;
            }

            foreach (var effect in card.Effects)
            {
                ApplyEffect(effect, user, opponent, reflected);
            }

            return new TrapResult(false, reflected, punished);
        }

        private static void ApplyEffect(EffectEntry effect, CombatantState user, CombatantState opponent, bool reflected)
        {
            switch (effect.Kind)
            {
                case EffectKind.Storm:
                    opponent.SetStorm(effect.Turns);
                    if (reflected)
                    {
                        user.SetStorm(effect.Turns);
                    }

                    break;
                case EffectKind.Confuse:
                    user.SetPendingConfuse(true);
                    if (reflected)
                    {
                        opponent.SetPendingConfuse(true);
                    }

                    break;
                case EffectKind.Cleanse:
                    user.ClearDots();
                    break;
            }
        }

        private static bool TriggerTrap(IReadOnlyList<EffectCardData> passives, EffectKind kind, out EffectEntry trap)
        {
            foreach (var card in passives)
            {
                foreach (var effect in card.Effects)
                {
                    if (effect.Kind == kind && Roll(effect.Chance))
                    {
                        trap = effect;
                        return true;
                    }
                }
            }

            trap = null;
            return false;
        }

        private static bool Roll(float chancePercent)
        {
            if (chancePercent >= 100f)
            {
                return true;
            }

            return chancePercent > 0f && Random.Range(0f, 100f) < chancePercent;
        }
    }
}
