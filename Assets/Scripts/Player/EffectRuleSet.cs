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
        [SerializeField] private float _critChanceCap = 40f;
        [SerializeField] private float _critMultiplier = 1.5f;
        [SerializeField] private float _damageReductionCap = 80f;
        [SerializeField] private float _drawWeightFactor = 0.35f;

        public int DeckMax => _deckMax;
        public int EpicMax => _epicMax;
        public int LegendMax => _legendMax;
        public int BaseSlots => _baseSlots;
        public int StarterCount => _starterCount;
        public int DraftOfferCount => _draftOfferCount;

        // 액티브는 첫 사용 라운드부터 이 횟수만큼만 쓸 수 있고 그 뒤 삭제된다.
        public int ActiveUsableRounds => _activeUsableRounds;

        // 일격을 여러 장 겹쳐도 치명타가 사실상 확정되지 않도록 확률에 상한을 둔다.
        public float CritChanceCap => _critChanceCap;
        public float CritMultiplier => _critMultiplier;

        // 감면이 100%에 가까워져 피해가 사라지는 것을 막는다.
        public float DamageReductionCap => _damageReductionCap;

        // 가중치 카드가 몇 장 겹쳐도 한 문양만 나오지 않도록 카드 값에 곱하는 완충 계수다.
        public float DrawWeightFactor => _drawWeightFactor;
    }
}
