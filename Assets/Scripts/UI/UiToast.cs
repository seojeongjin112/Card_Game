using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 화면 아래쪽에 잠깐 떴다 사라지는 안내 글(덱에 못 넣는 이유 등). 새 글이 오면 이전 글을 바로 바꿔 보여 준다.
    public class UiToast : MonoBehaviour
    {
        private CanvasGroup _group;
        private Text _text;
        private float _clock = -1f;
        private float _seconds;
        private float _fadeSeconds;

        public static UiToast Create(RectTransform stage, Font font, float top, float width, float seconds, float fadeSeconds)
        {
            var image = UiBuild.AddImage("Toast", stage, new Color(0.02f, 0.09f, 0.07f, 0.96f));
            UiBuild.Place(image.rectTransform, (UiBuild.StageWidth - width) / 2f, top, width, 64f);
            image.raycastTarget = false;
            UiBuild.AddOutline(image, new Color(1f, 0.82f, 0.25f, 1f), new Vector2(3f, 3f));

            var toast = image.gameObject.AddComponent<UiToast>();
            toast._group = image.gameObject.AddComponent<CanvasGroup>();
            toast._group.alpha = 0f;
            toast._group.blocksRaycasts = false;
            toast._text = UiBuild.AddText("Text", image.rectTransform, font, 24, new Color(0.957f, 0.945f, 0.91f, 1f), FontStyle.Bold, TextAnchor.MiddleCenter);
            UiBuild.Stretch(toast._text.rectTransform);
            toast._seconds = seconds;
            toast._fadeSeconds = fadeSeconds;
            return toast;
        }

        public void Show(string message)
        {
            _text.text = message;
            _clock = 0f;
            transform.SetAsLastSibling();
        }

        private void Update()
        {
            if (_clock < 0f)
            {
                return;
            }

            _clock += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01(_clock / _fadeSeconds);
            float fadeOut = Mathf.Clamp01((_seconds - _clock) / _fadeSeconds);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);
            if (_clock >= _seconds)
            {
                _clock = -1f;
                _group.alpha = 0f;
            }
        }
    }
}
