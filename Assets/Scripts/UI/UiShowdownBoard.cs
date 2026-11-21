using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 정산 때 가운데 이벤트 박스 위쪽을 덮는 점수판(프로토타입의 쇼다운). 상대는 왼쪽, 나는 오른쪽에 카드 5장과 큰 점수가 놓이고 가운데에 VS가 있다.
    // 카드는 뒷면으로 시작해 상대 5장, 내 5장 순서로 하나씩 뒤집힌다. 점수가 올라간 뒤 서로 부딪히고,
    // 승자는 금색으로 커지며 패자는 흔들리다 가라앉는다. 씬에 오브젝트를 두지 않으려고 감독이 코드로 만들고, 움직임은 감독의 Tick이 돌린다.
    public class UiShowdownBoard : MonoBehaviour
    {
        private const int CardsPerSide = 5;

        private sealed class Side
        {
            public RectTransform Rect;
            public Text Score;
            public Text Name;
            public Text Detail;
            public Image[] Cards = new Image[CardsPerSide];
            public Sprite[] FaceSprites = new Sprite[CardsPerSide];
            public float[] FlipClocks = new float[CardsPerSide];
            public float[] HopClocks = new float[CardsPerSide];
            public Vector2[] CardRests = new Vector2[CardsPerSide];
            public Color BaseColor;
            public Vector2 Rest;
            public int Target;
            public float CountClock = -1f;
            public float CountSeconds;
            public float PopClock = -1f;
            public float ClashClock = -1f;
            public Vector2 ClashDelta;
            public float ResultClock = -1f;
            public bool Won;
            public float DetailClock = -1f;
        }

        [SerializeField] private int _scoreFontSize = 76;
        [SerializeField] private int _nameFontSize = 24;
        [SerializeField] private int _detailFontSize = 24;
        [SerializeField] private int _versusFontSize = 44;
        [SerializeField] private float _sideOffsetX = 270f;
        [SerializeField] private float _nameY = 100f;
        [SerializeField] private float _cardY = 40f;
        [SerializeField] private float _scoreY = -44f;
        [SerializeField] private float _detailY = -94f;
        [SerializeField] private float _versusY = 20f;
        [SerializeField] private Vector2 _cardSize = new Vector2(57f, 80f);
        [SerializeField] private float _cardGap = 8f;
        [SerializeField] private Color _myColor = new Color(0.553f, 0.722f, 1f, 1f);
        [SerializeField] private Color _enemyColor = new Color(1f, 0.604f, 0.518f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.72f, 0.67f, 1f);
        [SerializeField] private Color _winColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _versusColor = new Color(0.9f, 0.64f, 0.24f, 1f);
        [SerializeField] private float _introSeconds = 0.35f;
        [SerializeField] private float _introRise = 16f;
        [SerializeField] private float _openStagger = 0.12f;
        [SerializeField] private float _flipHalfSeconds = 0.225f;
        [SerializeField] private float _hopHeight = 21f;
        [SerializeField] private float _hopSeconds = 0.38f;
        [SerializeField] private float _hopStagger = 0.06f;
        [SerializeField] private float _detailSeconds = 0.4f;
        [SerializeField] private float _detailRise = 11f;
        [SerializeField] private float _countPulse = 0.25f;
        [SerializeField] private float _popStartScale = 1.6f;
        [SerializeField] private float _popSeconds = 0.3f;
        [SerializeField] private float _clashSeconds = 0.26f;
        [SerializeField] private float _clashPullX = 0.8f;
        [SerializeField] private float _clashPullY = 0.5f;
        [SerializeField] private float _clashScale = 1.3f;
        [SerializeField] private float _winSeconds = 0.7f;
        [SerializeField] private float _winStartScale = 1.8f;
        [SerializeField] private float _winEndScale = 1.25f;
        [SerializeField] private float _loseSeconds = 0.5f;
        [SerializeField] private Vector2 _loseShake = new Vector2(10.7f, 13.3f);
        [SerializeField] private float _loseAlpha = 0.35f;
        [SerializeField] private float _panelBumpSeconds = 0.42f;
        [SerializeField] private float _panelBumpScale = 1.03f;

        private RectTransform _panel;
        private CanvasGroup _group;
        private Outline _glow;
        private RectTransform _versus;
        private Sprite _backSprite;
        private Side _me;
        private Side _enemy;
        private float _introClock = -1f;
        private float _bumpClock = -1f;
        private Vector2 _panelRest;

        public bool IsShown => gameObject.activeSelf;
        public RectTransform VersusRect => _versus;
        public RectTransform MyScoreRect => _me.Score.rectTransform;
        public RectTransform EnemyScoreRect => _enemy.Score.rectTransform;
        public float ClashSeconds => _clashSeconds;
        public float IntroSeconds => _introSeconds;

        // 카드 10장이 하나씩 뒤집히는 데 걸리는 시간(시작 간격 기준). 정산 연출이 공개가 끝난 뒤에 이어지도록 맞추는 데 쓴다.
        public float RevealSeconds => _openStagger * CardsPerSide * 2f;

        // 한 장이 뒤집히기 시작하는 순간 알린다. 연출 쪽이 그 자리에서 반짝임을 터뜨리는 데 쓴다.
        public event Action<RectTransform> CardOpened;

        public static UiShowdownBoard Create(RectTransform parent, Font font, Vector2 size, Vector2 position, Color panelColor)
        {
            var go = new GameObject("ShowdownBoard", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var board = go.AddComponent<UiShowdownBoard>();
            board.Build(font, size, position, panelColor);
            go.SetActive(false);
            return board;
        }

        private void Build(Font font, Vector2 size, Vector2 position, Color panelColor)
        {
            _panel = (RectTransform)transform;
            _panel.anchorMin = _panel.anchorMax = _panel.pivot = new Vector2(0.5f, 0.5f);
            _panel.anchoredPosition = position;
            _panel.sizeDelta = size;
            _panelRest = position;

            var background = gameObject.AddComponent<Image>();
            background.color = panelColor;
            background.raycastTarget = false;
            _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _glow = gameObject.AddComponent<Outline>();
            _glow.effectDistance = new Vector2(6f, 6f);
            _glow.effectColor = Color.clear;

            _enemy = BuildSide(font, -_sideOffsetX, _enemyColor);
            _me = BuildSide(font, _sideOffsetX, _myColor);
            var versusText = AddText("Versus", _panel, font, new Vector2(0f, _versusY), new Vector2(160f, 80f), _versusFontSize, _versusColor, FontStyle.Bold);
            versusText.text = "VS";
            _versus = versusText.rectTransform;
        }

        private Side BuildSide(Font font, float x, Color color)
        {
            var side = new Side { BaseColor = color };
            side.Name = AddText("Name", _panel, font, new Vector2(x, _nameY), new Vector2(360f, 36f), _nameFontSize, _mutedColor, FontStyle.Bold);
            side.Score = AddText("Score", _panel, font, new Vector2(x, _scoreY), new Vector2(360f, 110f), _scoreFontSize, color, FontStyle.Bold);
            side.Detail = AddText("Detail", _panel, font, new Vector2(x, _detailY), new Vector2(460f, 36f), _detailFontSize, _mutedColor, FontStyle.Normal);
            side.Rect = side.Score.rectTransform;
            side.Rest = side.Rect.anchoredPosition;

            float rowWidth = CardsPerSide * _cardSize.x + (CardsPerSide - 1) * _cardGap;
            float startX = x - rowWidth / 2f + _cardSize.x / 2f;
            for (int i = 0; i < CardsPerSide; i++)
            {
                var go = new GameObject("Card" + i, typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(_panel, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = _cardSize;
                side.CardRests[i] = new Vector2(startX + i * (_cardSize.x + _cardGap), _cardY);
                rect.anchoredPosition = side.CardRests[i];
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                side.Cards[i] = image;
            }

            return side;
        }

        // 정산이 시작될 때 이름과 카드(내가 낸 5장, 상대가 낸 5장)를 정하고, 카드는 뒷면으로 둔 채 아래에서 떠오르며 나타난다.
        public void Show(string myName, string enemyName, IReadOnlyList<PokerCardData> myCards, IReadOnlyList<PokerCardData> enemyCards, Sprite backSprite)
        {
            gameObject.SetActive(true);
            _backSprite = backSprite;
            _me.Name.text = myName;
            _enemy.Name.text = enemyName;
            ResetSide(_me, myCards);
            ResetSide(_enemy, enemyCards);
            _introClock = 0f;
            _bumpClock = -1f;
            Apply();
        }

        public void Hide()
        {
            _introClock = -1f;
            gameObject.SetActive(false);
        }

        // 상대 5장, 내 5장 순서로 하나씩 뒤집는다(프로토타입의 sd-c open).
        public void OpenCards()
        {
            int order = 0;
            for (int i = 0; i < CardsPerSide; i++)
            {
                _enemy.FlipClocks[i] = -order * _openStagger;
                order++;
            }

            for (int i = 0; i < CardsPerSide; i++)
            {
                _me.FlipClocks[i] = -order * _openStagger;
                order++;
            }
        }

        // 족보 배너와 함께 내 카드 5장이 왼쪽부터 차례로 한 번씩 튄다.
        public void HopMine()
        {
            for (int i = 0; i < CardsPerSide; i++)
            {
                _me.HopClocks[i] = -i * _hopStagger;
            }
        }

        // 족보 이름 같은 설명 줄이 아래에서 올라오며 나타난다(프로토타입의 sd-f.show).
        public void ShowDetails(string myText, string enemyText)
        {
            _me.Detail.text = myText;
            _enemy.Detail.text = enemyText;
            _me.Detail.color = Color.clear;
            _enemy.Detail.color = Color.clear;
            _me.DetailClock = 0f;
            _enemy.DetailClock = 0f;
        }

        // 두 점수가 0에서 시작해 빠르게 올라가다 천천히 멈춘다.
        public void CountTo(int myScore, int enemyScore, float seconds)
        {
            StartCount(_me, myScore, seconds);
            StartCount(_enemy, enemyScore, seconds);
        }

        // 치명타 등으로 숫자가 바로 바뀔 때. 새 값이 커졌다 돌아오며 튄다.
        public void SetScore(bool isMine, int score)
        {
            var side = isMine ? _me : _enemy;
            side.CountClock = -1f;
            side.Target = score;
            side.Score.text = score.ToString();
            side.PopClock = 0f;
        }

        // 두 숫자가 가운데 VS 쪽으로 달려들었다가 제자리로 돌아온다. 승자가 있으면 그 뒤 승패 모습이 이어진다.
        public void Clash(bool hasWinner, bool playerWon)
        {
            StartClash(_me, hasWinner && playerWon);
            StartClash(_enemy, hasWinner && !playerWon);
            _bumpClock = 0f;
        }

        public void Tick(float dt)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            if (_introClock >= 0f)
            {
                _introClock += dt;
                if (_introClock >= _introSeconds)
                {
                    _introClock = -1f;
                }
            }

            if (_bumpClock >= 0f)
            {
                _bumpClock += dt;
                if (_bumpClock >= _panelBumpSeconds)
                {
                    _bumpClock = -1f;
                }
            }

            TickSide(_me, dt);
            TickSide(_enemy, dt);
            Apply();
        }

        private void StartCount(Side side, int target, float seconds)
        {
            side.Target = target;
            side.CountSeconds = seconds;
            side.CountClock = 0f;
            side.PopClock = -1f;
        }

        // 무승부는 양쪽 모두 won이 false라서 둘 다 패자처럼 가라앉는다(프로토타입).
        private void StartClash(Side side, bool won)
        {
            Vector2 toVersus = _versus.anchoredPosition - side.Rest;
            side.ClashDelta = new Vector2(toVersus.x * _clashPullX, toVersus.y * _clashPullY);
            side.ClashClock = 0f;
            side.ResultClock = -1f;
            side.Won = won;
        }

        private void ResetSide(Side side, IReadOnlyList<PokerCardData> cards)
        {
            side.CountClock = -1f;
            side.PopClock = -1f;
            side.ClashClock = -1f;
            side.ResultClock = -1f;
            side.DetailClock = -1f;
            side.Target = 0;
            side.Score.text = "0";
            side.Score.color = side.BaseColor;
            side.Detail.text = string.Empty;
            side.Rect.anchoredPosition = side.Rest;
            side.Rect.localScale = Vector3.one;
            side.Rect.localRotation = Quaternion.identity;

            for (int i = 0; i < CardsPerSide; i++)
            {
                side.FlipClocks[i] = float.NaN;
                side.HopClocks[i] = float.PositiveInfinity;
                side.FaceSprites[i] = cards != null && i < cards.Count ? cards[i].Sprite : null;
                side.Cards[i].sprite = _backSprite;
                side.Cards[i].rectTransform.localScale = Vector3.one;
                side.Cards[i].rectTransform.anchoredPosition = side.CardRests[i];
            }
        }

        private void TickSide(Side side, float dt)
        {
            if (side.CountClock >= 0f)
            {
                side.CountClock += dt;
                float k = Mathf.Clamp01(side.CountClock / Mathf.Max(0.0001f, side.CountSeconds));
                float eased = 1f - Mathf.Pow(1f - k, 3f);
                side.Score.text = Mathf.RoundToInt(side.Target * eased).ToString();
                if (k >= 1f)
                {
                    side.CountClock = -1f;
                    side.Score.text = side.Target.ToString();
                    side.PopClock = 0f;
                }
            }

            if (side.PopClock >= 0f)
            {
                side.PopClock += dt;
                if (side.PopClock >= _popSeconds)
                {
                    side.PopClock = -1f;
                }
            }

            if (side.DetailClock >= 0f)
            {
                side.DetailClock += dt;
                if (side.DetailClock >= _detailSeconds)
                {
                    side.DetailClock = -1f;
                    side.Detail.color = _mutedColor;
                    side.Detail.rectTransform.anchoredPosition = new Vector2(side.Rest.x, _detailY);
                }
                else
                {
                    float k = side.DetailClock / _detailSeconds;
                    side.Detail.color = new Color(_mutedColor.r, _mutedColor.g, _mutedColor.b, k);
                    side.Detail.rectTransform.anchoredPosition = new Vector2(side.Rest.x, _detailY - _detailRise * (1f - k));
                }
            }

            if (side.ClashClock >= 0f)
            {
                side.ClashClock += dt;
                if (side.ClashClock >= _clashSeconds)
                {
                    side.ClashClock = -1f;
                    side.ResultClock = 0f;
                }
            }

            if (side.ResultClock >= 0f)
            {
                side.ResultClock += dt;
            }

            TickCards(side, dt);
        }

        // 카드마다: 좁아지며 뒷면이 사라지고, 가장 좁을 때 앞면으로 바뀐 뒤 다시 펴진다. 튀기는 별도로 위아래로만 움직인다.
        private void TickCards(Side side, float dt)
        {
            for (int i = 0; i < CardsPerSide; i++)
            {
                var rect = side.Cards[i].rectTransform;
                float scaleX = 1f;
                if (!float.IsNaN(side.FlipClocks[i]))
                {
                    float before = side.FlipClocks[i];
                    side.FlipClocks[i] += dt;
                    float clock = side.FlipClocks[i];
                    if (clock > 0f)
                    {
                        if (before <= 0f)
                        {
                            CardOpened?.Invoke(rect);
                        }

                        if (before < _flipHalfSeconds && clock >= _flipHalfSeconds)
                        {
                            side.Cards[i].sprite = side.FaceSprites[i];
                        }

                        if (clock < _flipHalfSeconds)
                        {
                            scaleX = Mathf.Cos(clock / _flipHalfSeconds * Mathf.PI * 0.5f);
                        }
                        else if (clock < _flipHalfSeconds * 2f)
                        {
                            scaleX = Mathf.Sin((clock - _flipHalfSeconds) / _flipHalfSeconds * Mathf.PI * 0.5f);
                        }
                        else
                        {
                            side.FlipClocks[i] = float.NaN;
                        }
                    }
                }

                float hop = 0f;
                if (!float.IsPositiveInfinity(side.HopClocks[i]))
                {
                    side.HopClocks[i] += dt;
                    if (side.HopClocks[i] >= _hopSeconds)
                    {
                        side.HopClocks[i] = float.PositiveInfinity;
                    }
                    else if (side.HopClocks[i] > 0f)
                    {
                        float k = side.HopClocks[i] / _hopSeconds;
                        float ease = k < 0.5f
                            ? UiEase.CubicBezier(0.2f, 1.6f, 0.4f, 1f, k * 2f)
                            : 1f - UiEase.CubicBezier(0.2f, 1.6f, 0.4f, 1f, (k - 0.5f) * 2f);
                        hop = _hopHeight * ease;
                    }
                }

                rect.localScale = new Vector3(scaleX, 1f, 1f);
                rect.anchoredPosition = side.CardRests[i] + Vector2.up * hop;
            }
        }

        // 그려지는 모든 값을 한곳에서 정한다. 상태마다 위치·크기·기울기·색이 겹치므로 우선순위를 코드로 고정한다.
        private void Apply()
        {
            float intro = _introClock >= 0f ? Mathf.Clamp01(_introClock / _introSeconds) : 1f;
            _group.alpha = intro;
            float bump = 0f;
            if (_bumpClock >= 0f)
            {
                float k = _bumpClock / _panelBumpSeconds;
                bump = Mathf.Sin(k * Mathf.PI);
            }

            _panel.anchoredPosition = _panelRest + Vector2.down * (_introRise * (1f - intro));
            _panel.localScale = Vector3.one * (1f + (_panelBumpScale - 1f) * bump);
            _glow.effectColor = new Color(_winColor.r, _winColor.g, _winColor.b, bump);

            ApplySide(_me);
            ApplySide(_enemy);
        }

        private void ApplySide(Side side)
        {
            Vector2 position = side.Rest;
            float scale = 1f;
            float rotation = 0f;
            Color color = side.BaseColor;

            if (side.CountClock >= 0f)
            {
                float k = Mathf.Clamp01(side.CountClock / Mathf.Max(0.0001f, side.CountSeconds));
                scale = 1f + _countPulse * (1f - k);
            }

            if (side.PopClock >= 0f)
            {
                float k = side.PopClock / _popSeconds;
                float eased = UiEase.CubicBezier(0.2f, 1.8f, 0.4f, 1f, k);
                scale = Mathf.LerpUnclamped(_popStartScale, 1f, eased);
            }

            if (side.ClashClock >= 0f)
            {
                float k = side.ClashClock / _clashSeconds;
                float eased = UiEase.CubicBezier(0.6f, 0f, 0.9f, 0.4f, k);
                position += side.ClashDelta * eased;
                scale = Mathf.Lerp(1f, _clashScale, eased);
            }
            else if (side.ResultClock >= 0f)
            {
                if (side.Won)
                {
                    float k = Mathf.Clamp01(side.ResultClock / _winSeconds);
                    scale = Mathf.LerpUnclamped(_winStartScale, _winEndScale, UiEase.CubicBezier(0.2f, 1.8f, 0.4f, 1f, k));
                    color = _winColor;
                }
                else
                {
                    float k = Mathf.Clamp01(side.ResultClock / _loseSeconds);
                    ApplyLose(k, ref position, ref rotation, ref color, side.Rest);
                }
            }

            side.Rect.anchoredPosition = position;
            side.Rect.localScale = Vector3.one * scale;
            side.Rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            side.Score.color = color;
        }

        // 프로토타입의 sdLose: 좌우로 두 번 흔들리고, 아래로 가라앉으며 기울고 흐려진다.
        private void ApplyLose(float k, ref Vector2 position, ref float rotation, ref Color color, Vector2 rest)
        {
            float x;
            float rot;
            float down = 0f;
            float fade;
            if (k < 0.2f)
            {
                float t = k / 0.2f;
                x = -_loseShake.x * t;
                rot = -6f * t;
                fade = 0f;
            }
            else if (k < 0.4f)
            {
                float t = (k - 0.2f) / 0.2f;
                x = Mathf.Lerp(-_loseShake.x, _loseShake.x, t);
                rot = Mathf.Lerp(-6f, 5f, t);
                fade = 0f;
            }
            else
            {
                float t = (k - 0.4f) / 0.6f;
                x = Mathf.Lerp(_loseShake.x, 0f, t);
                rot = Mathf.Lerp(5f, -8f, t);
                down = Mathf.Lerp(0f, _loseShake.y, t);
                fade = t;
            }

            position = rest + new Vector2(x, -down);
            rotation = rot;

            // 회색으로 바래며 흐려진다(프로토타입의 grayscale + opacity).
            float gray = color.r * 0.299f + color.g * 0.587f + color.b * 0.114f;
            Color grayColor = new Color(gray, gray, gray, 1f);
            color = Color.Lerp(color, grayColor, fade);
            color.a = Mathf.Lerp(1f, _loseAlpha, fade);
        }

        private static Text AddText(string name, RectTransform parent, Font font, Vector2 position, Vector2 size, int fontSize, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
