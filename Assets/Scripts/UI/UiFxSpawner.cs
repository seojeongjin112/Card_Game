using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    // 한 지점에서 터지는 연출(링, 파편, 팝업 숫자, HP 조각)을 풀링해서 만든다. 움직임은 Tick 하나로 모두 돌린다.
    public class UiFxSpawner : MonoBehaviour
    {
        private sealed class FxItem
        {
            public RectTransform Rect;
            public Graphic Graphic;
            public Vector2 Velocity;
            public float Gravity;
            public float RotationVelocity;
            public float StartScale;
            public float EndScale;
            public float Life;
            public float Age;
            public float FadeStart;
            public Color Color;
            public bool IsText;
        }

        [SerializeField] private RectTransform _layer;
        [SerializeField] private Font _font;
        [SerializeField] private int _maxActiveItems = 220;
        [SerializeField] private int _ringTextureSize = 128;
        [SerializeField, Range(0.5f, 0.98f)] private float _ringInnerRadius = 0.86f;
        [SerializeField] private float _ringStartScale = 0.1f;
        [SerializeField] private float _burstGravity = 700f;
        [SerializeField] private Vector2 _burstSizeRange = new Vector2(6f, 15f);
        [SerializeField] private Vector2 _burstLifeRange = new Vector2(0.5f, 0.95f);
        [SerializeField] private float _popupRiseSpeed = 110f;
        [SerializeField] private float _popupLife = 1.1f;
        [SerializeField] private float _popupStartScale = 0.5f;
        [SerializeField] private float _popupEndScale = 1.15f;
        [SerializeField] private float _chunkWidthPerCount = 9f;
        [SerializeField] private Vector2Int _chunkCountRange = new Vector2Int(2, 14);
        [SerializeField] private float _chunkSpeed = 90f;
        [SerializeField] private float _chunkGravity = 900f;
        [SerializeField] private float _chunkLife = 1f;
        [SerializeField] private Color[] _confettiColors =
        {
            new Color(1f, 0.35f, 0.24f, 1f), new Color(0.16f, 0.42f, 0.74f, 1f), new Color(0.9f, 0.64f, 0.24f, 1f),
            new Color(0.49f, 0.3f, 0.77f, 1f), new Color(0.18f, 0.64f, 0.42f, 1f), new Color(1f, 0.88f, 0.54f, 1f), Color.white
        };
        [SerializeField] private Vector2 _confettiSizeRange = new Vector2(8f, 20f);
        [SerializeField] private Vector2 _confettiLifeRange = new Vector2(2.4f, 4f);
        [SerializeField] private float _confettiGravity = 260f;
        [SerializeField] private float _confettiDrift = 220f;
        [SerializeField] private float _confettiStartMargin = 40f;
        [SerializeField] private Vector2 _streamSizeRange = new Vector2(12f, 24f);
        [SerializeField] private float _streamSpread = 30f;

        private readonly List<FxItem> _active = new List<FxItem>();
        private readonly Stack<FxItem> _imagePool = new Stack<FxItem>();
        private readonly Stack<FxItem> _textPool = new Stack<FxItem>();
        private Sprite _ringSprite;

        public int ActiveCount => _active.Count;

        private void Awake()
        {
            _ringSprite = CreateRingSprite();
            enabled = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 대상 UI의 중심을 이 레이어의 좌표로 바꿔 준다. 오버레이 캔버스에서는 월드 좌표가 곧 화면 좌표다.
        public Vector2 ToLocal(RectTransform target)
        {
            Vector3 world = target.TransformPoint(target.rect.center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, world, null, out Vector2 local);
            return local;
        }

        public void Ring(Vector2 position, Color color, float size, float duration)
        {
            var item = Take(false);
            if (item == null)
            {
                return;
            }

            item.Graphic.GetComponent<Image>().sprite = _ringSprite;
            Place(item, position, new Vector2(size, size), color);
            item.Life = duration;
            item.StartScale = _ringStartScale;
            item.EndScale = 1f;
            item.FadeStart = 0f;
        }

        public void Burst(Vector2 position, Color[] colors, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                var item = Take(false);
                if (item == null)
                {
                    return;
                }

                item.Graphic.GetComponent<Image>().sprite = null;
                float size = Random.Range(_burstSizeRange.x, _burstSizeRange.y);
                Place(item, position, new Vector2(size, size), colors[Random.Range(0, colors.Length)]);
                float angle = Random.value * Mathf.PI * 2f;
                item.Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(speed * 0.3f, speed);
                item.Gravity = _burstGravity;
                item.RotationVelocity = Random.Range(-360f, 360f);
                item.Life = Random.Range(_burstLifeRange.x, _burstLifeRange.y);
                item.StartScale = 1f;
                item.EndScale = 0.4f;
                item.FadeStart = 0.5f;
            }
        }

        public void Popup(Vector2 position, string text, Color color, int fontSize)
        {
            var item = Take(true);
            if (item == null)
            {
                return;
            }

            var label = (Text)item.Graphic;
            label.text = text;
            label.fontSize = fontSize;
            Place(item, position, new Vector2(fontSize * 8f, fontSize * 1.6f), color);
            item.Velocity = new Vector2(0f, _popupRiseSpeed);
            item.Life = _popupLife;
            item.StartScale = _popupStartScale;
            item.EndScale = _popupEndScale;
            item.FadeStart = 0.6f;
        }

        // 깎인 HP 구간을 조각내서 튀어 나가게 한다. 구간이 넓을수록 조각이 많다.
        public void Chunks(RectTransform bar, float fromPercent, float toPercent, Color color)
        {
            if (fromPercent <= toPercent)
            {
                return;
            }

            Vector2 center = ToLocal(bar);
            float width = bar.rect.width * bar.lossyScale.x / _layer.lossyScale.x;
            float height = bar.rect.height * bar.lossyScale.y / _layer.lossyScale.y;
            float left = center.x - width / 2f;
            float lostWidth = width * (fromPercent - toPercent) / 100f;
            float startX = left + width * toPercent / 100f;
            int count = Mathf.Clamp(Mathf.RoundToInt(lostWidth / _chunkWidthPerCount), _chunkCountRange.x, _chunkCountRange.y);
            float pieceWidth = lostWidth / count;

            for (int i = 0; i < count; i++)
            {
                var item = Take(false);
                if (item == null)
                {
                    return;
                }

                item.Graphic.GetComponent<Image>().sprite = null;
                Place(item, new Vector2(startX + pieceWidth * (i + 0.5f), center.y), new Vector2(pieceWidth + 1f, height), color);
                float vx = Random.Range(-1f, 1f) * _chunkSpeed;
                item.Velocity = new Vector2(vx, _chunkSpeed * Random.Range(0.6f, 1.6f));
                item.Gravity = _chunkGravity;
                item.RotationVelocity = vx * 3f;
                item.Life = _chunkLife + Random.value * 0.2f;
                item.StartScale = 1f;
                item.EndScale = 1f;
                item.FadeStart = 0.4f;
            }
        }

        // 화면 위에서 색종이가 흩날리며 떨어진다. 높은 족보를 만들었을 때의 축하 연출이다.
        public void Confetti(int count)
        {
            float halfWidth = _layer.rect.width / 2f;
            float top = _layer.rect.height / 2f + _confettiStartMargin;
            for (int i = 0; i < count; i++)
            {
                var item = Take(false);
                if (item == null)
                {
                    return;
                }

                item.Graphic.GetComponent<Image>().sprite = null;
                float width = Random.Range(_confettiSizeRange.x, _confettiSizeRange.y);
                Place(item, new Vector2(Random.Range(-halfWidth, halfWidth), top), new Vector2(width, width * Random.Range(0.5f, 1.5f)),
                    _confettiColors[Random.Range(0, _confettiColors.Length)]);
                item.Velocity = new Vector2(Random.Range(-_confettiDrift, _confettiDrift), -Random.Range(40f, 120f));
                item.Gravity = _confettiGravity;
                item.RotationVelocity = Random.Range(-540f, 540f);
                item.Life = Random.Range(_confettiLifeRange.x, _confettiLifeRange.y);
                item.StartScale = 1f;
                item.EndScale = 1f;
                item.FadeStart = 0.8f;
            }
        }

        // 승자 쪽에서 패자 쪽으로 파편이 쏟아지는 줄기. 프로토타입의 광선(beam)을 대신한다.
        public void Stream(Vector2 from, Vector2 to, Color color, int count, float duration)
        {
            for (int i = 0; i < count; i++)
            {
                var item = Take(false);
                if (item == null)
                {
                    return;
                }

                item.Graphic.GetComponent<Image>().sprite = null;
                float size = Random.Range(_streamSizeRange.x, _streamSizeRange.y);
                Vector2 start = from + Random.insideUnitCircle * _streamSpread;
                Place(item, start, new Vector2(size, size), color);
                float life = duration * Random.Range(0.8f, 1f);
                item.Velocity = (to - start) / life;
                item.Gravity = 0f;
                item.RotationVelocity = Random.Range(-360f, 360f);
                item.Life = life;
                item.StartScale = 1f;
                item.EndScale = 0.5f;
                item.FadeStart = 0.7f;
            }
        }

        // 두 점을 잇는 가는 선분. 움직이지 않고 오래 남는다(패배 때 화면에 금이 가는 연출 등).
        public void Segment(Vector2 from, Vector2 to, Color color, float thickness, float life)
        {
            var item = Take(false);
            if (item == null)
            {
                return;
            }

            item.Graphic.GetComponent<Image>().sprite = null;
            Vector2 delta = to - from;
            Place(item, (from + to) / 2f, new Vector2(delta.magnitude, thickness), color);
            item.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            item.Life = life;
            item.StartScale = 1f;
            item.EndScale = 1f;
            item.FadeStart = 0.85f;
        }

        public void Tick(float dt)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var item = _active[i];
                item.Age += dt;
                float t = Mathf.Clamp01(item.Age / item.Life);
                if (t >= 1f)
                {
                    Release(item, i);
                    continue;
                }

                item.Velocity.y -= item.Gravity * dt;
                item.Rect.anchoredPosition += item.Velocity * dt;
                item.Rect.localRotation = Quaternion.Euler(0f, 0f, item.Rect.localEulerAngles.z + item.RotationVelocity * dt);

                // 처음에는 빠르게 커졌다가 느려지도록 이징을 건다.
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                item.Rect.localScale = Vector3.one * Mathf.Lerp(item.StartScale, item.EndScale, eased);

                float alpha = t < item.FadeStart ? 1f : 1f - (t - item.FadeStart) / (1f - item.FadeStart);
                var color = item.Color;
                color.a *= alpha;
                item.Graphic.color = color;
            }

            enabled = _active.Count > 0;
        }

        private FxItem Take(bool isText)
        {
            if (_active.Count >= _maxActiveItems)
            {
                return null;
            }

            var pool = isText ? _textPool : _imagePool;
            var item = pool.Count > 0 ? pool.Pop() : Create(isText);
            item.Age = 0f;
            item.Velocity = Vector2.zero;
            item.Gravity = 0f;
            item.RotationVelocity = 0f;
            item.Rect.localRotation = Quaternion.identity;
            item.Rect.gameObject.SetActive(true);
            _active.Add(item);
            enabled = true;
            return item;
        }

        private void Release(FxItem item, int index)
        {
            _active.RemoveAt(index);
            item.Rect.gameObject.SetActive(false);
            (item.IsText ? _textPool : _imagePool).Push(item);
        }

        private FxItem Create(bool isText)
        {
            var go = new GameObject(isText ? "FxText" : "FxImage", typeof(RectTransform));
            go.layer = _layer.gameObject.layer;
            go.transform.SetParent(_layer, false);

            Graphic graphic;
            if (isText)
            {
                var text = go.AddComponent<Text>();
                text.font = _font;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                graphic = text;
            }
            else
            {
                graphic = go.AddComponent<Image>();
            }

            graphic.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return new FxItem { Rect = rect, Graphic = graphic, IsText = isText };
        }

        private static void Place(FxItem item, Vector2 position, Vector2 size, Color color)
        {
            item.Rect.anchoredPosition = position;
            item.Rect.sizeDelta = size;
            item.Color = color;
            item.Graphic.color = color;
        }

        private Sprite CreateRingSprite()
        {
            int size = _ringTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 중심에서 가장자리까지의 거리(0~1)가 안쪽 반지름과 1 사이일 때만 칠해 고리 모양을 만든다.
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float inner = Mathf.InverseLerp(_ringInnerRadius - 0.04f, _ringInnerRadius, distance);
                    float outer = 1f - Mathf.InverseLerp(0.96f, 1f, distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(inner * outer)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
