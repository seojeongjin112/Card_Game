using System;
using System.Collections.Generic;
using Core;
using Effect_Card;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Poker_Card
{
    // 덱 화면(프로토타입의 덱 탭). 왼쪽에 카드 정보와 덱 통계, 가운데에 내 메인 덱, 오른쪽에 보유 카드 목록이 있다.
    // 보유 카드를 눌러 선택하고 더블클릭(또는 버튼)으로 덱에 넣고 빼며, 바꿀 때마다 저장해서 전투가 그 덱으로 시작한다.
    // 씬에는 이 컴포넌트가 붙은 빈 오브젝트와 카메라만 두고, 화면은 모두 코드가 만든다.
    public class DeckScreen : MonoBehaviour
    {
        private const float AsideWidth = 400f;
        private const float OwnLeft = 1440f;
        private const int DeckColumns = 7;
        private static readonly Suit[] SuitOrder = { Suit.Spade, Suit.Heart, Suit.Clover, Suit.Diamond };
        private static readonly string[] SuitGlyphs = { "♠", "♥", "♦", "♣" };

        [Header("데이터")]
        [SerializeField] private EffectCardDatabase _effectDatabase;
        [SerializeField] private EffectRuleSet _ruleSet;

        [Header("문구")]
        [SerializeField] private string _logoWord = "RUNE DUEL";
        [SerializeField] private string _logoSub = "룬 결투";
        [SerializeField] private string _pageLabel = "덱";
        [SerializeField] private string _infoLabel = "카드 정보";
        [SerializeField] private string _deckTitle = "메인 덱";
        [SerializeField] private string _deckCountFormat = "{0} / {1}";
        [SerializeField] private string _limitFormat = "영웅 {0}/{1}   전설 {2}/{3}";
        [SerializeField] private string _completeText = "✓ 덱 완성";
        [SerializeField] private string _incompleteFormat = "덱 미완성 · {0}장 더 필요";
        [SerializeField] private string _overFormat = "덱 미완성 · {0}장 줄여야 함";
        [SerializeField] private string _hint = "카드를 눌러 선택하고, 더블클릭하거나 버튼으로 덱에 넣고 뺍니다";
        [SerializeField] private string _fallbackNote = "덱이 완성되지 않으면 전투는 기본 덱으로 시작합니다";
        [SerializeField] private string _defaultLabel = "기본 덱";
        [SerializeField] private string _randomLabel = "무작위 덱";
        [SerializeField] private string _clearLabel = "모두 비우기";
        [SerializeField] private string _ownTitle = "보유 카드";
        [SerializeField] private string _allLabel = "전체";
        [SerializeField] private string _removeLabel = "덱에서 빼기";
        [SerializeField] private string _addLabel = "덱에 넣기";
        [SerializeField] private string _deckTag = "덱";
        [SerializeField] private string[] _gradeNames = { "일반", "희귀", "영웅", "전설" };
        [SerializeField] private string _activeName = "액티브";
        [SerializeField] private string _passiveName = "패시브";
        [SerializeField] private string _activeFooterFormat = "액티브 · 첫 사용부터 {0}라운드";
        [SerializeField] private string _passiveFooter = "패시브 · 상시";
        [SerializeField] private string _statActive = "액티브";
        [SerializeField] private string _statPassive = "패시브";
        [SerializeField] private string _addedToast = "덱에 넣었습니다";
        [SerializeField] private string _removedToast = "덱에서 뺐습니다";
        [SerializeField] private string _defaultToast = "기본 덱으로 되돌렸습니다";
        [SerializeField] private string _randomToast = "무작위 덱을 만들었습니다";
        [SerializeField] private string _clearToast = "덱을 비웠습니다";

        [Header("색")]
        [SerializeField] private Color _backInner = new Color(0.07f, 0.27f, 0.2f, 1f);
        [SerializeField] private Color _backOuter = new Color(0.01f, 0.06f, 0.045f, 1f);
        [SerializeField] private Color _panelColor = new Color(0.015f, 0.075f, 0.058f, 0.78f);
        [SerializeField] private Color _lineColor = new Color(0.18f, 0.34f, 0.29f, 1f);
        [SerializeField] private Color _fieldColor = new Color(0.05f, 0.17f, 0.13f, 1f);
        [SerializeField] private Color _rowColor = new Color(0.045f, 0.15f, 0.115f, 1f);
        [SerializeField] private Color _rowInDeckColor = new Color(0.1f, 0.3f, 0.22f, 1f);
        [SerializeField] private Color _textColor = new Color(0.957f, 0.945f, 0.91f, 1f);
        [SerializeField] private Color _bodyColor = new Color(0.812f, 0.902f, 0.863f, 1f);
        [SerializeField] private Color _mutedColor = new Color(0.62f, 0.76f, 0.7f, 1f);
        [SerializeField] private Color _goldColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private Color _okColor = new Color(0.42f, 0.93f, 0.62f, 1f);
        [SerializeField] private Color _warnColor = new Color(1f, 0.6f, 0.3f, 1f);
        [SerializeField] private Color _inkColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color _grayButton = new Color(0.247f, 0.298f, 0.278f, 1f);
        [SerializeField] private Color _tagColor = new Color(1f, 0.353f, 0.235f, 1f);

        [Header("움직임")]
        [SerializeField] private float _fadeSeconds = 0.5f;

        private Font _font;
        private RectTransform _root;
        private RectTransform _stage;
        private UiScreenFade _fade;
        private UiToast _toast;
        private EffectDeckRules _rules;
        private List<EffectCardData> _deck;
        private EffectCardData _selected;
        private Suit? _ownFilter;

        private EffectCardFace _face;
        private Text _nameText;
        private Text _descriptionText;
        private readonly List<Image> _tagBoxes = new List<Image>();
        private readonly List<Text> _tagTexts = new List<Text>();
        private readonly List<Text> _statValues = new List<Text>();
        private Image _toggleButton;
        private Text _toggleLabel;
        private Text _toggleCount;
        private Text _countText;
        private Text _limitText;
        private Text _statusText;
        private Text _noteText;
        private RectTransform _deckGrid;
        private RectTransform _ownContent;
        private ScrollRect _ownScroll;
        private readonly List<Image> _ownChips = new List<Image>();
        private readonly List<Text> _ownChipTexts = new List<Text>();

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            _rules = new EffectDeckRules(_ruleSet);
            _deck = PlayerDeckStore.LoadEditable(_effectDatabase);

            _root = UiBuild.CreateCanvas(transform, "DeckCanvas");
            UiBuild.BuildBackdrop(_root, _font, _backInner, _backOuter);
            var stageGo = new GameObject("Stage", typeof(RectTransform));
            stageGo.transform.SetParent(_root, false);
            _stage = (RectTransform)stageGo.transform;
            UiBuild.Center(_stage, new Vector2(UiBuild.StageWidth, UiBuild.StageHeight), Vector2.zero);

            BuildAside();
            BuildMain();
            BuildOwn();
            UiLobbyChrome.BuildTopBar(_stage, _font, _logoWord, _logoSub, _pageLabel, null, null);
            UiLobbyChrome.BuildTabBar(_stage, _font, "deck", id => _fade.GoTo(UiLobbyChrome.GetSceneName(id)));
            _toast = UiToast.Create(_stage, _font, 900f, 640f, 2.2f, 0.25f);
            _fade = UiScreenFade.Create(_root, _fadeSeconds);

            _selected = _deck.Count > 0 ? _deck[0] : (_effectDatabase.Cards.Count > 0 ? _effectDatabase.Cards[0] : null);
            Refresh();
        }

        private void Update()
        {
            float fit = Mathf.Min(_root.rect.width / UiBuild.StageWidth, _root.rect.height / UiBuild.StageHeight);
            _stage.localScale = Vector3.one * fit;
        }

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

        // ---------- 만들기 ----------

        private void BuildAside()
        {
            float top = UiLobbyChrome.TopBarHeight + 3f;
            float height = UiBuild.StageHeight - UiLobbyChrome.TabBarHeight - top;
            var panel = UiBuild.AddImage("Aside", _stage, _panelColor);
            UiBuild.Place(panel.rectTransform, 0f, top, AsideWidth, height);
            panel.raycastTarget = false;
            var line = UiBuild.AddImage("AsideLine", _stage, _lineColor);
            UiBuild.Place(line.rectTransform, AsideWidth, top, 3f, height);
            line.raycastTarget = false;

            var label = UiBuild.AddText("InfoLabel", _stage, _font, 16, _mutedColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            label.text = _infoLabel;
            UiBuild.Place(label.rectTransform, 28f, 104f, 300f, 24f);

            var cardGo = new GameObject("InfoCard", typeof(RectTransform));
            cardGo.transform.SetParent(_stage, false);
            var cardRect = (RectTransform)cardGo.transform;
            UiBuild.Place(cardRect, (AsideWidth - 230f) / 2f, 134f, 230f, 312f);
            var hit = cardGo.AddComponent<Image>();
            hit.color = Color.clear;
            cardGo.AddComponent<UiCardTilt>();
            _face = EffectCardFace.Create(cardRect, _font, new Vector2(230f, 312f));

            _nameText = UiBuild.AddText("Name", _stage, _font, 34, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiBuild.Place(_nameText.rectTransform, 28f, 462f, 344f, 46f);

            _descriptionText = UiBuild.AddText("Description", _stage, _font, 20, _bodyColor, FontStyle.Normal, TextAnchor.UpperLeft);
            _descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UiBuild.Place(_descriptionText.rectTransform, 28f, 512f, 344f, 66f);

            for (int i = 0; i < 3; i++)
            {
                var box = UiBuild.AddImage("Tag" + i, _stage, _fieldColor);
                box.raycastTarget = false;
                var text = UiBuild.AddText("Text", box.rectTransform, _font, 16, _bodyColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                UiBuild.Stretch(text.rectTransform);
                _tagBoxes.Add(box);
                _tagTexts.Add(text);
            }

            // 덱 통계 6칸: 액티브·패시브 장수와 문양별 장수.
            for (int i = 0; i < 6; i++)
            {
                float left = 28f + (i % 3) * 120f;
                float cellTop = 628f + (i / 3) * 70f;
                var box = UiBuild.AddImage("Stat" + i, _stage, _fieldColor);
                UiBuild.Place(box.rectTransform, left, cellTop, 112f, 62f);
                box.raycastTarget = false;
                var statLabel = UiBuild.AddText("Label", box.rectTransform, _font, 15, _mutedColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                UiBuild.Place(statLabel.rectTransform, 0f, 6f, 112f, 22f);
                statLabel.text = i == 0 ? _statActive : i == 1 ? _statPassive : SuitGlyphs[SuitIndexForStat(i)];
                var value = UiBuild.AddText("Value", box.rectTransform, _font, 26, _textColor, FontStyle.Bold, TextAnchor.MiddleCenter);
                UiBuild.Place(value.rectTransform, 0f, 26f, 112f, 34f);
                _statValues.Add(value);
            }

            _toggleButton = UiBuild.AddButton("Toggle", _stage, _font, _grayButton, string.Empty, 28, ToggleSelected);
            UiBuild.Place(_toggleButton.rectTransform, 28f, 836f, 344f, 76f);
            _toggleLabel = _toggleButton.GetComponentInChildren<Text>();
            _toggleLabel.rectTransform.offsetMin = new Vector2(0f, 18f);
            _toggleCount = UiBuild.AddText("Count", _toggleButton.rectTransform, _font, 16, new Color(1f, 1f, 1f, 0.7f), FontStyle.Bold, TextAnchor.LowerCenter);
            UiBuild.Stretch(_toggleCount.rectTransform);
            _toggleCount.rectTransform.offsetMin = new Vector2(0f, 8f);
        }

        // 통계 칸 2~5번은 문양 순서(♠ ♥ ♣ ◆)로 놓는다.
        private static int SuitIndexForStat(int cell)
        {
            Suit suit = SuitOrder[cell - 2];
            return (int)suit;
        }

        private void BuildMain()
        {
            const float left = AsideWidth + 28f;
            var title = UiBuild.AddText("DeckTitle", _stage, _font, 30, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = _deckTitle;
            UiBuild.Place(title.rectTransform, left, 104f, 180f, 44f);

            _countText = UiBuild.AddText("DeckCount", _stage, _font, 28, _goldColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            UiBuild.Place(_countText.rectTransform, left + 160f, 104f, 200f, 44f);

            _limitText = UiBuild.AddText("Limits", _stage, _font, 20, _bodyColor, FontStyle.Bold, TextAnchor.MiddleRight);
            UiBuild.Place(_limitText.rectTransform, left + 300f, 108f, 340f, 36f);
            _statusText = UiBuild.AddText("Status", _stage, _font, 20, _okColor, FontStyle.Bold, TextAnchor.MiddleRight);
            UiBuild.Place(_statusText.rectTransform, left + 640f, 108f, 344f, 36f);

            var gridGo = new GameObject("DeckGrid", typeof(RectTransform));
            gridGo.transform.SetParent(_stage, false);
            _deckGrid = (RectTransform)gridGo.transform;
            UiBuild.Place(_deckGrid, left, 164f, 984f, 490f);
            var layout = gridGo.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(128f, 112f);
            layout.spacing = new Vector2(10f, 10f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = DeckColumns;
            layout.childAlignment = TextAnchor.UpperLeft;

            var hint = UiBuild.AddText("Hint", _stage, _font, 18, _mutedColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            hint.text = _hint;
            UiBuild.Place(hint.rectTransform, left, 676f, 984f, 30f);

            float x = left;
            string[] labels = { _defaultLabel, _randomLabel, _clearLabel };
            UnityEngine.Events.UnityAction[] actions = { ResetToDefault, MakeRandom, ClearDeck };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = UiBuild.AddButton("Action" + i, _stage, _font, _grayButton, labels[i], 22, actions[i]);
                float width = button.GetComponentInChildren<Text>().preferredWidth + 48f;
                UiBuild.Place(button.rectTransform, x, 724f, width, 54f);
                x += width + 12f;
            }

            _noteText = UiBuild.AddText("Note", _stage, _font, 18, _warnColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            UiBuild.Place(_noteText.rectTransform, left, 800f, 984f, 30f);
        }

        private void BuildOwn()
        {
            float top = UiLobbyChrome.TopBarHeight + 3f;
            float height = UiBuild.StageHeight - UiLobbyChrome.TabBarHeight - top;
            var panel = UiBuild.AddImage("Own", _stage, _panelColor);
            UiBuild.Place(panel.rectTransform, OwnLeft, top, UiBuild.StageWidth - OwnLeft, height);
            panel.raycastTarget = false;
            var line = UiBuild.AddImage("OwnLine", _stage, _lineColor);
            UiBuild.Place(line.rectTransform, OwnLeft - 3f, top, 3f, height);
            line.raycastTarget = false;

            var title = UiBuild.AddText("OwnTitle", _stage, _font, 28, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.text = _ownTitle;
            UiBuild.Place(title.rectTransform, OwnLeft + 24f, 104f, 200f, 44f);

            // 문양 필터 칩: 전체와 ♠ ♥ ♣ ◆
            string[] labels = { _allLabel, "♠", "♥", "♣", "♦" };
            float x = OwnLeft + 24f;
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                var chip = UiBuild.AddButton("OwnChip" + i, _stage, _font, Color.white, labels[i], 20, () => SetOwnFilter(index));
                var text = chip.GetComponentInChildren<Text>();
                float width = Mathf.Max(56f, text.preferredWidth + 36f);
                UiBuild.Place(chip.rectTransform, x, 156f, width, 44f);
                _ownChips.Add(chip);
                _ownChipTexts.Add(text);
                x += width + 8f;
            }

            // 보유 카드 목록: 세로로 쌓이고 마우스 휠로 움직인다.
            var viewportGo = new GameObject("OwnViewport", typeof(RectTransform));
            viewportGo.transform.SetParent(_stage, false);
            var viewport = (RectTransform)viewportGo.transform;
            UiBuild.Place(viewport, OwnLeft + 24f, 216f, 432f, 748f);
            viewportGo.AddComponent<RectMask2D>();
            var hit = viewportGo.AddComponent<Image>();
            hit.color = Color.clear;

            var contentGo = new GameObject("OwnContent", typeof(RectTransform));
            contentGo.transform.SetParent(viewport, false);
            _ownContent = (RectTransform)contentGo.transform;
            _ownContent.anchorMin = new Vector2(0f, 1f);
            _ownContent.anchorMax = new Vector2(1f, 1f);
            _ownContent.pivot = new Vector2(0.5f, 1f);
            _ownContent.anchoredPosition = Vector2.zero;
            _ownContent.sizeDelta = Vector2.zero;
            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 0, 12);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var track = UiBuild.AddImage("ScrollTrack", _stage, new Color(0.04f, 0.13f, 0.1f, 0.9f));
            UiBuild.Place(track.rectTransform, OwnLeft + 24f + 432f + 6f, 216f, 10f, 748f);
            var handle = UiBuild.AddImage("ScrollHandle", track.rectTransform, new Color(0.62f, 0.76f, 0.7f, 0.75f));
            UiBuild.Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            _ownScroll = viewportGo.AddComponent<ScrollRect>();
            _ownScroll.viewport = viewport;
            _ownScroll.content = _ownContent;
            _ownScroll.horizontal = false;
            _ownScroll.vertical = true;
            _ownScroll.movementType = ScrollRect.MovementType.Clamped;
            _ownScroll.scrollSensitivity = 40f;
            _ownScroll.verticalScrollbar = scrollbar;
            _ownScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        }

        // ---------- 갱신 ----------

        // 덱이나 선택이 바뀔 때마다 화면 전체를 다시 그린다. 카드가 최대 52장이라 부담이 없다.
        private void Refresh()
        {
            RefreshOwnChips();
            RebuildDeckGrid();
            RebuildOwnList();
            RefreshHeader();
            RefreshInfo();
        }

        private void RefreshHeader()
        {
            _countText.text = string.Format(_deckCountFormat, _deck.Count, _ruleSet.DeckMax);
            int epic = CountGrade(EffectGrade.Epic);
            int legend = CountGrade(EffectGrade.Legend);
            _limitText.text = string.Format(_limitFormat, epic, _ruleSet.EpicMax, legend, _ruleSet.LegendMax);

            bool complete = PlayerDeckStore.IsComplete(_deck, _ruleSet);
            if (complete)
            {
                _statusText.text = _completeText;
                _statusText.color = _okColor;
            }
            else
            {
                int diff = _ruleSet.DeckMax - _deck.Count;
                _statusText.text = diff >= 0 ? string.Format(_incompleteFormat, diff) : string.Format(_overFormat, -diff);
                _statusText.color = _warnColor;
            }

            _noteText.text = complete ? string.Empty : _fallbackNote;

            int active = 0;
            foreach (var card in _deck)
            {
                if (card.IsActive)
                {
                    active++;
                }
            }

            _statValues[0].text = active.ToString();
            _statValues[1].text = (_deck.Count - active).ToString();
            for (int i = 0; i < SuitOrder.Length; i++)
            {
                int count = 0;
                foreach (var card in _deck)
                {
                    if (card.Suit == SuitOrder[i])
                    {
                        count++;
                    }
                }

                _statValues[2 + i].text = count.ToString();
            }
        }

        private int CountGrade(EffectGrade grade)
        {
            int count = 0;
            foreach (var card in _deck)
            {
                if (card.Grade == grade)
                {
                    count++;
                }
            }

            return count;
        }

        private void RebuildDeckGrid()
        {
            for (int i = _deckGrid.childCount - 1; i >= 0; i--)
            {
                Destroy(_deckGrid.GetChild(i).gameObject);
            }

            var sorted = new List<EffectCardData>(_deck);
            sorted.Sort(CompareCards);
            foreach (var card in sorted)
            {
                var captured = card;
                var tile = UiMiniCard.Create(_deckGrid, _font, card, new Vector2(128f, 112f), card == _selected, null);
                AddTileInput(tile, captured);
            }
        }

        // 눌러서 선택, 더블클릭으로 덱에서 빼기.
        private void AddTileInput(Image tile, EffectCardData card)
        {
            var button = tile.gameObject.AddComponent<Button>();
            button.targetGraphic = tile;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            tile.gameObject.AddComponent<UiClickRelay>().Initialize(count => OnCardClicked(card, count));
        }

        private void RebuildOwnList()
        {
            for (int i = _ownContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_ownContent.GetChild(i).gameObject);
            }

            var list = new List<EffectCardData>();
            foreach (var card in _effectDatabase.Cards)
            {
                if (_ownFilter == null || card.Suit == _ownFilter.Value)
                {
                    list.Add(card);
                }
            }

            list.Sort(CompareCards);
            foreach (var card in list)
            {
                BuildOwnRow(card);
            }
        }

        // 보유 카드 한 줄: 등급 색 띠, 코드, 이름, 등급 이름. 덱에 있으면 배경이 밝아지고 붉은 "덱" 표가 붙는다.
        private void BuildOwnRow(EffectCardData card)
        {
            bool inDeck = _deck.Contains(card);
            var row = UiBuild.AddImage("Row_" + card.DisplayName, _ownContent, inDeck ? _rowInDeckColor : _rowColor);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 58f;

            if (card == _selected)
            {
                UiBuild.AddOutline(row, _goldColor, new Vector2(3f, 3f));
            }

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var captured = card;
            row.gameObject.AddComponent<UiClickRelay>().Initialize(count => OnCardClicked(captured, count));

            var strip = UiBuild.AddImage("Strip", row.rectTransform, EffectCardFace.GetGradeColor(card.Grade));
            strip.raycastTarget = false;
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(0f, 1f);
            strip.rectTransform.pivot = new Vector2(0f, 0.5f);
            strip.rectTransform.sizeDelta = new Vector2(8f, 0f);
            strip.rectTransform.anchoredPosition = Vector2.zero;

            bool red = card.Suit == Suit.Heart || card.Suit == Suit.Diamond;
            var code = UiBuild.AddText("Code", row.rectTransform, _font, 26, red ? new Color(1f, 0.5f, 0.45f, 1f) : _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            code.text = EffectCardFace.GetCodeText(card);
            UiBuild.Place(code.rectTransform, 24f, 0f, 90f, 58f);

            var name = UiBuild.AddText("Name", row.rectTransform, _font, 22, _textColor, FontStyle.Bold, TextAnchor.MiddleLeft);
            name.text = card.DisplayName;
            UiBuild.Place(name.rectTransform, 118f, 6f, 200f, 30f);

            var grade = UiBuild.AddText("Grade", row.rectTransform, _font, 15, _mutedColor, FontStyle.Normal, TextAnchor.MiddleLeft);
            grade.text = _gradeNames[(int)card.Grade];
            UiBuild.Place(grade.rectTransform, 118f, 34f, 200f, 20f);

            if (inDeck)
            {
                var tag = UiBuild.AddImage("Tag", row.rectTransform, _tagColor);
                tag.raycastTarget = false;
                tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = tag.rectTransform.pivot = new Vector2(1f, 0.5f);
                tag.rectTransform.sizeDelta = new Vector2(44f, 28f);
                tag.rectTransform.anchoredPosition = new Vector2(-16f, 0f);
                var tagText = UiBuild.AddText("Text", tag.rectTransform, _font, 17, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
                tagText.text = _deckTag;
                UiBuild.Stretch(tagText.rectTransform);
            }
        }

        private void RefreshOwnChips()
        {
            for (int i = 0; i < _ownChips.Count; i++)
            {
                bool on = (_ownFilter == null && i == 0) || (_ownFilter != null && i == 1 + Array.IndexOf(SuitOrder, _ownFilter.Value));
                Color color = i == 0 ? new Color(0.82f, 0.88f, 0.85f, 1f)
                    : (i == 2 || i == 4) ? new Color(1f, 0.5f, 0.45f, 1f) : new Color(0.82f, 0.88f, 0.85f, 1f);
                UiBuild.SetButtonColors(_ownChips[i].GetComponent<Button>(), on ? color : new Color(color.r, color.g, color.b, 0.2f));
                _ownChipTexts[i].color = on ? _inkColor : color;
            }
        }

        // 선택한 카드의 정보를 왼쪽에 보여 준다.
        private void RefreshInfo()
        {
            if (_selected == null)
            {
                return;
            }

            int rounds = _ruleSet.ActiveUsableRounds;
            string footer = _selected.IsActive ? string.Format(_activeFooterFormat, rounds) : _passiveFooter;
            _face.Show(_selected, _activeName, _passiveName, footer, EffectCardFace.GetGradeColor(_selected.Grade), "CREL"[(int)_selected.Grade].ToString());
            _nameText.text = _selected.DisplayName;
            _descriptionText.text = _selected.Description;

            string[] tags = { _gradeNames[(int)_selected.Grade], _selected.IsActive ? _activeName : _passiveName, EffectCardFace.GetCategoryLabel(_selected.Category) };
            float x = 28f;
            for (int i = 0; i < _tagBoxes.Count; i++)
            {
                _tagTexts[i].text = tags[i];
                float width = _tagTexts[i].preferredWidth + 26f;
                UiBuild.Place(_tagBoxes[i].rectTransform, x, 588f, width, 30f);
                x += width + 8f;
            }

            bool inDeck = _deck.Contains(_selected);
            _toggleLabel.text = inDeck ? _removeLabel : _addLabel;
            _toggleCount.text = string.Format(_deckCountFormat, _deck.Count, _ruleSet.DeckMax);
        }

        // ---------- 동작 ----------

        private void OnCardClicked(EffectCardData card, int clickCount)
        {
            _selected = card;
            if (clickCount >= 2)
            {
                Toggle(card);
                return;
            }

            Refresh();
        }

        private void ToggleSelected()
        {
            if (_selected != null)
            {
                Toggle(_selected);
            }
        }

        // 덱에 있으면 빼고, 없으면 규칙(장수·영웅·전설 제한)을 확인한 뒤 넣는다. 넣을 수 없으면 이유를 안내 글로 보여 준다.
        private void Toggle(EffectCardData card)
        {
            if (_deck.Contains(card))
            {
                _deck.Remove(card);
                _toast.Show(_removedToast);
            }
            else
            {
                string reason = _rules.GetRejectReason(_deck, card);
                if (reason != null)
                {
                    _toast.Show(reason);
                    Refresh();
                    return;
                }

                _deck.Add(card);
                _toast.Show(_addedToast);
            }

            PlayerDeckStore.Save(_deck);
            Refresh();
        }

        private void ResetToDefault()
        {
            _deck = PlayerDeckStore.CreateDefault(_effectDatabase);
            PlayerDeckStore.Save(_deck);
            _toast.Show(_defaultToast);
            Refresh();
        }

        private void MakeRandom()
        {
            _deck = _rules.BuildRandomDeck(_effectDatabase.Cards);
            PlayerDeckStore.Save(_deck);
            _toast.Show(_randomToast);
            Refresh();
        }

        private void ClearDeck()
        {
            _deck = new List<EffectCardData>();
            PlayerDeckStore.Save(_deck);
            _toast.Show(_clearToast);
            Refresh();
        }

        private void SetOwnFilter(int index)
        {
            _ownFilter = index == 0 ? (Suit?)null : SuitOrder[index - 1];
            RefreshOwnChips();
            RebuildOwnList();
            _ownScroll.verticalNormalizedPosition = 1f;
        }

        private static int CompareCards(EffectCardData a, EffectCardData b)
        {
            int suit = Array.IndexOf(SuitOrder, a.Suit).CompareTo(Array.IndexOf(SuitOrder, b.Suit));
            if (suit != 0)
            {
                return suit;
            }

            return GetLowRank(a.Rank).CompareTo(GetLowRank(b.Rank));
        }

        private static int GetLowRank(Rank rank)
        {
            return rank == Rank.Ace ? 1 : (int)rank;
        }
    }
}
