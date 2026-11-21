using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 피해를 받은 HUD가 흔들리고 붉게 번쩍이며, HP 숫자가 커졌다 돌아온다(프로토타입 hudHit / hpHit).
    // 씬에는 아무것도 더하지 않고 기존 HUD와 HP 글자에 값만 덮어쓴다. 움직임은 감독의 Tick이 돌린다.
    public class UiFxHudHit
    {
        private readonly RectTransform _hud;
        private readonly Image _background;
        private readonly Text _hpText;
        private readonly Settings _settings;
        private Color _restBackground;
        private Color _restTextColor;
        private Vector2 _appliedOffset;
        private float _time = -1f;

        public sealed class Settings
        {
            // 1440x810 기준 프로토타입 값을 1920x1080으로 환산해 둔 흔들림 키프레임. 성분은 (시간 비율, x, y, 회전)이다.
            public Vector4[] Shake { get; } =
            {
                new Vector4(0f, 0f, 0f, 0f),
                new Vector4(0.15f, -13.3f, 5.3f, -1.5f),
                new Vector4(0.30f, 12f, -4f, 1f),
                new Vector4(0.45f, -8f, 2.7f, 0f),
                new Vector4(0.60f, 5.3f, 0f, 0f),
                new Vector4(1f, 0f, 0f, 0f)
            };
            public float Duration { get; } = 0.55f;
            public float TextDuration { get; } = 0.5f;
            public float TextScale { get; } = 1.5f;
            public Color FlashColor { get; } = new Color(0.478f, 0.102f, 0.071f, 1f);
            public Color TextFlashColor { get; } = new Color(1f, 0.35f, 0.24f, 1f);
        }

        public UiFxHudHit(RectTransform hud, RectTransform bar, Settings settings)
        {
            _hud = hud;
            _background = hud.GetComponent<Image>();
            _hpText = bar != null ? bar.GetComponentInChildren<Text>(true) : null;
            _settings = settings;
        }

        public void Play()
        {
            if (_time < 0f)
            {
                _restBackground = _background != null ? _background.color : Color.clear;
                _restTextColor = _hpText != null ? _hpText.color : Color.white;
            }

            _time = 0f;
            Apply();
        }

        public void Tick(float dt)
        {
            if (_time < 0f)
            {
                return;
            }

            _time += dt;
            Apply();
            if (_time >= Mathf.Max(_settings.Duration, _settings.TextDuration))
            {
                Finish();
            }
        }

        // 진행 중에 멈추더라도 HUD가 어긋난 채 남지 않게 원래대로 돌린다.
        public void Cancel()
        {
            if (_time >= 0f)
            {
                Finish();
            }
        }

        private void Apply()
        {
            float k = Mathf.Clamp01(_time / _settings.Duration);
            Vector4 shake = SampleShake(k);

            // 레이아웃이 자리를 다시 잡아도 어긋나지 않게, 지난번에 더한 오프셋만 빼고 새 오프셋을 더한다.
            Vector2 offset = new Vector2(shake.y, shake.z);
            _hud.anchoredPosition += offset - _appliedOffset;
            _appliedOffset = offset;
            _hud.localRotation = Quaternion.Euler(0f, 0f, shake.w);

            if (_background != null)
            {
                _background.color = Color.Lerp(_settings.FlashColor, _restBackground, k);
            }

            if (_hpText != null)
            {
                float textK = Mathf.Clamp01(_time / _settings.TextDuration);
                _hpText.transform.localScale = Vector3.one * Mathf.Lerp(_settings.TextScale, 1f, textK);
                _hpText.color = Color.Lerp(_settings.TextFlashColor, _restTextColor, textK);
            }
        }

        private void Finish()
        {
            _hud.anchoredPosition -= _appliedOffset;
            _appliedOffset = Vector2.zero;
            _hud.localRotation = Quaternion.identity;
            if (_background != null)
            {
                _background.color = _restBackground;
            }

            if (_hpText != null)
            {
                _hpText.transform.localScale = Vector3.one;
                _hpText.color = _restTextColor;
            }

            _time = -1f;
        }

        private Vector4 SampleShake(float k)
        {
            var frames = _settings.Shake;
            for (int i = 0; i < frames.Length - 1; i++)
            {
                if (k >= frames[i].x && k <= frames[i + 1].x)
                {
                    float t = Mathf.InverseLerp(frames[i].x, frames[i + 1].x, k);
                    return Vector4.Lerp(frames[i], frames[i + 1], t);
                }
            }

            return Vector4.zero;
        }
    }
}
