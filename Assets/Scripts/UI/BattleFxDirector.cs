using System;
using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 전투에서 일어나는 사건(라운드 시작, 정산, 피해, 회복, 실드)을 받아 연출 도구를 순서대로 호출한다. 연출은 로직에 영향을 주지 않는다.
    public class BattleFxDirector : MonoBehaviour
    {
        [Serializable]
        private class HandFx
        {
            [SerializeField] private HandRank _handRank;
            [SerializeField] private string _englishName;
            [SerializeField] private Color _color = Color.white;

            // 0이면 배너를 띄우지 않는다. 높을수록 색종이가 많아지고 4는 무지개 띠다.
            [SerializeField] private int _tier;

            public HandRank HandRank => _handRank;
            public string EnglishName => _englishName;
            public Color Color => _color;
            public int Tier => _tier;

            public HandFx(HandRank handRank, string englishName, Color color, int tier)
            {
                _handRank = handRank;
                _englishName = englishName;
                _color = color;
                _tier = tier;
            }
        }

        private readonly struct TimedAction
        {
            public float Time { get; }
            public Action Action { get; }

            public TimedAction(float time, Action action)
            {
                Time = time;
                Action = action;
            }
        }

        private sealed class SideWatcher
        {
            private readonly BattleFxDirector _owner;
            private readonly CombatantState _state;
            private readonly bool _isPlayer;

            public SideWatcher(BattleFxDirector owner, CombatantState state, bool isPlayer)
            {
                _owner = owner;
                _state = state;
                _isPlayer = isPlayer;
            }

            public void Subscribe()
            {
                Unsubscribe();
                _state.HpLost += OnHpLost;
                _state.HpHealed += OnHpHealed;
                _state.ShieldAdded += OnShieldAdded;
                _state.ShieldAbsorbed += OnShieldAbsorbed;
            }

            public void Unsubscribe()
            {
                _state.HpLost -= OnHpLost;
                _state.HpHealed -= OnHpHealed;
                _state.ShieldAdded -= OnShieldAdded;
                _state.ShieldAbsorbed -= OnShieldAbsorbed;
            }

            private void OnHpLost(int amount)
            {
                _owner.OnHpLost(_isPlayer, _state, amount);
            }

            private void OnHpHealed(int amount)
            {
                _owner.PlayHealed(_isPlayer, amount);
            }

            private void OnShieldAdded(int amount)
            {
                _owner.PlayShield(_isPlayer, amount, _owner._shieldAddedFormat);
            }

            private void OnShieldAbsorbed(int amount)
            {
                _owner.PlayShield(_isPlayer, amount, _owner._shieldAbsorbedFormat);
            }
        }

        [SerializeField] private BattleController _controller;
        [SerializeField] private UiFxLayer _fx;
        [SerializeField] private UiFxSpawner _spawner;
        [SerializeField] private RectTransform _playerHud;
        [SerializeField] private RectTransform _enemyHud;
        [SerializeField] private RectTransform _playerBar;
        [SerializeField] private RectTransform _enemyBar;
        [SerializeField] private RectTransform _eventBox;

        [Header("라운드 배너")]
        [SerializeField] private string _roundFormat = "ROUND {0}";
        [SerializeField] private string _firstRoundSub = "나 VS AI";
        [SerializeField] private string _nextRoundSub = "다음 라운드";
        [SerializeField] private Color _firstRoundColor = new Color(1f, 0.35f, 0.24f, 1f);
        [SerializeField] private Color _nextRoundColor = new Color(0.9f, 0.64f, 0.24f, 1f);

        [Header("색")]
        [SerializeField] private Color _playerHitColor = new Color(1f, 0.35f, 0.3f, 1f);
        [SerializeField] private Color _enemyHitColor = new Color(1f, 0.88f, 0.54f, 1f);
        [SerializeField] private Color _healColor = new Color(0.44f, 1f, 0.66f, 1f);
        [SerializeField] private Color _shieldColor = new Color(0.61f, 0.76f, 1f, 1f);
        [SerializeField] private Color _playerChunkColor = new Color(0.24f, 1f, 0.58f, 1f);
        [SerializeField] private Color _enemyChunkColor = new Color(1f, 0.48f, 0.36f, 1f);
        [SerializeField] private Color _playerFlashColor = new Color(1f, 0.16f, 0.16f, 1f);
        [SerializeField] private Color _enemyFlashColor = Color.white;
        [SerializeField] private Color[] _hitBurstColors = { Color.white, new Color(1f, 0.88f, 0.54f, 1f), new Color(1f, 0.35f, 0.24f, 1f) };

        [Header("피해 연출 세기 (프로토타입 값)")]
        [SerializeField] private string _damageFormat = "-{0}";
        [SerializeField] private string _healFormat = "+{0} HP";
        [SerializeField] private string _shieldAddedFormat = "실드 +{0}";
        [SerializeField] private string _shieldAbsorbedFormat = "실드 -{0}";
        [SerializeField] private float _heavyHitRatio = 0.12f;
        [SerializeField] private Vector2 _shakePowerRange = new Vector2(0.6f, 2.6f);
        [SerializeField] private float _shakePowerBase = 0.6f;
        [SerializeField] private float _shakePowerPerRatio = 5f;
        [SerializeField] private float _lightFlashOpacity = 0.3f;
        [SerializeField] private float _heavyFlashOpacity = 0.5f;
        [SerializeField] private float _lightFlashDuration = 0.2f;
        [SerializeField] private float _heavyFlashDuration = 0.32f;
        [SerializeField] private float _ringBaseSize = 480f;
        [SerializeField] private float _ringSizePerRatio = 800f;
        [SerializeField] private float _ringDuration = 0.52f;
        [SerializeField] private int _burstBaseCount = 20;
        [SerializeField] private int _burstCountPerRatio = 120;
        [SerializeField] private int _burstMaxCount = 140;
        [SerializeField] private float _burstBaseSpeed = 200f;
        [SerializeField] private float _burstSpeedPerRatio = 500f;
        [SerializeField] private float _punchBase = 0.015f;
        [SerializeField] private float _punchPerRatio = 0.1f;
        [SerializeField] private float _punchMax = 0.06f;
        [SerializeField] private int _damageFontSize = 52;
        [SerializeField] private int _heavyDamageFontSize = 76;
        [SerializeField] private int _supportFontSize = 40;

        [Header("정산 연출 순서와 시간")]
        [SerializeField] private float _bannerGap = 1.15f;
        [SerializeField] private float _collisionDelay = 0.25f;
        [SerializeField] private float _streamDuration = 0.4f;
        [SerializeField] private int _streamCount = 30;
        [SerializeField] private int _tierConfettiThreshold = 3;
        [SerializeField] private int _tierConfettiCount = 60;
        [SerializeField] private int _topTierConfettiCount = 160;
        [SerializeField] private int _topTierSecondConfettiCount = 100;
        [SerializeField] private float _topTierSecondConfettiDelay = 0.7f;
        [SerializeField] private string _handBannerSubFormat = "{0} · 기본 {1}점";
        [SerializeField] private string _critText = "CRITICAL!";
        [SerializeField] private string _critSubFormat = "{0} 점수 → {1}";
        [SerializeField] private Color _critColor = new Color(1f, 0.35f, 0.24f, 1f);
        [SerializeField] private Color _critFlashColor = new Color(1f, 0.88f, 0.54f, 1f);
        [SerializeField] private string _playerLabel = "나";
        [SerializeField] private string _enemyLabel = "상대";
        [SerializeField] private string _drawText = "DRAW";
        [SerializeField] private string _drawSub = "점수가 같아 피해 없음";
        [SerializeField] private Color _drawColor = new Color(0.55f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color _playerStreamColor = new Color(0.55f, 0.72f, 1f, 1f);
        [SerializeField] private Color _enemyStreamColor = new Color(1f, 0.6f, 0.52f, 1f);
        [SerializeField] private float _collisionRingSize = 760f;
        [SerializeField] private float _collisionRingDuration = 0.5f;
        [SerializeField] private int _collisionBurstCount = 36;
        [SerializeField] private float _collisionBurstSpeed = 360f;
        [SerializeField] private float _collisionShakePower = 0.7f;
        [SerializeField] private float _collisionFlashOpacity = 0.55f;
        [SerializeField] private float _collisionFlashDuration = 0.2f;
        [SerializeField] private float _critShakePower = 0.8f;
        [SerializeField] private float _critFlashOpacity = 0.5f;
        [SerializeField] private float _critFlashDuration = 0.25f;
        [SerializeField] private float _bigHitRatio = 0.2f;
        [SerializeField] private float _devastatingRatio = 0.35f;
        [SerializeField] private float _bigHitBannerDelay = 0.45f;
        [SerializeField] private string _bigHitText = "BIG HIT!";
        [SerializeField] private string _devastatingText = "DEVASTATING!";
        [SerializeField] private string _damageSubFormat = "{0} 피해";
        [SerializeField] private Color _bigHitColor = new Color(1f, 0.35f, 0.24f, 1f);

        [Header("효과 카드 연출")]
        [SerializeField] private UiFxCutIn _cutIn;
        [SerializeField] private EffectHandView _handView;
        [SerializeField] private EnemyEffectView _enemyView;
        [SerializeField] private Color _attackColor = new Color(1f, 0.35f, 0.24f, 1f);
        [SerializeField] private Color _disruptColor = new Color(0.49f, 0.3f, 0.77f, 1f);
        [SerializeField] private Color _supportColor = new Color(0.16f, 0.42f, 0.74f, 1f);
        [SerializeField] private Color _wildColor = new Color(0.9f, 0.64f, 0.24f, 1f);
        [SerializeField] private Color _otherColor = new Color(0.18f, 0.64f, 0.42f, 1f);
        [SerializeField] private Color _epicColor = new Color(0.49f, 0.3f, 0.77f, 1f);
        [SerializeField] private Color _legendColor = new Color(0.9f, 0.64f, 0.24f, 1f);
        [SerializeField] private Color _rivalColor = new Color(1f, 0.35f, 0.24f, 1f);
        [SerializeField] private string _epicSkillText = "◆ EPIC SKILL";
        [SerializeField] private string _legendSkillText = "★ LEGENDARY SKILL";
        [SerializeField] private string _rivalEpicText = "◆ RIVAL EPIC";
        [SerializeField] private string _rivalLegendText = "★ RIVAL LEGENDARY";
        [SerializeField] private float _epicCutInDuration = 1.25f;
        [SerializeField] private float _legendCutInDuration = 1.7f;
        [SerializeField] private float _shortCutInDuration = 0.7f;
        [SerializeField] private float _cutInFlashAt = 0.16f;
        [SerializeField] private float _cutInRingAt = 0.2f;
        [SerializeField] private float _activationAt = 0.88f;
        [SerializeField] private float _epicCutInShake = 0.8f;
        [SerializeField] private float _legendCutInShake = 1.3f;
        [SerializeField] private float _epicCutInFlashOpacity = 0.4f;
        [SerializeField] private float _legendCutInFlashOpacity = 0.6f;
        [SerializeField] private float _cutInFlashDuration = 0.3f;
        [SerializeField] private float _cutInRingSize = 900f;
        [SerializeField] private float _cutInRingDuration = 0.7f;
        [SerializeField] private int _cutInBurstCount = 50;
        [SerializeField] private float _cutInBurstSpeed = 420f;
        [SerializeField] private string _activatedText = "효과 발동!";
        [SerializeField] private string _enemyActivatedFormat = "{0}!";
        [SerializeField] private float _activationFlashOpacity = 0.28f;
        [SerializeField] private float _activationFlashDuration = 0.26f;
        [SerializeField] private float _activationRingSize = 420f;
        [SerializeField] private float _activationRingDuration = 0.65f;
        [SerializeField] private float _activationInnerRingSize = 260f;
        [SerializeField] private float _activationInnerRingDuration = 0.5f;
        [SerializeField] private float _activationPunch = 1.02f;
        [SerializeField] private int _activationBurstCount = 34;
        [SerializeField] private float _activationBurstSpeed = 200f;
        [SerializeField] private int _activationFontSize = 56;
        [SerializeField] private float _slotRingSize = 220f;
        [SerializeField] private float _slotRingDuration = 0.45f;
        [SerializeField] private int _slotPopupFontSize = 36;
        [SerializeField] private string _epicGetText = "EPIC!";
        [SerializeField] private string _legendGetText = "LEGENDARY!";
        [SerializeField] private string _getSubFormat = "{0} 획득";
        [SerializeField] private float _getFlashOpacity = 0.7f;
        [SerializeField] private float _getFlashDuration = 0.45f;
        [SerializeField] private float _epicGetShake = 0.7f;
        [SerializeField] private float _legendGetShake = 1.2f;
        [SerializeField] private float _getRingSize = 1000f;
        [SerializeField] private float _getRingDuration = 0.9f;
        [SerializeField] private float _getInnerRingSize = 700f;
        [SerializeField] private float _getInnerRingDuration = 0.7f;
        [SerializeField] private float _getInnerRingDelay = 0.15f;
        [SerializeField] private int _epicGetBurstCount = 50;
        [SerializeField] private int _legendGetBurstCount = 90;
        [SerializeField] private float _getBurstSpeed = 460f;
        [SerializeField] private int _legendGetConfettiCount = 200;
        [SerializeField] private int _legendGetSecondConfettiCount = 100;
        [SerializeField] private float _legendGetSecondConfettiDelay = 0.7f;

        [Header("획득 화면 (프로토타입의 가운데 카드)")]
        [SerializeField] private UiFxGetCard _getCard;
        [SerializeField] private int _getCardActiveRounds = 3;

        [Header("HP 위기 비네트 (프로토타입 danger / clutchPulse)")]
        [SerializeField] private float _dangerHpRatio = 0.3f;
        [SerializeField] private Color _dangerColor = new Color(1f, 0.12f, 0.12f, 1f);
        [SerializeField] private float _dangerStartAlpha = 0.6f;
        [SerializeField] private float _dangerMidAlpha = 0.25f;
        [SerializeField] private float _dangerPeriod = 1.1f;
        [SerializeField] private Vector2 _dangerFalloff = new Vector2(0.1f, 0.175f);
        [SerializeField] private float _clutchEnterRatio = 0.35f;
        [SerializeField] private float _clutchExitRatio = 0.45f;
        [SerializeField] private Color _clutchColor = new Color(0.745f, 0.157f, 1f, 1f);
        [SerializeField] private float _clutchStartAlpha = 0.4f;
        [SerializeField] private float _clutchMidAlpha = 0.55f;
        [SerializeField] private float _clutchPeriod = 0.9f;
        [SerializeField] private Vector2 _clutchFalloff = new Vector2(0.12f, 0.21f);
        [SerializeField] private float _vignetteFadeSeconds = 0.4f;

        [Header("장착 연출 (프로토타입 equip)")]
        [SerializeField] private float _equipBurstDelay = 0.45f;
        [SerializeField] private Color _equipColor = new Color(1f, 0.88f, 0.54f, 1f);
        [SerializeField] private Color[] _equipBurstColors = { new Color(1f, 0.88f, 0.54f, 1f), Color.white };
        [SerializeField] private float _equipRingSize = 240f;
        [SerializeField] private float _equipRingDuration = 0.5f;
        [SerializeField] private int _equipBurstCount = 20;
        [SerializeField] private float _equipBurstSpeed = 120f;

        [Header("별·방울·반짝임 크기")]
        [SerializeField] private float _hitStarBase = 240f;
        [SerializeField] private float _hitStarPerRatio = 400f;
        [SerializeField] private float _hitInnerRingBase = 200f;
        [SerializeField] private float _hitInnerRingPerRatio = 300f;
        [SerializeField] private float _hitInnerRingDuration = 0.38f;
        [SerializeField] private float _collisionStarSize = 260f;
        [SerializeField] private float _critStarSize = 220f;
        [SerializeField] private float _legendRevealStarSize = 320f;
        [SerializeField] private Color _healSparkleColor = new Color(0.44f, 1f, 0.66f, 1f);
        [SerializeField] private int _healSparkleCount = 22;

        [Header("상대 이름표 (시작 화면에서 고른 난이도)")]
        [SerializeField] private string _rivalPrefix = "AI";
        [SerializeField] private string _rivalFormat = "AI · {0}";
        [SerializeField] private string[] _difficultyNames = { "쉬움", "보통", "어려움" };

        [Header("결과 화면 (프로토타입 endov)")]
        [SerializeField] private BattleResultView _resultView;
        [SerializeField] private string _endWinFormat = "승리 · {0}라운드";
        [SerializeField] private string _endLoseFormat = "패배 · {0}라운드";
        [SerializeField] private string _endHpFormat = "나 HP {0} · 상대 HP {1}";
        [SerializeField] private string _endRestartLabel = "새 게임";

        [Header("쇼다운 점수판 (프로토타입 sd)")]
        [SerializeField] private float _boardHeight = 236f;
        [SerializeField] private float _scoreCountSeconds = 0.8f;

        [Header("상대 패 공개 (프로토타입 sd-c open)")]
        [SerializeField] private EnemyHandView _enemyHandView;
        [SerializeField] private float _afterRevealPause = 0.2f;
        [SerializeField] private Color[] _flipBurstColors = { Color.white, new Color(1f, 0.88f, 0.54f, 1f) };
        [SerializeField] private int _flipBurstCount = 5;
        [SerializeField] private float _flipBurstSpeed = 40f;

        [Header("낸 카드 튀기 (프로토타입 족보 배너 때)")]
        [SerializeField] private PokerHandView _pokerHand;

        [Header("점수 카운트업 연결")]
        [SerializeField] private EffectSettleSummaryView _settleSummary;

        [Header("패시브 발동 연출")]
        [SerializeField] private float _passiveStep = 0.38f;
        [SerializeField] private float _passiveTail = 0.5f;
        [SerializeField] private string _passiveFormat = "효과 발동! {0}";
        [SerializeField] private string _epicPassiveText = "◆ EPIC";
        [SerializeField] private string _legendPassiveText = "★ LEGENDARY";
        [SerializeField] private float _passiveRingSize = 160f;
        [SerializeField] private float _passiveRingDuration = 0.45f;
        [SerializeField] private float _passiveHighRingSize = 300f;
        [SerializeField] private float _passiveHighRingDuration = 0.6f;
        [SerializeField] private int _passiveBurstCount = 10;
        [SerializeField] private float _passiveBurstSpeed = 70f;
        [SerializeField] private int _passiveHighBurstCount = 26;
        [SerializeField] private float _passiveHighBurstSpeed = 150f;
        [SerializeField] private float _passiveHighFlashOpacity = 0.3f;
        [SerializeField] private float _passiveHighFlashDuration = 0.26f;
        [SerializeField] private float _passivePopupLift = 60f;
        [SerializeField] private float _passiveRarityLift = 120f;

        [Header("히트스톱 (초)")]
        [SerializeField] private float _lightHitStop = 0.045f;
        [SerializeField] private float _heavyHitStop = 0.1f;
        [SerializeField] private float _critHitStop = 0.12f;

        [Header("배경 문양")]
        [SerializeField] private UiFxBackground _background;
        [SerializeField] private float _roundPulse = 0.6f;
        [SerializeField] private float _hitPulse = 0.5f;
        [SerializeField] private float _activationPulse = 0.8f;

        [Header("전투 종료 연출")]
        [SerializeField] private float _endDelay = 0.6f;
        [SerializeField] private string _victoryText = "VICTORY";
        [SerializeField] private string _victorySub = "승리!";
        [SerializeField] private string _defeatText = "DEFEAT";
        [SerializeField] private string _defeatSub = "패배...";
        [SerializeField] private string _endDrawText = "DRAW";
        [SerializeField] private string _endDrawSub = "무승부";
        [SerializeField] private Color _victoryColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _defeatColor = new Color(1f, 0.2f, 0.2f, 1f);
        [Header("승리 (프로토타입 값)")]
        [SerializeField] private Color _winFlashColor = new Color(1f, 0.88f, 0.54f, 1f);
        [SerializeField] private float _winFlashOpacity = 0.6f;
        [SerializeField] private float _winFlashDuration = 0.5f;
        [SerializeField] private float _winBurstDelay = 0.45f;
        [SerializeField] private float _winRingSize = 900f;
        [SerializeField] private float _winRingDuration = 0.9f;
        [SerializeField] private int _winBurstCount = 80;
        [SerializeField] private float _winBurstSpeed = 420f;
        [SerializeField] private Color[] _winBurstColors =
        {
            new Color(1f, 0.88f, 0.54f, 1f), Color.white, new Color(0.16f, 0.42f, 0.74f, 1f), new Color(1f, 0.35f, 0.24f, 1f)
        };
        [SerializeField] private int _winConfettiCount = 220;
        [SerializeField] private int _winSecondConfettiCount = 120;
        [SerializeField] private float _winSecondConfettiDelay = 1.5f;

        [Header("패배 (프로토타입 값)")]
        [SerializeField] private Color _loseFlashColor = new Color(1f, 0.16f, 0.16f, 1f);
        [SerializeField] private float _loseFlashOpacity = 0.5f;
        [SerializeField] private float _loseFlashDuration = 0.6f;
        [SerializeField] private float _loseShake = 1.6f;
        [SerializeField] private float _loseCrackDelay = 0.2f;
        [SerializeField] private int _loseCrackLines = 9;
        [SerializeField] private int _loseCrackSegments = 6;
        [SerializeField] private float _loseCrackDrawSeconds = 0.6f;
        [SerializeField] private float _loseCrackLife = 6.8f;
        [SerializeField] private Color _loseWashColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private float _loseWashOpacity = 0.5f;
        [SerializeField] private float _loseWashDelay = 0.7f;
        [SerializeField] private float _loseWashDuration = 6f;

        [Header("드래프트 카드 공개 연출")]
        [SerializeField] private EffectDraftView _draftView;
        [SerializeField] private Color _legendRevealFlashColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color _epicRevealFlashColor = new Color(0.91f, 0.82f, 1f, 1f);
        [SerializeField] private float _legendRevealFlashOpacity = 0.55f;
        [SerializeField] private float _epicRevealFlashOpacity = 0.35f;
        [SerializeField] private float _legendRevealFlashDuration = 0.35f;
        [SerializeField] private float _epicRevealFlashDuration = 0.28f;
        [SerializeField] private float _legendRevealRingSize = 440f;
        [SerializeField] private float _epicRevealRingSize = 330f;
        [SerializeField] private float _legendRevealRingDuration = 0.65f;
        [SerializeField] private float _epicRevealRingDuration = 0.55f;
        [SerializeField] private int _legendRevealBurstCount = 44;
        [SerializeField] private int _epicRevealBurstCount = 28;
        [SerializeField] private float _legendRevealBurstSpeed = 230f;
        [SerializeField] private float _epicRevealBurstSpeed = 170f;
        [SerializeField] private float _legendRevealShake = 0.8f;

        [SerializeField]
        private List<HandFx> _handFx = new List<HandFx>
        {
            new HandFx(HandRank.HighCard, "HIGH CARD", new Color(0.62f, 0.8f, 0.72f, 1f), 0),
            new HandFx(HandRank.OnePair, "ONE PAIR", new Color(0.62f, 0.8f, 0.72f, 1f), 0),
            new HandFx(HandRank.TwoPair, "TWO PAIR", new Color(0.16f, 0.42f, 0.74f, 1f), 1),
            new HandFx(HandRank.ThreeOfAKind, "THREE OF A KIND", new Color(0.18f, 0.64f, 0.42f, 1f), 1),
            new HandFx(HandRank.Straight, "STRAIGHT", new Color(0.9f, 0.64f, 0.24f, 1f), 2),
            new HandFx(HandRank.Flush, "FLUSH", new Color(0.9f, 0.64f, 0.24f, 1f), 2),
            new HandFx(HandRank.FullHouse, "FULL HOUSE", new Color(0.49f, 0.3f, 0.77f, 1f), 2),
            new HandFx(HandRank.FourOfAKind, "FOUR OF A KIND", new Color(1f, 0.35f, 0.24f, 1f), 3),
            new HandFx(HandRank.StraightFlush, "STRAIGHT FLUSH", new Color(1f, 0.35f, 0.24f, 1f), 3),
            new HandFx(HandRank.RoyalStraightFlush, "ROYAL FLUSH", Color.white, 4)
        };

        private readonly List<TimedAction> _timeline = new List<TimedAction>();
        private readonly List<Action> _due = new List<Action>();

        // 같은 카드의 컷인은 한 판에서 두 번째부터 짧게 보여 준다(프로토타입 규칙). 새 전투가 시작되면 비운다.
        private readonly HashSet<(bool, EffectCardData)> _cutInSeen = new HashSet<(bool, EffectCardData)>();

        // 컷인이 겹쳐 덮어써지지 않도록, 앞 컷인이 끝나기까지 남은 시간을 센다.
        private float _cutInBusyFor;
        private SideWatcher _playerWatcher;
        private SideWatcher _enemyWatcher;
        private EffectBattleCoordinator _coordinator;
        private float _clock;

        // 정산 중 일어나는 피해 연출을 이 시간만큼 미룬다. 점수 연출이 끝난 뒤에 맞는 모습이 나오게 하려는 것이다.
        private float _hitDelay;
        private bool _started;
        private UiEndOverlay _endOverlay;
        private UiShowdownBoard _showdown;
        private UiFxHudHit _playerHudHit;
        private UiFxHudHit _enemyHudHit;
        private UiFxVignette _dangerVignette;
        private UiFxVignette _clutchVignette;
        private bool _clutchOn;
        private bool _passiveFlashed;
        private float _hitStopRemaining;
        private float _savedTimeScale = 1f;

        // 상태 객체가 BattleController.Awake에서 만들어지므로, 모든 Awake가 끝난 Start에서 연결한다.
        private void Start()
        {
            var state = _controller.State;
            _playerWatcher = new SideWatcher(this, state.Player, true);
            _enemyWatcher = new SideWatcher(this, state.Enemy, false);
            _coordinator = _controller.Coordinator;
            BuildHudReactions();
            ShowRivalDifficulty();
            _started = true;
            if (_settleSummary != null)
            {
                _settleSummary.SetDirected(true);
            }

            Subscribe();

            // 컨트롤러가 먼저 Start해 첫 라운드 알림이 이미 지나갔다면 지금 라운드를 직접 보여 준다.
            if (_controller.Round > 0)
            {
                PlayRoundBanner(_controller.Round);
            }
        }

        private void OnEnable()
        {
            if (_started)
            {
                Subscribe();
                if (_settleSummary != null)
                {
                    _settleSummary.SetDirected(true);
                }
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            EndHitStop();
            if (_started)
            {
                _playerHudHit.Cancel();
                _enemyHudHit.Cancel();
            }

            if (_settleSummary != null)
            {
                _settleSummary.SetDirected(false);
            }
        }

        private void Update()
        {
            if (_hitStopRemaining > 0f)
            {
                // timeScale이 0이면 deltaTime이 멈추므로 풀리는 시간은 실제 시간으로 센다.
                _hitStopRemaining -= Time.unscaledDeltaTime;
                if (_hitStopRemaining <= 0f)
                {
                    EndHitStop();
                }
            }

            Tick(Time.deltaTime);
        }

        // 프로토타입의 freeze: 강한 타격 순간에 화면 연출을 아주 잠깐 멈춰 묵직함을 준다.
        private void StartHitStop(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            if (_hitStopRemaining <= 0f)
            {
                _savedTimeScale = Time.timeScale;
            }

            _hitStopRemaining = Mathf.Max(_hitStopRemaining, seconds);
            Time.timeScale = 0f;
        }

        private void EndHitStop()
        {
            if (_hitStopRemaining > 0f || Time.timeScale == 0f)
            {
                Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            }

            _hitStopRemaining = 0f;
        }

        // 시작 화면에서 고른 난이도가 상대 이름표에 보이도록 "AI · 보통" 글자를 바꾼다.
        private void ShowRivalDifficulty()
        {
            int index = (int)_controller.Difficulty;
            if (index < 0 || index >= _difficultyNames.Length)
            {
                return;
            }

            foreach (var label in _enemyHud.GetComponentsInChildren<Text>(true))
            {
                if (label.text.StartsWith(_rivalPrefix))
                {
                    label.text = string.Format(_rivalFormat, _difficultyNames[index]);
                }
            }
        }

        // 씬에 오브젝트를 더하지 않고 기존 HUD에 흔들림을 걸고, 비네트 두 장은 효과 레이어 맨 아래에 코드로 만든다.
        private void BuildHudReactions()
        {
            var settings = new UiFxHudHit.Settings();
            _playerHudHit = new UiFxHudHit(_playerHud, _playerBar, settings);
            _enemyHudHit = new UiFxHudHit(_enemyHud, _enemyBar, settings);

            // 점수판은 이벤트 박스의 위쪽 칸을 덮는다. 같은 부모(Stage)의 맨 위에 두어 제목 글자 위에 그려지게 한다.
            var stage = (RectTransform)_eventBox.parent;
            var boxImage = _eventBox.GetComponent<Image>();
            Color boardColor = boxImage != null ? boxImage.color : new Color(0.05f, 0.15f, 0.12f, 1f);
            boardColor.a = 1f;
            Vector2 boardSize = new Vector2(_eventBox.rect.width, _boardHeight);
            Vector2 boardPosition = _eventBox.anchoredPosition + Vector2.up * (_eventBox.rect.height / 2f - _boardHeight / 2f);
            var font = _playerBar.GetComponentInChildren<Text>(true).font;
            _showdown = UiShowdownBoard.Create(stage, font, boardSize, boardPosition, boardColor);
            _showdown.transform.SetAsLastSibling();
            _showdown.CardOpened += OnBoardCardOpened;

            var layer = (RectTransform)_fx.transform;
            _dangerVignette = UiFxVignette.Create(layer, "DangerVignette", _dangerFalloff);
            _clutchVignette = UiFxVignette.Create(layer, "ClutchVignette", _clutchFalloff);
            _clutchVignette.transform.SetAsFirstSibling();
            _dangerVignette.transform.SetAsFirstSibling();

            // 결과 화면은 비네트 바로 위, 플래시와 컷인 아래에 둔다. 승리 폭죽과 번쩍임이 그 위에 그려진다.
            _endOverlay = UiEndOverlay.Create(layer, font);
            _endOverlay.transform.SetSiblingIndex(2);
            if (_resultView != null)
            {
                _endOverlay.SetButtonAction(_resultView.TriggerNext, _endRestartLabel);
            }
        }

        // 내 HP가 위험하면 붉게, 양쪽 모두 벼랑 끝이면 보라색으로 화면 가장자리가 숨 쉰다. 접전은 들어갈 때와 나올 때 기준을 달리한다.
        private void RefreshMood()
        {
            var state = _controller.State;
            float mine = state.Player.Hp / (float)state.Player.MaxHp;
            float theirs = state.Enemy.Hp / (float)state.Enemy.MaxHp;
            bool alive = state.Player.Hp > 0 && state.Enemy.Hp > 0;

            bool danger = alive && mine <= _dangerHpRatio;
            if (danger && !_dangerVignette.IsActive)
            {
                _dangerVignette.Show(_dangerColor, _dangerStartAlpha, _dangerMidAlpha, _dangerPeriod, _vignetteFadeSeconds);
            }
            else if (!danger && _dangerVignette.IsActive)
            {
                _dangerVignette.Hide();
            }

            float limit = _clutchOn ? _clutchExitRatio : _clutchEnterRatio;
            _clutchOn = alive && mine <= limit && theirs <= limit;
            if (_clutchOn && !_clutchVignette.IsActive)
            {
                _clutchVignette.Show(_clutchColor, _clutchStartAlpha, _clutchMidAlpha, _clutchPeriod, _vignetteFadeSeconds);
            }
            else if (!_clutchOn && _clutchVignette.IsActive)
            {
                _clutchVignette.Hide();
            }
        }

        public void Tick(float dt)
        {
            if (_started)
            {
                _showdown.Tick(dt);
                _endOverlay.Tick(dt);
                _playerHudHit.Tick(dt);
                _enemyHudHit.Tick(dt);
                _dangerVignette.Tick(dt);
                _clutchVignette.Tick(dt);
            }

            _cutInBusyFor = Mathf.Max(0f, _cutInBusyFor - dt);

            if (_timeline.Count == 0)
            {
                return;
            }

            _clock += dt;

            // 실행 중에 새 예약이 생길 수 있어서, 시간이 된 것을 먼저 모아 지우고 나서 실행한다.
            _due.Clear();
            for (int i = _timeline.Count - 1; i >= 0; i--)
            {
                if (_timeline[i].Time <= _clock)
                {
                    _due.Add(_timeline[i].Action);
                    _timeline.RemoveAt(i);
                }
            }

            for (int i = _due.Count - 1; i >= 0; i--)
            {
                _due[i]();
            }

            if (_timeline.Count == 0)
            {
                _clock = 0f;
            }
        }

        private void Subscribe()
        {
            _controller.RoundStarted -= OnRoundStarted;
            _controller.RoundStarted += OnRoundStarted;
            _controller.BattleEnded -= OnBattleEnded;
            _controller.BattleEnded += OnBattleEnded;
            _playerWatcher.Subscribe();
            _enemyWatcher.Subscribe();

            if (_coordinator != null)
            {
                _coordinator.ScoresResolved -= OnScoresResolved;
                _coordinator.ScoresResolved += OnScoresResolved;
                _coordinator.Settled -= OnSettled;
                _coordinator.Settled += OnSettled;
                _coordinator.Logged -= OnLogged;
                _coordinator.Logged += OnLogged;
            }

            if (_draftView != null)
            {
                _draftView.CardRevealed -= OnCardRevealed;
                _draftView.CardRevealed += OnCardRevealed;
                _draftView.CardEquipped -= OnCardEquipped;
                _draftView.CardEquipped += OnCardEquipped;
            }
        }

        private void Unsubscribe()
        {
            if (!_started)
            {
                return;
            }

            _controller.RoundStarted -= OnRoundStarted;
            _controller.BattleEnded -= OnBattleEnded;
            _playerWatcher.Unsubscribe();
            _enemyWatcher.Unsubscribe();

            if (_coordinator != null)
            {
                _coordinator.ScoresResolved -= OnScoresResolved;
                _coordinator.Settled -= OnSettled;
                _coordinator.Logged -= OnLogged;
            }

            if (_draftView != null)
            {
                _draftView.CardRevealed -= OnCardRevealed;
                _draftView.CardEquipped -= OnCardEquipped;
            }
        }

        // 점수판의 카드가 뒤집히기 시작하는 순간 작은 불꽃이 튄다.
        private void OnBoardCardOpened(RectTransform card)
        {
            _spawner.Burst(_spawner.ToLocal(card), _flipBurstColors, _flipBurstCount, _flipBurstSpeed);
        }

        private void Schedule(float delay, Action action)
        {
            if (delay <= 0f)
            {
                action();
                return;
            }

            _timeline.Add(new TimedAction(_clock + delay, action));
        }

        // 다음 라운드가 시작되면 앞 라운드의 남은 연출은 의미가 없으므로 버린다.
        private void OnRoundStarted(int round)
        {
            _timeline.Clear();
            _clock = 0f;
            _hitDelay = 0f;
            _cutInBusyFor = 0f;
            ClearSpotlight();
            if (_showdown != null)
            {
                _showdown.Hide();
            }

            _endOverlay.Hide();
            PlayRoundBanner(round);
        }

        // 마지막 피해 연출이 끝난 뒤에 승패 연출이 나오도록 피해 지연 뒤로 미룬다.
        private void OnBattleEnded(BattleWinner winner, int round)
        {
            Schedule(_hitDelay + _endDelay, () => PlayBattleEnd(winner));
        }

        private void PlayBattleEnd(BattleWinner winner)
        {
            _dangerVignette.Hide();
            _clutchVignette.Hide();
            _clutchOn = false;
            if (winner == BattleWinner.Player)
            {
                _fx.Flash(_winFlashColor, _winFlashOpacity, _winFlashDuration);
                Schedule(_winBurstDelay, () =>
                {
                    _spawner.Burst(Vector2.zero, _winBurstColors, _winBurstCount, _winBurstSpeed);
                    _spawner.Ring(Vector2.zero, _winFlashColor, _winRingSize, _winRingDuration);
                });
                _spawner.Confetti(_winConfettiCount);
                Schedule(_winSecondConfettiDelay, () => _spawner.Confetti(_winSecondConfettiCount));
                ShowEndOverlay(true);
            }
            else if (winner == BattleWinner.Enemy)
            {
                _fx.Flash(_loseFlashColor, _loseFlashOpacity, _loseFlashDuration);
                _fx.Shake(_loseShake);
                Schedule(_loseCrackDelay, () => _spawner.Crack(_loseCrackLines, _loseCrackSegments, _loseCrackDrawSeconds, _loseCrackLife));
                // 프로토타입은 화면 전체를 흑백으로 만든다. UI에는 그 필터가 없어서 회색 막으로 색이 빠진 느낌만 낸다.
                Schedule(_loseWashDelay, () => _fx.Flash(_loseWashColor, _loseWashOpacity, _loseWashDuration));
                ShowEndOverlay(false);
            }
            else
            {
                _fx.ShowBanner(_endDrawText, _endDrawSub, _drawColor, false);
            }
        }

        // 버튼으로 새 게임을 시작할 수 있을 때는 결과 화면을, 아니면 이전처럼 배너를 보여 준다.
        private void ShowEndOverlay(bool win)
        {
            if (_resultView == null)
            {
                _fx.ShowBanner(win ? _victoryText : _defeatText, win ? _victorySub : _defeatSub, win ? _victoryColor : _defeatColor, win);
                return;
            }

            var state = _controller.State;
            _endOverlay.Show(win, _controller.Round, state.Player.Hp, state.Enemy.Hp, _endWinFormat, _endLoseFormat, _endHpFormat);
        }

        private void PlayRoundBanner(int round)
        {
            Pulse(_roundPulse);
            bool first = round == 1;
            _fx.ShowBanner(string.Format(_roundFormat, round), first ? _firstRoundSub : _nextRoundSub,
                first ? _firstRoundColor : _nextRoundColor, false);
        }

        // 정산 연출: 족보 배너 → 치명타 → 충돌 → 광선(또는 무승부). 마지막에 피해 연출이 이어지도록 지연 시간을 정한다.
        private void OnScoresResolved(EffectBattleCoordinator.SettlePreview preview)
        {
            // 정산 직전에 상대 액티브 컷인이 돌고 있다면 그것이 끝난 뒤에 점수 연출을 시작한다.
            float time = _cutInBusyFor;

            // 점수판은 정산이 시작되는 순간 0점으로 먼저 떠 있고, 뒤에서 숫자가 올라간다.
            if (_showdown != null)
            {
                Schedule(time, () =>
                {
                    _showdown.Show(_playerLabel, _enemyLabel, _pokerHand != null ? _pokerHand.GetSelectedCards() : null,
                        _controller.EnemyBestCards, _enemyHandView != null ? _enemyHandView.BackSprite : null);
                });
                Schedule(time + _showdown.IntroSeconds, _showdown.OpenCards);
            }

            // 프로토타입 순서: 패를 하나씩 뒤집어 공개하고 잠깐 쉰 뒤에 족보 연출이 시작된다.
            if (_showdown != null)
            {
                time += _showdown.IntroSeconds + _showdown.RevealSeconds + _afterRevealPause;
            }

            var handFx = FindHandFx(preview.PlayerHand.HandRank);
            if (handFx != null && handFx.Tier >= 1)
            {
                Schedule(time, () => PlayHandBanner(handFx, preview.PlayerHand));
                time += _bannerGap;
            }

            time = SchedulePassiveFlash(time, true, preview.PlayerPassives);
            time = SchedulePassiveFlash(time, false, preview.EnemyPassives);
            if (_passiveFlashed)
            {
                time += _passiveTail;
                Schedule(time, ClearSpotlight);
                _passiveFlashed = false;
            }

            // 프로토타입 순서: 패시브 발동 뒤에 점수 숫자가 올라가고, 그 다음에 치명타가 터진다.
            if (_settleSummary != null)
            {
                Schedule(time, _settleSummary.BeginCount);
            }

            if (_showdown != null)
            {
                Schedule(time, () =>
                {
                    _showdown.ShowDetails(preview.PlayerHand.Name, preview.EnemyHand.Name);
                    _showdown.CountTo(preview.PlayerBaseScore, preview.EnemyBaseScore, _scoreCountSeconds);
                });
            }

            time += _scoreCountSeconds;

            if (preview.PlayerCrit)
            {
                Schedule(time, () => PlayCrit(true, preview.PlayerScore));
                time += _bannerGap;
            }

            if (preview.EnemyCrit)
            {
                Schedule(time, () => PlayCrit(false, preview.EnemyScore));
                time += _bannerGap;
            }

            time += _collisionDelay;

            // 두 점수가 부딪히려고 달려드는 동안 기다렸다가, 닿는 순간 충돌 연출을 터뜨린다.
            if (_showdown != null)
            {
                bool hasWinner = preview.Winner != BattleWinner.Draw;
                bool playerWon = preview.Winner == BattleWinner.Player;
                Schedule(time, () => _showdown.Clash(hasWinner, playerWon));
                time += _showdown.ClashSeconds;
            }

            Schedule(time, PlayCollision);

            if (preview.Winner == BattleWinner.Draw)
            {
                Schedule(time + _collisionDelay, PlayDraw);
                time += _bannerGap;
            }
            else
            {
                bool playerWon = preview.Winner == BattleWinner.Player;
                Schedule(time + _collisionDelay, () => PlayStream(playerWon));
                time += _collisionDelay + _streamDuration;
            }

            _hitDelay = time;
        }

        // 프로토타입의 passiveFlash: 발동하는 패시브를 하나씩 밝히며 링과 불꽃을 터뜨린다. 끝난 시각을 돌려준다.
        private float SchedulePassiveFlash(float time, bool isPlayer, IReadOnlyList<EffectCardData> passives)
        {
            if (passives == null)
            {
                return time;
            }

            int order = 0;
            for (int i = 0; i < passives.Count; i++)
            {
                var card = passives[i];
                if (card == null || card.IsActive)
                {
                    continue;
                }

                var slot = isPlayer ? (_handView != null ? _handView.GetSlotRect(card) : null)
                    : (_enemyView != null ? _enemyView.GetSlotRect(card) : null);
                if (slot == null)
                {
                    continue;
                }

                order++;
                int number = order;
                Schedule(time, () => PlayPassiveFlash(isPlayer, card, slot, number));
                time += _passiveStep;
                _passiveFlashed = true;
            }

            return time;
        }

        private void PlayPassiveFlash(bool isPlayer, EffectCardData card, RectTransform slot, int number)
        {
            if (isPlayer)
            {
                _handView.SetSpotlight(card);
                if (_enemyView != null)
                {
                    _enemyView.SetSpotlight(null);
                }
            }
            else
            {
                _enemyView.SetSpotlight(card);
                if (_handView != null)
                {
                    _handView.SetSpotlight(null);
                }
            }

            bool high = card.Grade == EffectGrade.Epic || card.Grade == EffectGrade.Legend;
            Color color = card.Grade == EffectGrade.Legend ? _legendColor : card.Grade == EffectGrade.Epic ? _epicColor : _wildColor;
            Vector2 position = _spawner.ToLocal(slot);
            _spawner.Ring(position, color, high ? _passiveHighRingSize : _passiveRingSize, high ? _passiveHighRingDuration : _passiveRingDuration);
            _spawner.Burst(position, new[] { color, Color.white }, high ? _passiveHighBurstCount : _passiveBurstCount,
                high ? _passiveHighBurstSpeed : _passiveBurstSpeed);
            _spawner.Popup(position + Vector2.up * _passivePopupLift, string.Format(_passiveFormat, number), color, _supportFontSize);
            Pulse(_activationPulse);

            if (high)
            {
                _fx.Flash(color, _passiveHighFlashOpacity, _passiveHighFlashDuration);
                _spawner.Popup(position + Vector2.up * _passiveRarityLift, card.Grade == EffectGrade.Legend ? _legendPassiveText : _epicPassiveText,
                    color, _supportFontSize);
            }
        }

        private void ClearSpotlight()
        {
            if (_handView != null)
            {
                _handView.SetSpotlight(null);
            }

            if (_enemyView != null)
            {
                _enemyView.SetSpotlight(null);
            }
        }

        // 정산이 끝나면(피해 적용까지 마친 뒤) 지연을 풀어서, 이후 사건은 바로 연출한다.
        private void OnSettled(EffectBattleCoordinator.SettleResult result)
        {
            _hitDelay = 0f;
        }

        private HandFx FindHandFx(HandRank rank)
        {
            foreach (var entry in _handFx)
            {
                if (entry.HandRank == rank)
                {
                    return entry;
                }
            }

            return null;
        }

        private void PlayHandBanner(HandFx handFx, PokerHandResult hand)
        {
            bool topTier = handFx.Tier >= 4;

            // 프로토타입: 족보 배너와 함께 점수판의 내 카드가 차례로 통통 튄다.
            if (_showdown != null)
            {
                _showdown.HopMine();
            }

            _fx.ShowBanner(handFx.EnglishName, string.Format(_handBannerSubFormat, hand.Name, hand.Score), handFx.Color, topTier);

            if (handFx.Tier >= _tierConfettiThreshold)
            {
                _spawner.Confetti(topTier ? _topTierConfettiCount : _tierConfettiCount);
                if (topTier)
                {
                    Schedule(_topTierSecondConfettiDelay, () => _spawner.Confetti(_topTierSecondConfettiCount));
                }
            }
        }

        private void PlayCrit(bool isPlayer, int score)
        {
            StartHitStop(_critHitStop);
            _fx.Flash(_critFlashColor, _critFlashOpacity, _critFlashDuration);
            _fx.Shake(_critShakePower);
            _fx.ShowBanner(_critText, string.Format(_critSubFormat, isPlayer ? _playerLabel : _enemyLabel, score), _critColor, false);
            // 점수판이 있으면 그 점수 숫자가 최종 점수로 바뀌며 튀고, 별도 그 숫자 자리에서 터진다.
            RectTransform critTarget = isPlayer ? _playerHud : _enemyHud;
            if (_showdown != null)
            {
                _showdown.SetScore(isPlayer, score);
                critTarget = isPlayer ? _showdown.MyScoreRect : _showdown.EnemyScoreRect;
            }

            Vector2 critPosition = _spawner.ToLocal(critTarget);
            _spawner.Ring(critPosition, _critFlashColor, _collisionRingSize, _collisionRingDuration);
            _spawner.Star(critPosition, _critFlashColor, _critStarSize);
        }

        private void Pulse(float amount)
        {
            if (_background != null)
            {
                _background.Pulse(amount);
            }
        }

        private void PlayCollision()
        {
            Pulse(_hitPulse);
            Vector2 center = _spawner.ToLocal(_showdown != null ? _showdown.VersusRect : _eventBox);
            _fx.Flash(Color.white, _collisionFlashOpacity, _collisionFlashDuration);
            _fx.Shake(_collisionShakePower);
            _spawner.Ring(center, Color.white, _collisionRingSize, _collisionRingDuration);
            _spawner.Star(center, _critFlashColor, _collisionStarSize);
            _spawner.Burst(center, _hitBurstColors, _collisionBurstCount, _collisionBurstSpeed);
        }

        private void PlayDraw()
        {
            _fx.ShowBanner(_drawText, _drawSub, _drawColor, false);
        }

        private void PlayStream(bool playerWon)
        {
            Vector2 from = _spawner.ToLocal(playerWon ? _playerHud : _enemyHud);
            Vector2 to = _spawner.ToLocal(playerWon ? _enemyHud : _playerHud);
            _spawner.Stream(from, to, playerWon ? _playerStreamColor : _enemyStreamColor, _streamCount, _streamDuration);
        }

        // 피해는 사건이 일어난 순간의 HP 비율을 기억해 두었다가, 정산 연출이 끝난 뒤에 보여 준다.
        private void OnHpLost(bool isPlayer, CombatantState state, int amount)
        {
            float afterPercent = Mathf.Clamp(state.Hp * 100f / state.MaxHp, 0f, 100f);
            float beforePercent = Mathf.Clamp((state.Hp + amount) * 100f / state.MaxHp, 0f, 100f);
            int maxHp = state.MaxHp;
            Schedule(_hitDelay, () => PlayHpLost(isPlayer, amount, maxHp, beforePercent, afterPercent));
        }

        private void PlayHpLost(bool isPlayer, int amount, int maxHp, float beforePercent, float afterPercent)
        {
            Pulse(_hitPulse);
            (isPlayer ? _playerHudHit : _enemyHudHit).Play();
            RefreshMood();
            float ratio = amount / (float)maxHp;
            bool heavy = ratio >= _heavyHitRatio;
            StartHitStop(heavy ? _heavyHitStop : _lightHitStop);
            var hud = isPlayer ? _playerHud : _enemyHud;
            Vector2 position = _spawner.ToLocal(hud);
            float power = Mathf.Clamp(_shakePowerBase + ratio * _shakePowerPerRatio, _shakePowerRange.x, _shakePowerRange.y);

            _spawner.Popup(position, string.Format(_damageFormat, amount), isPlayer ? _playerHitColor : _enemyHitColor,
                heavy ? _heavyDamageFontSize : _damageFontSize);
            _spawner.Ring(position, isPlayer ? _playerHitColor : _enemyHitColor, _ringBaseSize + ratio * _ringSizePerRatio, _ringDuration);
            _spawner.Ring(position, Color.white, _hitInnerRingBase + ratio * _hitInnerRingPerRatio, _hitInnerRingDuration);
            _spawner.Star(position, isPlayer ? _playerHitColor : _enemyHitColor, _hitStarBase + ratio * _hitStarPerRatio);
            _spawner.Burst(position, _hitBurstColors,
                Mathf.Min(_burstMaxCount, _burstBaseCount + Mathf.RoundToInt(ratio * _burstCountPerRatio)),
                _burstBaseSpeed + ratio * _burstSpeedPerRatio);

            _fx.Shake(power);
            _fx.Punch(1f + Mathf.Min(_punchMax, _punchBase + ratio * _punchPerRatio));
            _fx.Flash(isPlayer ? _playerFlashColor : _enemyFlashColor,
                heavy ? _heavyFlashOpacity : _lightFlashOpacity,
                heavy ? _heavyFlashDuration : _lightFlashDuration);
            _spawner.Chunks(isPlayer ? _playerBar : _enemyBar, beforePercent, afterPercent,
                isPlayer ? _playerChunkColor : _enemyChunkColor);

            if (ratio >= _bigHitRatio)
            {
                bool devastating = ratio >= _devastatingRatio;
                Schedule(_bigHitBannerDelay, () => _fx.ShowBanner(devastating ? _devastatingText : _bigHitText,
                    string.Format(_damageSubFormat, amount), _bigHitColor, false));
            }
        }

        private void OnLogged(EffectLogEntry entry)
        {
            switch (entry.Kind)
            {
                case EffectLogKind.BattleStarted:
                    _cutInSeen.Clear();
                    RefreshMood();
                    break;
                case EffectLogKind.ActiveUsed:
                    PlayActiveUsed(entry.IsPlayer, entry.Card);
                    break;
                case EffectLogKind.Drafted:
                    // 상대가 무엇을 골랐는지는 숨기므로 내가 영웅·전설을 얻었을 때만 축하한다.
                    if (entry.IsPlayer && entry.Card.Grade >= EffectGrade.Epic)
                    {
                        PlayRarityGet(entry.Card);
                    }

                    break;
            }
        }

        // 영웅·전설은 컷인이 먼저 지나가고, 그 뒤에 발동 연출이 터진다. 일반·희귀는 바로 발동한다.
        private void PlayActiveUsed(bool isPlayer, EffectCardData card)
        {
            float activationDelay = 0f;
            if (card.Grade >= EffectGrade.Epic)
            {
                activationDelay = ScheduleCutIn(isPlayer, card);
            }

            Schedule(activationDelay, () => PlayActivation(isPlayer, card));
        }

        // 컷인을 줄 세워 예약하고, 발동 연출을 시작할 시각을 돌려준다.
        private float ScheduleCutIn(bool isPlayer, EffectCardData card)
        {
            bool legend = card.Grade == EffectGrade.Legend;

            // 같은 카드를 이미 보여 줬다면 짧게 지나간다.
            bool seen = !_cutInSeen.Add((isPlayer, card));
            float duration = seen ? _shortCutInDuration : legend ? _legendCutInDuration : _epicCutInDuration;
            float start = _cutInBusyFor;
            _cutInBusyFor += duration;

            Color color = isPlayer ? (legend ? _legendColor : _epicColor) : _rivalColor;
            string grade = isPlayer
                ? (legend ? _legendSkillText : _epicSkillText)
                : (legend ? _rivalLegendText : _rivalEpicText);
            Color accent = legend ? _legendColor : _epicColor;

            // 컷인 오브젝트가 씬에 없어도 나머지 연출과 게임 진행은 멈추지 않게 한다.
            if (_cutIn != null)
            {
                Schedule(start, () => _cutIn.Show(grade, card.DisplayName, card.Description, color, duration));
            }

            Schedule(start + duration * _cutInFlashAt, () =>
            {
                _fx.Flash(accent, legend ? _legendCutInFlashOpacity : _epicCutInFlashOpacity, _cutInFlashDuration);
                _fx.Shake(legend ? _legendCutInShake : _epicCutInShake);
            });

            if (legend)
            {
                Schedule(start + duration * _cutInRingAt, () =>
                {
                    _spawner.Ring(Vector2.zero, _legendColor, _cutInRingSize, _cutInRingDuration);
                    _spawner.Burst(Vector2.zero, new[] { _legendColor, Color.white, _rivalColor }, _cutInBurstCount, _cutInBurstSpeed);
                });
            }

            return start + duration * _activationAt;
        }

        // 액티브가 발동하는 순간: 이벤트 상자 가운데에서 분류 색 번쩍임·링·파편이 터지고, 카드 자리에도 반응이 간다.
        private void PlayActivation(bool isPlayer, EffectCardData card)
        {
            Pulse(_activationPulse);
            Color color = GetCategoryColor(card);
            Vector2 center = _spawner.ToLocal(_eventBox);

            _fx.Flash(color, _activationFlashOpacity, _activationFlashDuration);
            _fx.Punch(_activationPunch);
            _spawner.Ring(center, color, _activationRingSize, _activationRingDuration);
            _spawner.Ring(center, Color.white, _activationInnerRingSize, _activationInnerRingDuration);
            _spawner.Burst(center, new[] { _enemyHitColor, Color.white, color }, _activationBurstCount, _activationBurstSpeed);
            _spawner.Popup(center, _activatedText, color, _activationFontSize);

            RectTransform slot = null;
            if (isPlayer && _handView != null)
            {
                slot = _handView.GetSlotRect(card);
            }
            else if (!isPlayer && _enemyView != null)
            {
                slot = _enemyView.GetSlotRect(card);
            }

            if (slot != null)
            {
                Vector2 position = _spawner.ToLocal(slot);
                _spawner.Ring(position, color, _slotRingSize, _slotRingDuration);
                if (!isPlayer)
                {
                    _spawner.Popup(position, string.Format(_enemyActivatedFormat, card.DisplayName), color, _slotPopupFontSize);
                }
            }
        }

        private Color GetCategoryColor(EffectCardData card)
        {
            var category = card.Category;
            if (category.HasFlag(EffectCategory.Attack))
            {
                return _attackColor;
            }

            if (category.HasFlag(EffectCategory.Disrupt))
            {
                return _disruptColor;
            }

            if (category.HasFlag(EffectCategory.Defense) || category.HasFlag(EffectCategory.Utility))
            {
                return _supportColor;
            }

            return category.HasFlag(EffectCategory.Wild) ? _wildColor : _otherColor;
        }

        // 영웅·전설을 얻었을 때의 축하: 번쩍임, 흔들림, 겹친 링, 파편, 전설은 색종이까지.
        private void PlayRarityGet(EffectCardData card)
        {
            bool legend = card.Grade == EffectGrade.Legend;
            Color accent = legend ? _legendColor : _epicColor;

            _fx.Flash(accent, _getFlashOpacity, _getFlashDuration);
            _fx.Shake(legend ? _legendGetShake : _epicGetShake);
            _spawner.Ring(Vector2.zero, accent, _getRingSize, _getRingDuration);
            Schedule(_getInnerRingDelay, () => _spawner.Ring(Vector2.zero, Color.white, _getInnerRingSize, _getInnerRingDuration));
            _spawner.Burst(Vector2.zero, new[] { accent, Color.white, _enemyHitColor }, legend ? _legendGetBurstCount : _epicGetBurstCount, _getBurstSpeed);

            if (legend)
            {
                _spawner.Confetti(_legendGetConfettiCount);
                Schedule(_legendGetSecondConfettiDelay, () => _spawner.Confetti(_legendGetSecondConfettiCount));
            }

            // 카드가 가운데에 뒤집히며 뜨는 것이 프로토타입 획득 화면의 중심이다. 오브젝트가 없으면 나머지만 보여 준다.
            if (_getCard != null)
            {
                _getCard.Show(card, _coordinator != null ? _coordinator.ActiveUsableRounds : _getCardActiveRounds);
            }

            _fx.ShowBanner(legend ? _legendGetText : _epicGetText, string.Format(_getSubFormat, card.DisplayName), accent, legend);
        }

        // 고른 카드가 손패 슬롯으로 날아와 앉고, 앉을 즈음에 금색 링과 불꽃이 터진다.
        private void OnCardEquipped(Vector3 worldFrom, EffectCardData card)
        {
            if (_handView == null)
            {
                return;
            }

            var slot = _handView.GetSlotView(card);
            if (slot == null)
            {
                return;
            }

            slot.PlayArrive(worldFrom);
            var rect = (RectTransform)slot.transform;
            Schedule(_equipBurstDelay, () =>
            {
                Vector2 position = _spawner.ToLocal(rect);
                _spawner.Ring(position, _equipColor, _equipRingSize, _equipRingDuration);
                _spawner.Burst(position, _equipBurstColors, _equipBurstCount, _equipBurstSpeed);
            });
        }

        // 드래프트 카드가 공개되는 순간 그 자리에서 터진다. 일반·희귀는 조용히 넘어가고 영웅·전설만 연출한다.
        private void OnCardRevealed(RectTransform slot, EffectCardData card)
        {
            if (card.Grade < EffectGrade.Epic)
            {
                return;
            }

            bool legend = card.Grade == EffectGrade.Legend;
            Color accent = legend ? _legendColor : _epicColor;
            Vector2 position = _spawner.ToLocal(slot);

            _fx.Flash(legend ? _legendRevealFlashColor : _epicRevealFlashColor,
                legend ? _legendRevealFlashOpacity : _epicRevealFlashOpacity,
                legend ? _legendRevealFlashDuration : _epicRevealFlashDuration);
            _spawner.Ring(position, accent, legend ? _legendRevealRingSize : _epicRevealRingSize,
                legend ? _legendRevealRingDuration : _epicRevealRingDuration);
            _spawner.Burst(position, new[] { accent, Color.white, _legendRevealFlashColor },
                legend ? _legendRevealBurstCount : _epicRevealBurstCount,
                legend ? _legendRevealBurstSpeed : _epicRevealBurstSpeed);

            if (legend)
            {
                _spawner.Star(position, accent, _legendRevealStarSize);
                _fx.Shake(_legendRevealShake);
            }
        }

        private void PlayHealed(bool isPlayer, int amount)
        {
            Vector2 position = _spawner.ToLocal(isPlayer ? _playerHud : _enemyHud);
            RefreshMood();
            _spawner.Popup(position, string.Format(_healFormat, amount), _healColor, _supportFontSize);
            _spawner.Sparkle(isPlayer ? _playerHud : _enemyHud, _healSparkleColor, _healSparkleCount);
        }

        private void PlayShield(bool isPlayer, int amount, string format)
        {
            Vector2 position = _spawner.ToLocal(isPlayer ? _playerHud : _enemyHud);
            _spawner.Popup(position, string.Format(format, amount), _shieldColor, _supportFontSize);
            _spawner.Bubble(isPlayer ? _playerHud : _enemyHud, _shieldColor);
        }
    }
}
