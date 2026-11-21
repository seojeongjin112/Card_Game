using Core;
using Effect_Card;

namespace Poker_Card
{
    public class EnemyEffectDrafter
    {
        private readonly bool _preferHighGrade;

        public EnemyEffectDrafter(bool preferHighGrade)
        {
            _preferHighGrade = preferHighGrade;
        }

        // 난이도가 높을수록 높은 등급을 우선 고르고, 낮으면 무작위로 골라 규칙을 익히는 상대처럼 보이게 한다.
        public void Draft(EffectDraft draft)
        {
            if (!draft.Open())
            {
                return;
            }

            var chosen = ChooseCard(draft);
            if (!draft.NeedsReplace)
            {
                draft.TryPick(chosen);
                return;
            }

            int replaceIndex = FindReplaceIndex(draft, chosen);
            if (replaceIndex < 0)
            {
                draft.Skip();
                return;
            }

            draft.TryPick(chosen, replaceIndex);
        }

        private EffectCardData ChooseCard(EffectDraft draft)
        {
            var offer = draft.Offer;
            var best = offer[UnityEngine.Random.Range(0, offer.Count)];
            if (!_preferHighGrade)
            {
                return best;
            }

            foreach (var card in offer)
            {
                if (card.Grade > best.Grade)
                {
                    best = card;
                }
            }

            return best;
        }

        // 새 카드보다 등급이 낮은 카드 중 가장 낮은 것만 버린다. 슬롯을 늘려 주는 카드를 버려 넘치는 교체는 제외한다.
        private static int FindReplaceIndex(EffectDraft draft, EffectCardData incoming)
        {
            var equipped = draft.Inventory.Equipped;
            int found = -1;
            for (int i = 0; i < equipped.Count; i++)
            {
                if (equipped[i].Grade >= incoming.Grade || !draft.Inventory.CanReplace(i, incoming))
                {
                    continue;
                }

                if (found < 0 || equipped[i].Grade < equipped[found].Grade)
                {
                    found = i;
                }
            }

            return found;
        }
    }
}
