using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 액티브의 남은 라운드를 건드리거나 다시 쓰게 해 주는 유틸·특수 효과를 처리한다.
    public class EffectUtilityHandler
    {
        // 재정비: 상대가 이미 쓰기 시작한 액티브 중 하나의 남은 라운드를 줄인다. 대상이 없으면 null.
        public EffectCardData ApplyRefit(EffectCardData card, EffectActiveTracker opponentTracker)
        {
            foreach (var effect in card.Effects)
            {
                if (effect.Kind != EffectKind.Refit)
                {
                    continue;
                }

                var target = PickRandomStarted(opponentTracker);
                if (target != null)
                {
                    opponentTracker.AdjustRemainingRounds(target, -(int)effect.Value);
                }

                return target;
            }

            return null;
        }

        // 신속: 라운드가 시작될 때 내가 쓰고 있는 액티브 하나의 남은 라운드를 늘린다.
        public EffectCardData ApplyHaste(IReadOnlyList<EffectCardData> passives, EffectActiveTracker ownTracker)
        {
            EffectCardData lastTarget = null;
            foreach (var passive in passives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind != EffectKind.Haste)
                    {
                        continue;
                    }

                    var target = PickRandomStarted(ownTracker);
                    if (target != null)
                    {
                        ownTracker.AdjustRemainingRounds(target, (int)effect.Value);
                        lastTarget = target;
                    }
                }
            }

            return lastTarget;
        }

        // 순환: 액티브를 쓴 직후 호출한다. 발동하면 같은 라운드에 그 액티브를 한 번 더 쓸 수 있다.
        public bool TryRecycle(EffectCardData usedCard, IReadOnlyList<EffectCardData> passives, EffectActiveTracker ownTracker)
        {
            foreach (var passive in passives)
            {
                foreach (var effect in passive.Effects)
                {
                    if (effect.Kind == EffectKind.Recycle && Roll(effect.Chance))
                    {
                        ownTracker.ResetUse(usedCard);
                        return true;
                    }
                }
            }

            return false;
        }

        // 마법 거울: 복제할 카드를 돌려준다. 아직 쓴 액티브가 없으면 null이라 거울을 쓰지 않은 것으로 처리해야 한다.
        public EffectCardData GetMirrorTarget(EffectActiveTracker ownTracker)
        {
            return ownTracker.LastUsed;
        }

        private static EffectCardData PickRandomStarted(EffectActiveTracker tracker)
        {
            var candidates = new List<EffectCardData>(tracker.StartedCards);
            return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
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
