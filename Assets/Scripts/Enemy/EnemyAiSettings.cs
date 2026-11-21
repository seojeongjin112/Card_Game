using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Poker_Card
{
    // 상대 AI의 판단 기준을 코드 밖으로 빼서, 난이도와 점수 규모가 바뀌어도 에셋만 고치면 되게 한다.
    [CreateAssetMenu(fileName = "EnemyAiSettings", menuName = "CardGame/Enemy AI Settings")]
    public class EnemyAiSettings : ScriptableObject
    {
        [Serializable]
        public class Profile
        {
            [SerializeField] private EnemyDifficulty _difficulty;

            // 이 확률로만 상황을 보고 판단하고, 나머지는 UseRate로 무작위로 쓴다. 쉬움은 판단을 거의 하지 않는다.
            [SerializeField, Range(0f, 1f)] private float _smartRate;
            [SerializeField, Range(0f, 1f)] private float _useRate;
            [SerializeField] private bool _prefersHighGradeDraft;

            public EnemyDifficulty Difficulty => _difficulty;
            public float SmartRate => _smartRate;
            public float UseRate => _useRate;
            public bool PrefersHighGradeDraft => _prefersHighGradeDraft;

            public Profile(EnemyDifficulty difficulty, float smartRate, float useRate, bool prefersHighGradeDraft)
            {
                _difficulty = difficulty;
                _smartRate = smartRate;
                _useRate = useRate;
                _prefersHighGradeDraft = prefersHighGradeDraft;
            }
        }

        // 프로토타입은 점수 규모가 달라서 그대로 쓰지 않고, 현재 점수표(1~12점)에 맞춰 잡았다.
        [SerializeField] private int _weakScore = 2;
        [SerializeField] private int _strongScore = 6;
        [SerializeField, Range(0f, 1f)] private float _lowHpRatio = 0.4f;
        [SerializeField] private int _playerKillZoneHp = 25;
        [SerializeField] private int _playerFinishZoneHp = 15;
        [SerializeField, Range(0f, 1f)] private float _fallbackUseRate = 0.5f;

        [SerializeField]
        private List<Profile> _profiles = new List<Profile>
        {
            new Profile(EnemyDifficulty.Easy, 0f, 0.35f, false),
            new Profile(EnemyDifficulty.Normal, 0.8f, 0.6f, false),
            new Profile(EnemyDifficulty.Hard, 1f, 0.85f, true)
        };

        public int WeakScore => _weakScore;
        public int StrongScore => _strongScore;
        public float LowHpRatio => _lowHpRatio;
        public int PlayerKillZoneHp => _playerKillZoneHp;
        public int PlayerFinishZoneHp => _playerFinishZoneHp;

        // 효과가 따로 정해지지 않은 카드는 이 확률로 쓴다.
        public float FallbackUseRate => _fallbackUseRate;

        public Profile GetProfile(EnemyDifficulty difficulty)
        {
            foreach (var profile in _profiles)
            {
                if (profile.Difficulty == difficulty)
                {
                    return profile;
                }
            }

            return _profiles[0];
        }
    }
}
