using System;

namespace Core
{
    // "공격/방해", "유틸/방어"처럼 한 카드가 분류를 둘 가질 수 있어 Flags로 둔다.
    [Flags]
    public enum EffectCategory
    {
        None = 0,
        Attack = 1 << 0,
        Defense = 1 << 1,
        Utility = 1 << 2,
        Disrupt = 1 << 3,
        Enhance = 1 << 4,
        Trap = 1 << 5,
        Wild = 1 << 6,
        Special = 1 << 7
    }
}
