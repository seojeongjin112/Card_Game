using System;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 드래프트 창을 프로토타입의 배치(창 틀, 큰 제목과 부제, PICK 태그, 표 모양 미리보기, 오른쪽 아래 큰 확정 버튼)로 맞추는 값들.
    [Serializable]
    public class EffectDraftLayoutSettings
    {
        [SerializeField] private Vector2 _modalSize = new Vector2(1237f, 640f);
        [SerializeField] private float _frameBorder = 4f;
        [SerializeField] private Color _dimColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color _frameBorderColor = new Color(0.88f, 0.95f, 0.9f, 1f);
        [SerializeField] private Color _modalColor = new Color(0.06f, 0.22f, 0.18f, 1f);
        [SerializeField] private float _modalPadding = 46f;
        [SerializeField] private string _title = "이번 라운드에 더할 효과를 고르세요.";
        [SerializeField] private int _titleFontSize = 40;
        [SerializeField] private float _titleTop = 56f;
        [SerializeField] private string _subtitle = "3장 중 1장만 획득 · 확정 전까지는 미리보기";
        [SerializeField] private int _subtitleFontSize = 22;
        [SerializeField] private float _subtitleTop = 112f;
        [SerializeField] private Color _subtitleColor = new Color(0.72f, 0.82f, 0.77f, 1f);
        [SerializeField] private string _pickTag = "PICK 1/1";
        [SerializeField] private Vector2 _pickTagSize = new Vector2(150f, 34f);
        [SerializeField] private float _pickTagTop = 44f;
        [SerializeField] private float _cardRowTop = 153f;
        [SerializeField] private float _cardSpacing = 24f;
        [SerializeField] private Vector2 _previewSize = new Vector2(464f, 357f);
        [SerializeField] private Color _previewColor = new Color(0.04f, 0.15f, 0.12f, 1f);
        [SerializeField] private Color _previewBorderColor = new Color(0.3f, 0.45f, 0.4f, 1f);
        [SerializeField] private string _previewHeader = "장착 후 예상 상태";
        [SerializeField] private Color _cardBoxColor = new Color(0.96f, 0.95f, 0.9f, 1f);
        [SerializeField] private Color _cardBoxTextColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color _cardBoxInfoColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private int _helpFontSize = 20;
        [SerializeField] private Color _helpColor = new Color(0.72f, 0.82f, 0.77f, 1f);
        [SerializeField] private float _bottomMargin = 73f;
        [SerializeField] private Vector2 _confirmSize = new Vector2(252f, 63f);
        [SerializeField] private Color _confirmColor = new Color(0.96f, 0.38f, 0.27f, 1f);
        [SerializeField] private Color _confirmDisabledColor = new Color(0.32f, 0.4f, 0.37f, 1f);
        [SerializeField] private int _confirmFontSize = 26;
        [SerializeField] private Vector2 _skipSize = new Vector2(220f, 60f);
        [SerializeField] private Color _skipColor = new Color(0.35f, 0.45f, 0.42f, 1f);
        [SerializeField] private string _skipText = "새 카드 포기";
        [SerializeField] private Vector2 _discardSlotSize = new Vector2(150f, 64f);
        [SerializeField] private float _discardRowY = -196f;
        [SerializeField] private int _discardFontSize = 14;

        public Vector2 ModalSize => _modalSize;
        public float FrameBorder => _frameBorder;
        public Color DimColor => _dimColor;
        public Color FrameBorderColor => _frameBorderColor;
        public Color ModalColor => _modalColor;
        public float ModalPadding => _modalPadding;
        public string Title => _title;
        public int TitleFontSize => _titleFontSize;
        public float TitleTop => _titleTop;
        public string Subtitle => _subtitle;
        public int SubtitleFontSize => _subtitleFontSize;
        public float SubtitleTop => _subtitleTop;
        public Color SubtitleColor => _subtitleColor;
        public string PickTag => _pickTag;
        public Vector2 PickTagSize => _pickTagSize;
        public float PickTagTop => _pickTagTop;
        public float CardRowTop => _cardRowTop;
        public float CardSpacing => _cardSpacing;
        public Vector2 PreviewSize => _previewSize;
        public Color PreviewColor => _previewColor;
        public Color PreviewBorderColor => _previewBorderColor;
        public string PreviewHeader => _previewHeader;
        public Color CardBoxColor => _cardBoxColor;
        public Color CardBoxTextColor => _cardBoxTextColor;
        public Color CardBoxInfoColor => _cardBoxInfoColor;
        public int HelpFontSize => _helpFontSize;
        public Color HelpColor => _helpColor;
        public float BottomMargin => _bottomMargin;
        public Vector2 ConfirmSize => _confirmSize;
        public Color ConfirmColor => _confirmColor;
        public Color ConfirmDisabledColor => _confirmDisabledColor;
        public int ConfirmFontSize => _confirmFontSize;
        public Vector2 SkipSize => _skipSize;
        public Color SkipColor => _skipColor;
        public string SkipText => _skipText;
        public Vector2 DiscardSlotSize => _discardSlotSize;
        public float DiscardRowY => _discardRowY;
        public int DiscardFontSize => _discardFontSize;
    }

    // 프로토타입식 표 모양 미리보기를 이루는 글자들. 드래프트 창이 내용을 채운다.
    public sealed class DraftPreviewParts
    {
        public Text Header;
        public Text Row1Label;
        public Text Row1Value;
        public Text Row2Label;
        public Text Row2Value;
        public GameObject CardBox;
        public Text CardName;
        public Text CardDescription;
        public Text CardInfo;
    }

    // 장면에 이미 있는 드래프트 창 오브젝트의 위치·색을 프로토타입처럼 다시 배치하고, 모자란 틀은 코드로 만든다.
    public static class EffectDraftLayout
    {
        public static DraftPreviewParts Apply(EffectDraftLayoutSettings s, Font font, RectTransform panel, Text title,
            RectTransform offerRow, RectTransform previewPanel, Text oldPreviewText, RectTransform equippedRow,
            EffectCardSlotView[] equippedSlots, Button confirm, Button skip, Text hint)
        {
            float halfW = s.ModalSize.x / 2f;
            float halfH = s.ModalSize.y / 2f;
            float left = -halfW + s.ModalPadding;
            float right = halfW - s.ModalPadding;
            float top = halfH;
            float bottom = -halfH;

            // 뒤쪽 게임 화면이 어둡게 비치도록 배경을 반투명으로 바꾼다.
            var dim = panel.GetComponent<Image>();
            if (dim != null)
            {
                dim.color = s.DimColor;
            }

            // 창 틀: 흰 테두리 위에 짙은 녹색 바탕. 다른 요소들 뒤에 깔리도록 맨 앞 자식으로 둔다.
            var border = CreateImage("ModalBorder", panel, Vector2.zero, s.ModalSize + new Vector2(s.FrameBorder * 2f, s.FrameBorder * 2f), s.FrameBorderColor);
            var body = CreateImage("ModalBody", panel, Vector2.zero, s.ModalSize, s.ModalColor);
            border.transform.SetAsFirstSibling();
            body.transform.SetSiblingIndex(1);

            Place(title.rectTransform, new Vector2(left + 450f, top - s.TitleTop), new Vector2(900f, 60f));
            title.text = s.Title;
            title.fontSize = s.TitleFontSize;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleLeft;
            title.color = Color.white;

            var subtitle = CreateText("Subtitle", panel, font, new Vector2(left + 450f, top - s.SubtitleTop), new Vector2(900f, 34f),
                s.SubtitleFontSize, TextAnchor.MiddleLeft, s.SubtitleColor, FontStyle.Normal);
            subtitle.text = s.Subtitle;

            var tagBack = CreateImage("PickTagBack", panel, new Vector2(right - s.PickTagSize.x / 2f, top - s.PickTagTop), s.PickTagSize, new Color(0.02f, 0.1f, 0.08f, 1f));
            var tag = CreateText("PickTag", panel, font, new Vector2(right - s.PickTagSize.x / 2f, top - s.PickTagTop), s.PickTagSize, 18,
                TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            tag.text = s.PickTag;

            // 카드 3장은 왼쪽 영역에, 미리보기는 오른쪽에 둔다. 줄 폭은 슬롯 폭과 간격으로 정해 왼쪽 끝을 맞춘다.
            float slotWidth = ((RectTransform)offerRow.GetChild(0)).sizeDelta.x;
            int slotCount = offerRow.childCount;
            float rowWidth = slotCount * slotWidth + (slotCount - 1) * s.CardSpacing;
            Place(offerRow, new Vector2(left + rowWidth / 2f, top - s.CardRowTop - offerRow.sizeDelta.y / 2f), new Vector2(rowWidth, offerRow.sizeDelta.y));
            var layout = offerRow.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = s.CardSpacing;
                layout.childAlignment = TextAnchor.UpperLeft;
            }

            Place(previewPanel, new Vector2(right - s.PreviewSize.x / 2f, top - s.CardRowTop - s.PreviewSize.y / 2f), s.PreviewSize);
            var previewBack = previewPanel.GetComponent<Image>();
            if (previewBack != null)
            {
                previewBack.color = s.PreviewColor;
            }

            oldPreviewText.enabled = false;
            var parts = BuildPreview(s, font, previewPanel);

            // 아래쪽: 왼쪽에 작은 안내문, 오른쪽에 큰 확정 버튼, 그 왼쪽에 (가득 찼을 때만 쓰는) 포기 버튼.
            float bottomY = bottom + s.BottomMargin;
            Place(hint.rectTransform, new Vector2(left + 430f, bottomY), new Vector2(860f, 36f));
            hint.alignment = TextAnchor.MiddleLeft;
            hint.fontSize = s.HelpFontSize;
            hint.color = s.HelpColor;

            StyleButton(confirm, new Vector2(right - s.ConfirmSize.x / 2f, bottomY), s.ConfirmSize, s.ConfirmColor, s.ConfirmDisabledColor, s.ConfirmFontSize, null);
            StyleButton(skip, new Vector2(right - s.ConfirmSize.x - s.SkipSize.x / 2f - 20f, bottomY), s.SkipSize, s.SkipColor, s.ConfirmDisabledColor, 22, s.SkipText);

            // 교체 모드에서 보이는 내 장착 카드 줄은 창 폭 안에 들어가도록 작게 줄인다.
            if (equippedRow != null)
            {
                float rowW = equippedSlots.Length * s.DiscardSlotSize.x + (equippedSlots.Length - 1) * 8f;
                Place(equippedRow, new Vector2(left + rowW / 2f, s.DiscardRowY), new Vector2(rowW, s.DiscardSlotSize.y));
                foreach (var slot in equippedSlots)
                {
                    ((RectTransform)slot.transform).sizeDelta = s.DiscardSlotSize;
                    var label = slot.GetComponentInChildren<Text>(true);
                    if (label != null)
                    {
                        label.fontSize = s.DiscardFontSize;
                    }
                }
            }

            return parts;
        }

        // 제목 줄, 두 개의 표 행(왼쪽 이름·오른쪽 굵은 값), 그 아래 흰 카드 설명 상자.
        private static DraftPreviewParts BuildPreview(EffectDraftLayoutSettings s, Font font, RectTransform previewPanel)
        {
            var parts = new DraftPreviewParts();
            float w = s.PreviewSize.x;
            float h = s.PreviewSize.y;
            float padding = 28f;
            float innerW = w - padding * 2f;

            CreateImage("PreviewBorder", previewPanel, Vector2.zero, s.PreviewSize, s.PreviewBorderColor).transform.SetAsFirstSibling();
            CreateImage("PreviewInner", previewPanel, Vector2.zero, s.PreviewSize - new Vector2(6f, 6f), s.PreviewColor).transform.SetSiblingIndex(1);

            parts.Header = CreateText("PreviewHeader", previewPanel, font, new Vector2(0f, h / 2f - 40f), new Vector2(innerW, 40f), 26, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            parts.Header.text = s.PreviewHeader;

            parts.Row1Label = CreateText("Row1Label", previewPanel, font, new Vector2(0f, h / 2f - 100f), new Vector2(innerW, 34f), 22, TextAnchor.MiddleLeft, s.SubtitleColor, FontStyle.Normal);
            parts.Row1Value = CreateText("Row1Value", previewPanel, font, new Vector2(0f, h / 2f - 100f), new Vector2(innerW, 34f), 24, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
            parts.Row2Label = CreateText("Row2Label", previewPanel, font, new Vector2(0f, h / 2f - 150f), new Vector2(innerW, 34f), 22, TextAnchor.MiddleLeft, s.SubtitleColor, FontStyle.Normal);
            parts.Row2Value = CreateText("Row2Value", previewPanel, font, new Vector2(0f, h / 2f - 150f), new Vector2(innerW, 34f), 24, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);

            var boxImage = CreateImage("CardBox", previewPanel, new Vector2(0f, -h / 2f + 92f), new Vector2(innerW, 135f), s.CardBoxColor);
            parts.CardBox = boxImage.gameObject;
            var box = boxImage.rectTransform;
            parts.CardName = CreateText("CardName", box, font, new Vector2(0f, 46f), new Vector2(innerW - 36f, 36f), 26, TextAnchor.MiddleLeft, s.CardBoxTextColor, FontStyle.Bold);
            parts.CardDescription = CreateText("CardDescription", box, font, new Vector2(0f, 4f), new Vector2(innerW - 36f, 48f), 20, TextAnchor.UpperLeft, s.CardBoxTextColor, FontStyle.Normal);
            parts.CardInfo = CreateText("CardInfo", box, font, new Vector2(0f, -48f), new Vector2(innerW - 36f, 34f), 16, TextAnchor.MiddleLeft, s.CardBoxInfoColor, FontStyle.Normal);
            return parts;
        }

        private static void StyleButton(Button button, Vector2 position, Vector2 size, Color color, Color disabledColor, int fontSize, string text)
        {
            Place((RectTransform)button.transform, position, size);
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = Color.white;
            }

            // 버튼 색은 상태별 색조로 낸다. 눌러서 쓸 수 있을 때는 주황, 막혔을 때는 어두운 녹색이다.
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
            colors.selectedColor = color;
            colors.disabledColor = disabledColor;
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.color = Color.white;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                if (text != null)
                {
                    label.text = text;
                }
            }
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Image CreateImage(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Place(rect, position, size);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, RectTransform parent, Font font, Vector2 position, Vector2 size, int fontSize,
            TextAnchor anchor, Color color, FontStyle style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, position, size);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
