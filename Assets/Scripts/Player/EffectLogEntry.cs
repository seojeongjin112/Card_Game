using Core;

namespace Effect_Card
{
    // 로그 한 줄의 재료. 문장은 화면이 만들고, 여기에는 누가·무엇을·얼마나만 담아 문구를 바꿀 때 로직을 건드리지 않게 한다.
    public readonly struct EffectLogEntry
    {
        public EffectLogKind Kind { get; }
        public int Round { get; }
        public bool IsPlayer { get; }
        public EffectCardData Card { get; }
        public BattleWinner Winner { get; }

        // 종류마다 뜻이 다르다. Drafted: 장착 수 / DotDamage: 피해 / Settled: 내 점수, 상대 점수, 입힌 피해.
        public int Value1 { get; }
        public int Value2 { get; }
        public int Value3 { get; }

        public EffectLogEntry(EffectLogKind kind, int round, bool isPlayer, EffectCardData card,
            BattleWinner winner, int value1, int value2, int value3)
        {
            Kind = kind;
            Round = round;
            IsPlayer = isPlayer;
            Card = card;
            Winner = winner;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
        }
    }
}
