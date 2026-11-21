using System.Collections.Generic;

namespace Effect_Card
{
    // 한쪽(나 또는 상대)이 전투 동안 들고 다니는 효과 카드 상태를 한곳에 묶는다.
    public class EffectSideContext
    {
        public CombatantState State { get; }
        public EffectInventory Inventory { get; }
        public EffectActiveTracker Tracker { get; }
        public EffectDraft Draft { get; }

        // 이번 라운드에 실제로 효과가 적용된 액티브. 점수 계산과 감면·반격 판정이 이 목록을 본다.
        public List<EffectCardData> UsedThisRound { get; } = new List<EffectCardData>();

        // 상대가 한 번이라도 쓴 액티브. 상대 화면에서 이 카드만 정체를 보여 준다.
        public HashSet<EffectCardData> RevealedActives { get; } = new HashSet<EffectCardData>();

        public int SmeltStack { get; set; }
        public int CounterBonus { get; set; }
        public int UtilityUses { get; set; }
        public int ExtraRerolls { get; set; }

        public EffectSideContext(CombatantState state, EffectInventory inventory, EffectActiveTracker tracker, EffectDraft draft)
        {
            State = state;
            Inventory = inventory;
            Tracker = tracker;
            Draft = draft;
        }

        // 폭풍에 맞으면 장착한 패시브가 모두 꺼지므로 빈 목록을 돌려준다.
        public IReadOnlyList<EffectCardData> GetActivePassives()
        {
            var passives = new List<EffectCardData>();
            if (State.ArePassivesSealed)
            {
                return passives;
            }

            foreach (var card in Inventory.Equipped)
            {
                if (!card.IsActive)
                {
                    passives.Add(card);
                }
            }

            return passives;
        }
    }
}
