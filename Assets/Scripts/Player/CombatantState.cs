using System;
using System.Collections.Generic;
using UnityEngine;

namespace Effect_Card
{
    // 효과 카드 처리기들이 공통으로 읽고 쓰는 한쪽(나 또는 상대)의 전투 상태.
    public class CombatantState
    {
        public class DotEffect
        {
            public int Damage { get; }
            public int Turns { get; set; }

            // 어느 카드가 건 지속 피해인지 남겨 두면 나중에 피해 기여도를 카드별로 집계할 수 있다.
            public EffectCardData Source { get; }

            public DotEffect(int damage, int turns, EffectCardData source)
            {
                Damage = damage;
                Turns = turns;
                Source = source;
            }
        }

        private readonly List<DotEffect> _dots = new List<DotEffect>();

        public int Hp { get; private set; }
        public int MaxHp { get; }
        public int Shield { get; private set; }
        public bool IsImmune { get; private set; }
        public bool UndyingUsed { get; private set; }
        public IReadOnlyList<DotEffect> Dots => _dots;

        // 폭풍에 맞은 동안 패시브가 모두 꺼진다. 남은 라운드 수로 들고 있는다.
        public int StormRounds { get; private set; }
        public bool ArePassivesSealed => StormRounds > 0;

        // 혼란: 정산 때 상대의 포커 카드 1장을 강제로 바꾸라는 요청이다. 실제 교체는 포커 쪽이 한다.
        public bool HasPendingConfuse { get; private set; }

        public event Action Changed;

        public CombatantState(int maxHp)
        {
            MaxHp = maxHp;
            Hp = maxHp;
        }

        public int Heal(int amount)
        {
            int healed = Mathf.Clamp(amount, 0, MaxHp - Hp);
            Hp += healed;
            Changed?.Invoke();
            return healed;
        }

        public void AddShield(int amount)
        {
            Shield += amount;
            Changed?.Invoke();
        }

        public int AbsorbWithShield(int damage)
        {
            int absorbed = Mathf.Min(Shield, damage);
            Shield -= absorbed;
            Changed?.Invoke();
            return absorbed;
        }

        public void ClearShield()
        {
            Shield = 0;
            Changed?.Invoke();
        }

        // HP는 0 아래로도 내려가게 두어, 불사 판정이 "죽을 피해였는지"를 구분할 수 있게 한다.
        public void LoseHp(int damage)
        {
            Hp -= damage;
            Changed?.Invoke();
        }

        public void SetImmune(bool value)
        {
            IsImmune = value;
            Changed?.Invoke();
        }

        public void AddDot(int damage, int turns, EffectCardData source)
        {
            _dots.Add(new DotEffect(damage, turns, source));
            Changed?.Invoke();
        }

        public void ClearDots()
        {
            _dots.Clear();
            Changed?.Invoke();
        }

        // 라운드가 시작될 때 한 번 호출한다. 이번 라운드에 들어간 지속 피해의 합을 돌려준다.
        public int TickDots()
        {
            int total = 0;
            foreach (var dot in _dots)
            {
                total += dot.Damage;
                dot.Turns--;
            }

            _dots.RemoveAll(dot => dot.Turns <= 0);
            LoseHp(total);
            return total;
        }

        // 이미 더 긴 폭풍을 맞고 있으면 짧은 폭풍이 덮어써서 줄어들지 않게 한다.
        public void SetStorm(int rounds)
        {
            StormRounds = Mathf.Max(StormRounds, rounds);
            Changed?.Invoke();
        }

        public void TickStorm()
        {
            StormRounds = Mathf.Max(0, StormRounds - 1);
            Changed?.Invoke();
        }

        public void SetPendingConfuse(bool value)
        {
            HasPendingConfuse = value;
            Changed?.Invoke();
        }

        public void SurviveWithOneHp()
        {
            Hp = 1;
            UndyingUsed = true;
            Changed?.Invoke();
        }
    }
}
