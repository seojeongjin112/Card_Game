using System;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 효과마다 수치가 제각각(확률, 지속 턴, 상한)이라 매직 넘버를 코드에 두지 않으려고 모두 데이터로 뺐다.
    [Serializable]
    public class EffectEntry
    {
        [SerializeField] private EffectKind _kind;
        [SerializeField] private float _value;
        [SerializeField, Range(0f, 100f)] private float _chance = 100f;
        [SerializeField] private int _turns;
        [SerializeField] private float _limit;

        public EffectKind Kind => _kind;
        public float Value => _value;
        public float Chance => _chance;
        public int Turns => _turns;

        // 0이면 상한·하한이 없다는 뜻이다. 강타의 최소 보너스, 강건의 최대 실드에 쓴다.
        public float Limit => _limit;

        public EffectEntry(EffectKind kind, float value, float chance = 100f, int turns = 0, float limit = 0f)
        {
            _kind = kind;
            _value = value;
            _chance = chance;
            _turns = turns;
            _limit = limit;
        }
    }
}
