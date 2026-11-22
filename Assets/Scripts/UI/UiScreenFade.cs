using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Poker_Card
{
    // 화면이 열릴 때는 검은 막이 걷히고, 다른 씬으로 갈 때는 검은 막이 덮인 뒤에 씬을 바꾼다. 로비 화면들이 함께 쓴다.
    public class UiScreenFade : MonoBehaviour
    {
        private Image _image;
        private float _seconds;
        private float _clock;
        private bool _fadingIn = true;
        private string _nextScene;

        public bool IsLeaving => _nextScene != null;

        public static UiScreenFade Create(RectTransform root, float seconds)
        {
            var image = UiBuild.AddImage("Fade", root, Color.black);
            UiBuild.Stretch(image.rectTransform);
            image.raycastTarget = true;
            image.transform.SetAsLastSibling();

            var fade = image.gameObject.AddComponent<UiScreenFade>();
            fade._image = image;
            fade._seconds = seconds;
            return fade;
        }

        // 막이 완전히 덮이면 씬을 바꾼다. 이미 떠나는 중이면 무시한다.
        public void GoTo(string sceneName)
        {
            if (IsLeaving)
            {
                return;
            }

            _nextScene = sceneName;
            _clock = 0f;
            _fadingIn = false;
            _image.raycastTarget = true;
            _image.transform.SetAsLastSibling();
            enabled = true;
        }

        // 다른 창이 막 위로 올라오지 않도록, 새 창을 만든 쪽이 막을 다시 맨 위로 올릴 때 쓴다.
        public void BringToFront()
        {
            _image.transform.SetAsLastSibling();
        }

        private void Update()
        {
            _clock += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_clock / _seconds);
            if (_fadingIn)
            {
                _image.color = new Color(0f, 0f, 0f, 1f - k);
                if (k >= 1f)
                {
                    _fadingIn = false;
                    _image.raycastTarget = false;
                    enabled = false;
                }

                return;
            }

            _image.color = new Color(0f, 0f, 0f, k);
            if (k >= 1f && _nextScene != null)
            {
                SceneManager.LoadScene(_nextScene);
            }
        }
    }
}
