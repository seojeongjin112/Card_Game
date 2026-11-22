using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Poker_Card
{
    // 시작 화면(프로토타입의 홈 화면). 큰 대표 카드와 DUEL 패널, 난이도를 고르는 연습 듀얼 줄, 게임 방법 창이 있다.
    // 씬에는 이 컴포넌트가 붙은 빈 오브젝트 하나와 카메라만 두고, 화면은 모두 코드가 만든다.
    // 게임 시작을 누르면 고른 난이도를 GameSession에 담아 전투 씬으로 넘어간다.
    public class TitleScreen : MonoBehaviour
    {
        private const int MiniCardCount = 5;
        private const float MiniCardWidth = 100f;
        private const float MiniCardGap = 12f;
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;
        private const float BigButtonDepth = 8f;
        private const float BigButtonWidth = 300f;

        [Header("데이터와 씬")]
        [SerializeField] private EffectCardDatabase _effectDatabase;
        [SerializeField] private PokerCardDatabase _pokerDatabase;
        [SerializeField] private string _gameSceneName = "Card";

        [Header("문구")]
        [SerializeField] private string _logoWord = "RUNE DUEL";
        [SerializeField] private string _logoSub = "룬 결투";
        [SerializeField] private string _duelWord = "DUEL";
        [SerializeField] private string _heroLabel = "MY ACE";
        [SerializeField] private string _heroHeading = "이번 판을 끝내는 한 장";
        [SerializeField] private string _heroDescription = "포커 족보와 효과 카드로 상대의 HP를 먼저 0으로 만드세요.";
        [SerializeField] private string _deckFormat = "효과 덱 · {0} 중심 · {1}장";
        [SerializeField] private string[] _suitNames = { "스페이드", "하트", "다이아", "클로버" };
        [SerializeField] private string _panelTitle = "1VS1 듀얼";
        [SerializeField] private string _panelNote = "HP를 먼저 0으로";
        [SerializeField] private string _startLabel = "게임 시작";
        [SerializeField] private string _startSub = "START";
        [SerializeField] private string _quitLabel = "종료";
        [SerializeField] private string _quitSub = "QUIT";
        [SerializeField] private string _soloSmall = "SOLO · VS AI";
        [SerializeField] private string _soloTitle = "연습 듀얼";
        [SerializeField] private string _howSmall = "HOW TO PLAY";
        [SerializeField] private string _howTitle = "게임 방법";
        [SerializeField] private string _howPill = "처음이라면";
        [SerializeField] private string _recordSmall = "최근 기록";
        [SerializeField] private string _recordTitle = "기록 없음";
        [SerializeField] private string[] _difficultyNames = { "쉬움", "보통", "어려움" };
        [SerializeField] private string _closeLabel = "닫기";
        [SerializeField, TextArea(6, 14)]
        private string _howToBody =
            "1. 라운드마다 효과 카드 3장 중 1장을 골라 장착합니다.\n" +
            "2. 포커 카드 7장 중 5장을 골라 족보를 만들고 제출합니다.\n" +
            "3. 액티브 효과 카드를 써서 점수를 올리거나 방어를 준비합니다.\n" +
            "4. 최종 점수 = (족보 점수 + 보너스) × 배율. 점수가 높은 쪽이 이깁니다.\n" +
            "5. 두 점수의 차이만큼 진 쪽의 HP가 줄고, HP가 0이 되면 전투가 끝납니다.\n" +
            "6. 리롤은 라운드마다 정해진 횟수만 쓸 수 있습니다.";

        [Header("색")]
        [SerializeField] private Color _backInner = new Color(0.07f, 0.27f, 0.2f, 1f);
        [SerializeField] private Color _backOuter = new Color(0.01f, 0.06f, 0.045f, 1f);
        [SerializeField] private Color _barColor = new Color(0.008f, 0.045f, 0.035f, 0.94f);
        [SerializeField] private Color _lineColor = new Color(0.18f, 0.34f, 0.29f, 1f);
        [SerializeField] private Color _panelColor = new Color(0.03f, 0.125f, 0.095f, 1f);
        [SerializeField] private Color _borderColor = new Color(0.9f, 0.95f, 0.92f, 1f);
        [SerializeField] private Color _rowColor = new Color(0.055f, 0.19f, 0.14f, 1f);
        [SerializeField] private Color _textColor = new Color(0.957f, 0.945f, 0.91f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.76f, 0.7f, 1f);
        [SerializeField] private Color _goldColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _logoColor = new Color(0.56f, 0.63f, 0.6f, 1f);
        [SerializeField] private Color _duelColor = new Color(1f, 0.8f, 0.32f, 1f);
        [SerializeField] private Color _duelShadow = new Color(0.55f, 0.28f, 0.05f, 1f);
        [SerializeField] private Color _redButton = new Color(1f, 0.353f, 0.235f, 1f);
        [SerializeField] private Color _blueButton = new Color(0.165f, 0.42f, 0.741f, 1f);
        [SerializeField] private Color _grayButton = new Color(0.247f, 0.298f, 0.278f, 1f);
        [SerializeField] private Color _heroShadow = new Color(0.9f, 0.62f, 0.18f, 1f);
        [SerializeField] private Color[] _difficultyColors =
        {
            new Color(0.24f, 0.78f, 0.55f, 1f),
            new Color(0.18f, 0.64f, 0.42f, 1f),
            new Color(1f, 0.353f, 0.235f, 1f)
        };

        [Header("움직임")]
        [SerializeField] private float _fadeSeconds = 0.5f;
        [SerializeField] private float _heroBobHeight = 10f;
        [SerializeField] private float _heroBobPeriod = 4.5f;
        [SerializeField] private float _heroTilt = 5f;
        [SerializeField] private float _introSeconds = 0.6f;
        [SerializeField] private float _introRise = 24f;

        private Font _font;
        private RectTransform _root;
        private UiScreenFade _fadeScreen;
        private CanvasGroup _content;
        private RectTransform _stage;
        private RectTransform _hero;
        private Vector2 _heroRest;
        private Suit _topSuit = Suit.Spade;
        private Text _difficultyText;
        private Image _difficultyPill;
        private GameObject _howToWindow;
        private int _difficultyIndex = 1;
        private float _clock;

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (GameSession.HasDifficulty)
            {
                _difficultyIndex = (int)GameSession.Difficulty;
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildBackground();
            BuildContent();
            BuildHowToWindow();
            BuildFade();
            RefreshDifficulty();
        }

        private void Update()
        {
            _clock += Time.unscaledDeltaTime;

            if (_hero != null)
            {
                float bob = Mathf.Sin(_clock / _heroBobPeriod * Mathf.PI * 2f) * _heroBobHeight;
                _hero.anchoredPosition = _heroRest + Vector2.up * bob;
            }

            if (_content != null)
            {
                float fit = Mathf.Min(_root.rect.width / ReferenceWidth, _root.rect.height / ReferenceHeight);
                _stage.localScale = Vector3.one * fit;

                float intro = Mathf.Clamp01(_clock / _introSeconds);
                _content.alpha = intro;
                _stage.localPosition = Vector3.down * (_introRise * fit * (1f - intro));
            }

        }

        private void StartGame()
        {
            if (_fadeScreen.IsLeaving)
            {
                return;
            }

            GameSession.Difficulty = (EnemyDifficulty)_difficultyIndex;
            _fadeScreen.GoTo(_gameSceneName);
        }

        // 아래 탭에서 다른 화면을 고르면 그 씬으로 넘어간다.
        private void OnTabSelected(string id)
        {
            _fadeScreen.GoTo(UiLobbyChrome.GetSceneName(id));
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CycleDifficulty()
        {
            _difficultyIndex = (_difficultyIndex + 1) % _difficultyNames.Length;
            RefreshDifficulty();
        }

        private void RefreshDifficulty()
        {
            _difficultyText.text = _difficultyNames[_difficultyIndex];
            _difficultyPill.color = _difficultyColors[_difficultyIndex];
        }

        // ---------- 만들기 ----------

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void BuildCanvas()
        {
            _root = UiBuild.CreateCanvas(transform, "TitleCanvas");
        }

        private void BuildBackground()
        {
            UiBuild.BuildBackdrop(_root, _font, _backInner, _backOuter);
        }

        private void BuildContent()
        {
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(_root, false);
            var contentRect = (RectTransform)contentGo.transform;

            // 화면 비율이 달라도 프로토타입처럼 1920x1080 무대가 통째로 맞춰지도록, 고정 크기 무대를 만들어 배율만 조절한다.
            Center(contentRect, new Vector2(ReferenceWidth, ReferenceHeight), Vector2.zero);
            _stage = contentRect;
            _content = contentGo.AddComponent<CanvasGroup>();

            BuildTopBar(contentRect);
            BuildHero(contentRect);
            BuildFan(contentRect);
            BuildPanel(contentRect);
            BuildRows(contentRect);
        }

        private void BuildTopBar(RectTransform parent)
        {
            UiLobbyChrome.BuildTopBar(parent, _font, _logoWord, _logoSub, null, _quitLabel, QuitGame);
            UiLobbyChrome.BuildTabBar(parent, _font, "home", OnTabSelected);
        }

        // 왼쪽의 큰 대표 카드와 그 옆의 문구, 현재 덱 미리보기 카드 5장.
        private void BuildHero(RectTransform parent)
        {
            var heroGo = new GameObject("Hero", typeof(RectTransform));
            heroGo.transform.SetParent(parent, false);
            _hero = (RectTransform)heroGo.transform;
            Place(_hero, 64f, 168f, 444f, 600f);
            _hero.pivot = new Vector2(0.5f, 0.5f);
            _hero.anchoredPosition += new Vector2(222f, -300f);
            _heroRest = _hero.anchoredPosition;
            _hero.localRotation = Quaternion.Euler(0f, 0f, _heroTilt);

            var hit = heroGo.AddComponent<Image>();
            hit.color = Color.clear;
            heroGo.AddComponent<UiCardTilt>();

            var shadow = AddImage("HeroShadow", _hero, _heroShadow);
            Center(shadow.rectTransform, new Vector2(444f, 600f), new Vector2(20f, -16f));
            shadow.raycastTarget = false;

            var face = EffectCardFace.Create(_hero, _font, new Vector2(444f, 600f));
            var data = _effectDatabase != null ? _effectDatabase.Find(Suit.Spade, Rank.Queen) : null;
            if (data != null)
            {
                string footer = data.IsActive ? "액티브 · 첫 사용부터 3라운드" : "패시브 · 상시";
                face.Show(data, "액티브", "패시브", footer, EffectCardFace.GetGradeColor(data.Grade), data.Grade == EffectGrade.Legend ? "L" : "C");
            }

            var label = AddText("HeroLabel", parent, 20, _goldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.text = _heroLabel;
            Place(label.rectTransform, 560f, 236f, 400f, 30f);

            var heading = AddText("HeroHeading", parent, 48, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            heading.text = _heroHeading;
            Place(heading.rectTransform, 560f, 272f, 560f, 70f);

            var description = AddText("HeroDescription", parent, 20, _mutedColor, FontStyle.Normal, TextAnchor.UpperLeft);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.text = _heroDescription;
            Place(description.rectTransform, 560f, 352f, 560f, 60f);

            var deck = AddText("HeroDeck", parent, 22, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            var playerDeck = _effectDatabase != null ? PlayerDeckStore.LoadEditable(_effectDatabase) : new List<EffectCardData>();
            _topSuit = GetTopSuit(playerDeck);
            deck.text = string.Format(_deckFormat, _suitNames[(int)_topSuit], playerDeck.Count);
            Place(deck.rectTransform, 560f, 432f, 560f, 32f);

            BuildMiniCards(parent);
        }

        // 스페이드 효과 카드 중 낮은 숫자부터 5장을 작은 카드로 늘어놓는다.
        private void BuildMiniCards(RectTransform parent)
        {
            if (_effectDatabase == null)
            {
                return;
            }

            var picks = new List<EffectCardData>();
            foreach (var card in PlayerDeckStore.LoadEditable(_effectDatabase))
            {
                if (card.Suit == _topSuit)
                {
                    picks.Add(card);
                }
            }

            // Ace를 1로 보고 낮은 숫자부터 세운다(프로토타입도 A·2·3·4·5 순서).
            picks.Sort((a, b) => GetLowRank(a.Rank).CompareTo(GetLowRank(b.Rank)));

            for (int i = 0; i < MiniCardCount && i < picks.Count; i++)
            {
                var card = picks[i];
                float left = 560f + i * (MiniCardWidth + MiniCardGap);
                var border = AddImage("MiniBorder", parent, new Color(0.08f, 0.1f, 0.1f, 1f));
                Place(border.rectTransform, left, 484f, MiniCardWidth, 144f);
                var paper = AddImage("MiniPaper", border.rectTransform, new Color(0.97f, 0.96f, 0.92f, 1f));
                Stretch(paper.rectTransform);
                paper.rectTransform.offsetMin = new Vector2(4f, 4f);
                paper.rectTransform.offsetMax = new Vector2(-4f, -4f);

                var code = AddText("Code", paper.rectTransform, 24, new Color(0.1f, 0.1f, 0.1f, 1f), FontStyle.Bold, TextAnchor.UpperLeft);
                code.text = EffectCardFace.GetCodeText(card);
                Place(code.rectTransform, 8f, 6f, 84f, 32f);

                var name = AddText("Name", paper.rectTransform, 17, new Color(0.15f, 0.15f, 0.15f, 1f), FontStyle.Bold, TextAnchor.UpperLeft);
                name.text = card.DisplayName;
                Place(name.rectTransform, 8f, 40f, 88f, 26f);

                var grade = AddImage("GradeBar", paper.rectTransform, EffectCardFace.GetGradeColor(card.Grade));
                Place(grade.rectTransform, 8f, 118f, 84f, 8f);
            }
        }

        // 카드 몇 장이 겹쳐서 펼쳐진 모습. 포커 카드 그림을 그대로 쓴다.
        private void BuildFan(RectTransform parent)
        {
            if (_pokerDatabase == null)
            {
                return;
            }

            var picks = new[]
            {
                new KeyValuePair<Suit, Rank>(Suit.Diamond, Rank.Ace),
                new KeyValuePair<Suit, Rank>(Suit.Heart, Rank.Seven),
                new KeyValuePair<Suit, Rank>(Suit.Heart, Rank.King),
                new KeyValuePair<Suit, Rank>(Suit.Spade, Rank.Queen)
            };
            float[] angles = { 14f, 5f, -5f, -14f };
            float[] lifts = { 0f, 14f, 14f, 0f };

            for (int i = 0; i < picks.Length; i++)
            {
                Sprite sprite = FindPokerSprite(picks[i].Key, picks[i].Value);
                if (sprite == null)
                {
                    continue;
                }

                var go = new GameObject("FanCard" + i, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(132f, 184f);
                rect.anchoredPosition = new Vector2(620f + i * 76f, -(965f - lifts[i]));
                rect.localRotation = Quaternion.Euler(0f, 0f, angles[i]);
                var image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                AddShadow(image, new Color(0f, 0f, 0f, 0.5f), new Vector2(6f, -6f));
            }
        }

        private Sprite FindPokerSprite(Suit suit, Rank rank)
        {
            foreach (var card in _pokerDatabase.Cards)
            {
                if (card.Suit == suit && card.Rank == rank)
                {
                    return card.Sprite;
                }
            }

            return null;
        }

        // 오른쪽의 DUEL 패널: 큰 로고, 소제목, 게임 시작(빨강)과 종료(파랑) 버튼.
        private void BuildPanel(RectTransform parent)
        {
            const float left = 1147f;
            const float top = 144f;
            const float width = 718f;
            const float height = 379f;

            var border = AddImage("PanelBorder", parent, _borderColor);
            Place(border.rectTransform, left, top, width, height);
            var inner = AddImage("PanelInner", border.rectTransform, _panelColor);
            Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(4f, 4f);
            inner.rectTransform.offsetMax = new Vector2(-4f, -4f);

            float x = left + 44f;
            foreach (char letter in _duelWord)
            {
                var shadowText = AddText("DuelShadow", parent, 150, _duelShadow, FontStyle.Bold, TextAnchor.MiddleCenter);
                shadowText.text = letter.ToString();
                Place(shadowText.rectTransform, x + 8f, top + 24f, 110f, 170f);

                var text = AddText("Duel", parent, 150, _duelColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                text.text = letter.ToString();
                Place(text.rectTransform, x, top + 16f, 110f, 170f);
                AddOutline(text, new Color(1f, 0.85f, 0.4f, 0.35f), new Vector2(2f, 2f));
                x += 108f;
            }

            var title = AddText("PanelTitle", parent, 32, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = _panelTitle;
            Place(title.rectTransform, left + 44f, top + 206f, 320f, 44f);

            var note = AddText("PanelNote", parent, 18, _mutedColor, FontStyle.Normal, TextAnchor.MiddleRight);
            note.text = _panelNote;
            Place(note.rectTransform, left + width - 44f - 260f, top + 214f, 260f, 30f);

            var start = AddBigButton("StartButton", parent, _redButton, _startLabel, _startSub, StartGame, left + 40f, top + 258f, BigButtonWidth, 100f);
            start.gameObject.AddComponent<UiButtonFx>();

            AddBigButton("QuitButton", parent, _blueButton, _quitLabel, _quitSub, QuitGame, left + width - 40f - BigButtonWidth, top + 258f, BigButtonWidth, 100f);
        }

        private void BuildRows(RectTransform parent)
        {
            const float left = 1147f;
            const float width = 718f;
            const float height = 112f;

            var solo = BuildRow(parent, "SoloRow", left, 548f, width, height, _difficultyColors[1], _soloSmall, _soloTitle, StartGame);
            var pillGo = new GameObject("DifficultyPill", typeof(RectTransform));
            pillGo.transform.SetParent(solo.rectTransform, false);
            var pillRect = (RectTransform)pillGo.transform;
            pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 0.5f);
            pillRect.sizeDelta = new Vector2(132f, 48f);
            pillRect.anchoredPosition = new Vector2(-92f, 0f);
            _difficultyPill = pillGo.AddComponent<Image>();
            var pillButton = pillGo.AddComponent<Button>();
            pillButton.targetGraphic = _difficultyPill;
            pillButton.onClick.AddListener(CycleDifficulty);
            _difficultyText = AddText("PillText", pillRect, 24, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_difficultyText.rectTransform);
            AddRowIcon(solo.rectTransform, "▶");

            var how = BuildRow(parent, "HowRow", left, 684f, width, height, new Color(0.9f, 0.62f, 0.18f, 1f), _howSmall, _howTitle, OpenHowTo);
            AddPill(how.rectTransform, _howPill, new Color(0.9f, 0.62f, 0.18f, 1f), 160f, -92f);
            AddRowIcon(how.rectTransform, "?");

            BuildRow(parent, "RecordRow", left, 820f, width, height, new Color(0.35f, 0.42f, 0.4f, 1f), _recordSmall, _recordTitle, null);
        }

        // 왼쪽에 색 띠가 있는 한 줄. 위에 작은 영문, 아래에 큰 제목이 놓인다.
        private Image BuildRow(RectTransform parent, string name, float left, float top, float width, float height, Color accent,
            string small, string title, UnityEngine.Events.UnityAction onClick)
        {
            var border = AddImage(name + "Border", parent, _lineColor);
            Place(border.rectTransform, left, top, width, height);
            var row = AddImage(name, border.rectTransform, _rowColor);
            Stretch(row.rectTransform);
            row.rectTransform.offsetMin = new Vector2(3f, 3f);
            row.rectTransform.offsetMax = new Vector2(-3f, -3f);

            if (onClick != null)
            {
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                button.colors = colors;
                button.onClick.AddListener(onClick);
            }
            else
            {
                row.raycastTarget = false;
            }

            var bar = AddImage("Accent", row.rectTransform, accent);
            bar.raycastTarget = false;
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(0f, 1f);
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.sizeDelta = new Vector2(10f, 0f);
            bar.rectTransform.anchoredPosition = Vector2.zero;

            var smallText = AddText("Small", row.rectTransform, 15, _mutedColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            smallText.text = small;
            PlaceInside(smallText.rectTransform, 40f, 18f, 400f, 24f);
            var titleText = AddText("Title", row.rectTransform, 32, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            titleText.text = title;
            PlaceInside(titleText.rectTransform, 40f, 46f, 440f, 48f);
            return row;
        }

        private void AddPill(RectTransform row, string label, Color color, float width, float rightOffset)
        {
            var go = new GameObject("Pill", typeof(RectTransform));
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(width, 48f);
            rect.anchoredPosition = new Vector2(rightOffset, 0f);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var text = AddText("Text", rect, 22, new Color(0.12f, 0.08f, 0.02f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform);
        }

        private void AddRowIcon(RectTransform row, string glyph)
        {
            var text = AddText("Icon", row, 36, _goldColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.text = glyph;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(60f, 60f);
            rect.anchoredPosition = new Vector2(-24f, 0f);
        }

        // ---------- 게임 방법 창 ----------

        private void BuildHowToWindow()
        {
            _howToWindow = new GameObject("HowToWindow", typeof(RectTransform));
            _howToWindow.transform.SetParent(_root, false);
            var rect = (RectTransform)_howToWindow.transform;
            Stretch(rect);
            var dim = _howToWindow.AddComponent<Image>();
            dim.color = new Color(0f, 0.02f, 0.015f, 0.78f);

            var border = AddImage("Border", rect, _borderColor);
            Center(border.rectTransform, new Vector2(1004f, 620f), Vector2.zero);
            var body = AddImage("Body", border.rectTransform, _panelColor);
            Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(4f, 4f);
            body.rectTransform.offsetMax = new Vector2(-4f, -4f);

            var title = AddText("Title", body.rectTransform, 40, _goldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = _howTitle;
            PlaceInside(title.rectTransform, 48f, 36f, 600f, 56f);

            var text = AddText("Body", body.rectTransform, 26, _textColor, FontStyle.Normal, TextAnchor.UpperLeft);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.text = _howToBody;
            text.lineSpacing = 1.25f;
            PlaceInside(text.rectTransform, 48f, 116f, 900f, 380f);

            var close = AddButton("Close", body.rectTransform, _redButton, _closeLabel, null, CloseHowTo, 26);
            var closeRect = close.rectTransform;
            closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(1f, 0f);
            closeRect.sizeDelta = new Vector2(180f, 60f);
            closeRect.anchoredPosition = new Vector2(-40f, 32f);
            close.gameObject.AddComponent<UiButtonFx>();

            _howToWindow.SetActive(false);
        }

        private void OpenHowTo()
        {
            _howToWindow.SetActive(true);
            _howToWindow.transform.SetAsLastSibling();
            _fadeScreen.BringToFront();
        }

        private void CloseHowTo()
        {
            _howToWindow.SetActive(false);
        }

        private void BuildFade()
        {
            _fadeScreen = UiScreenFade.Create(_root, _fadeSeconds);
        }

        // ---------- 요소 만들기 (공통 도구에 맡긴다) ----------

        private Image AddImage(string name, RectTransform parent, Color color)
        {
            return UiBuild.AddImage(name, parent, color);
        }

        private Text AddText(string name, RectTransform parent, int fontSize, Color color, FontStyle style, TextAnchor anchor)
        {
            return UiBuild.AddText(name, parent, _font, fontSize, color, style, anchor);
        }

        private Image AddButton(string name, RectTransform parent, Color color, string label, string sub, UnityEngine.Events.UnityAction onClick, int fontSize)
        {
            return UiBuild.AddButton(name, parent, _font, color, label, fontSize, onClick);
        }

        // 큰 버튼: 아래쪽에 어두운 띠를 깔아 눌러 들어가는 듯한 입체감을 준다. 제목 아래에 작은 영문이 붙는다.
        private Image AddBigButton(string name, RectTransform parent, Color color, string label, string sub, UnityEngine.Events.UnityAction onClick,
            float left, float top, float width, float height)
        {
            var edge = AddImage(name + "Edge", parent, Color.Lerp(color, Color.black, 0.45f));
            Place(edge.rectTransform, left, top + BigButtonDepth, width, height);
            edge.raycastTarget = false;

            var face = AddButton(name, parent, color, label, sub, onClick, 36);
            Place(face.rectTransform, left, top, width, height - BigButtonDepth);
            var text = face.GetComponentInChildren<Text>();
            text.rectTransform.offsetMin = new Vector2(0f, 24f);

            var subText = AddText("Sub", face.rectTransform, 15, new Color(1f, 1f, 1f, 0.8f), FontStyle.Bold, TextAnchor.LowerCenter);
            subText.text = sub;
            Stretch(subText.rectTransform);
            subText.rectTransform.offsetMin = new Vector2(0f, 12f);
            return face;
        }

        private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
        {
            UiBuild.AddShadow(graphic, color, distance);
        }

        private static void AddOutline(Graphic graphic, Color color, Vector2 distance)
        {
            UiBuild.AddOutline(graphic, color, distance);
        }

        private static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            UiBuild.Place(rect, left, top, width, height);
        }

        // 부모의 왼쪽 위를 기준으로 하는 자리. 부모 안에서 안쪽 요소를 놓을 때 쓴다.
        private static void PlaceInside(RectTransform rect, float left, float top, float width, float height)
        {
            UiBuild.Place(rect, left, top, width, height);
        }

        private static void Center(RectTransform rect, Vector2 size, Vector2 offset)
        {
            UiBuild.Center(rect, size, offset);
        }

        private static void Stretch(RectTransform rect)
        {
            UiBuild.Stretch(rect);
        }

        // 덱에서 장수가 가장 많은 문양. 같으면 앞선 문양을 쓴다.
        private static Suit GetTopSuit(IReadOnlyList<EffectCardData> deck)
        {
            var counts = new int[4];
            foreach (var card in deck)
            {
                counts[(int)card.Suit]++;
            }

            int best = 0;
            for (int i = 1; i < counts.Length; i++)
            {
                if (counts[i] > counts[best])
                {
                    best = i;
                }
            }

            return (Suit)best;
        }

        private static int GetLowRank(Rank rank)
        {
            return rank == Rank.Ace ? 1 : (int)rank;
        }
    }
}
