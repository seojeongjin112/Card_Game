using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 화면 가장자리에만 색이 깔리는 비네트. HP 위험(붉은색)과 접전(보라색) 때 숨 쉬듯 깜빡인다.
    // 씬에 오브젝트를 두지 않으려고 감독이 코드로 만들고, 움직임은 감독의 Tick이 돌린다.
    public class UiFxVignette : MonoBehaviour
    {
        private const int TextureSize = 128;

        private Image _image;
        private Color _color;
        private float _strongAlpha;
        private float _weakAlpha;
        private float _period;
        private float _fadeSeconds;
        private float _clock;
        private float _visible;
        private bool _active;

        // edgeFalloff는 가장자리에서 색이 번지는 폭을 화면 너비·높이에 대한 비율로 준다(가로세로가 달라도 같은 픽셀 폭이 되도록 따로 받는다).
        public static UiFxVignette Create(RectTransform parent, string name, Vector2 edgeFalloff)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var vignette = go.AddComponent<UiFxVignette>();
            vignette._image = go.AddComponent<Image>();
            vignette._image.sprite = CreateSprite(edgeFalloff);
            vignette._image.raycastTarget = false;
            vignette._image.color = Color.clear;
            return vignette;
        }

        public bool IsActive => _active;

        // startAlpha에서 시작해 주기의 절반에 midAlpha까지 갔다가 돌아오는 것을 period초마다 되풀이한다.
        // 프로토타입의 danger는 진했다가 옅어지고, clutch는 옅었다가 진해져서 둘 다 이 두 값으로 표현한다.
        public void Show(Color color, float startAlpha, float midAlpha, float period, float fadeSeconds)
        {
            _color = color;
            _strongAlpha = startAlpha;
            _weakAlpha = midAlpha;
            _period = period;
            _fadeSeconds = fadeSeconds;
            _active = true;
        }

        public void Hide()
        {
            _active = false;
        }

        public void Tick(float dt)
        {
            _visible = Mathf.MoveTowards(_visible, _active ? 1f : 0f, dt / _fadeSeconds);
            if (_visible <= 0f)
            {
                _image.color = Color.clear;
                return;
            }

            _clock += dt;
            // 프로토타입의 ease-in-out 왕복: 한 주기 동안 진함 → 옅음 → 진함.
            float wave = (1f + Mathf.Cos(_clock / _period * Mathf.PI * 2f)) * 0.5f;
            float alpha = Mathf.Lerp(_weakAlpha, _strongAlpha, wave) * _visible;
            _image.color = new Color(_color.r, _color.g, _color.b, alpha);
        }

        // 가장자리에서 안쪽으로 갈수록 투명해지는 사각 비네트. falloff가 클수록 안쪽까지 색이 번진다.
        private static Sprite CreateSprite(Vector2 falloff)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float nx = (x + 0.5f) / TextureSize;
                    float ny = (y + 0.5f) / TextureSize;
                    float edgeX = Mathf.Min(nx, 1f - nx) / falloff.x;
                    float edgeY = Mathf.Min(ny, 1f - ny) / falloff.y;
                    float alpha = Mathf.Clamp01(1f - Mathf.Min(edgeX, edgeY));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
