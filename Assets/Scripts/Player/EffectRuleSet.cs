using UnityEngine;

namespace Effect_Card
{
    // 밸런스를 코드 수정 없이 조정하려고 효과 카드 관련 수치를 한곳에 모은다.
    [CreateAssetMenu(fileName = "EffectRuleSet", menuName = "CardGame/Effect Rule Set")]
    public class EffectRuleSet : ScriptableObject
    {
        [SerializeField] private int _deckMax = 26;
        [SerializeField] private int _epicMax = 6;
        [SerializeField] private int _legendMax = 2;
        [SerializeField] private int _baseSlots = 5;
        [SerializeField] private int _starterCount = 3;
        [SerializeField] private int _draftOfferCount = 3;
        [SerializeField] private int _activeUsableRounds = 3;

        public int DeckMax => _deckMax;
        public int EpicMax => _epicMax;
        public int LegendMax => _legendMax;
        public int BaseSlots => _baseSlots;
        public int StarterCount => _starterCount;
        public int DraftOfferCount => _draftOfferCount;

        // 액티브는 첫 사용 라운드부터 이 횟수만큼만 쓸 수 있고 그 뒤 삭제된다.
        public int ActiveUsableRounds => _activeUsableRounds;
    }
}
