using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    public class EnemyHandView : MonoBehaviour
    {
        [SerializeField] private Image[] _faces;
        [SerializeField] private Sprite _backSprite;

        [Header("뒷면이 내려앉는 연출 (프로토타입 dealOp)")]
        [SerializeField] private Vector2 _dropOffset = new Vector2(0f, 80f);
        [SerializeField] private float _dropRotation = 20f;
        [SerializeField] private float _dropDuration = 0.45f;
        [SerializeField] private float _dropStagger = 0.07f;
        [SerializeField] private Vector4 _dropEase = new Vector4(0.2f, 1f, 0.4f, 1f);

        private Vector2[] _appliedOffsets;
        private CanvasGroup[] _groups;
        private float _dropTime;
        private bool _dropping;

        // 점수판이 상대 카드를 같은 뒷면 그림으로 시작하도록 알려 준다.
        public Sprite BackSprite => _backSprite;

        private void Awake()
        {
            _appliedOffsets = new Vector2[_faces.Length];
            _groups = new CanvasGroup[_faces.Length];
            for (int i = 0; i < _faces.Length; i++)
            {
                _groups[i] = _faces[i].GetComponent<CanvasGroup>();
                if (_groups[i] == null)
                {
                    _groups[i] = _faces[i].gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        public void ShowBacks(float delay = 0f)
        {
            foreach (var face in _faces)
            {
                face.sprite = _backSprite;
                face.color = Color.white;
            }

            _dropTime = -delay;
            _dropping = true;
            ApplyDrop();
        }

        // 프로토타입은 상대 패를 위쪽 줄에서 공개하지 않는다("상대 전투 패 · 비공개"). 정산 때의 공개는 가운데 점수판이 맡는다.
        public void Reveal(IReadOnlyList<PokerCardData> hand, IReadOnlyList<PokerCardData> best)
        {
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // 테스트가 시간을 직접 넘길 수 있게 Tick을 연다.
        public void Tick(float dt)
        {
            if (!_dropping)
            {
                return;
            }

            _dropTime += dt;
            ApplyDrop();
            if (_dropTime >= _dropDuration + _dropStagger * (_faces.Length - 1))
            {
                _dropping = false;
            }
        }

        // 카드마다 시간차를 두고, 위에서 기울어진 채 내려와 제자리에 앉는다.
        private void ApplyDrop()
        {
            for (int i = 0; i < _faces.Length; i++)
            {
                float k = Mathf.Clamp01((_dropTime - i * _dropStagger) / _dropDuration);
                float progress = UiEase.CubicBezier(_dropEase.x, _dropEase.y, _dropEase.z, _dropEase.w, k);
                float remain = 1f - progress;

                // 레이아웃이 자리를 다시 잡아도 어긋나지 않도록, 지난번에 더한 오프셋만 빼고 새 오프셋을 더한다.
                Vector2 offset = _dropOffset * remain;
                _faces[i].rectTransform.anchoredPosition += offset - _appliedOffsets[i];
                _appliedOffsets[i] = offset;
                _faces[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, _dropRotation * remain);
                _groups[i].alpha = _dropTime - i * _dropStagger <= 0f ? 0f : Mathf.Clamp01(progress);
            }
        }
    }
}
