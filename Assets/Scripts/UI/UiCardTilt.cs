using UnityEngine;
using UnityEngine.EventSystems;

namespace Poker_Card
{
    // 마우스를 따라 카드가 입체적으로 기울어지는 효과. 레이아웃 그룹은 회전을 무시하므로 위치가 밀리지 않는다.
    public class UiCardTilt : MonoBehaviour, IPointerMoveHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _target;

        // 포커 카드는 20°, 효과 카드는 14°까지 기운다(프로토타입 값).
        [SerializeField] private float _maxDegrees = 20f;

        // 중심에서 가장자리 사이를 얼마나 빨리 최대 각도에 닿게 할지의 배율. 2면 가장자리 절반 지점에서 최대가 된다.
        [SerializeField] private float _edgeSensitivity = 2f;

        private void Awake()
        {
            if (_target == null)
            {
                _target = (RectTransform)transform;
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_target, eventData.position, eventData.pressEventCamera, out Vector2 local);
            ApplyTilt(new Vector2(local.x / _target.rect.width, local.y / _target.rect.height));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _target.localRotation = Quaternion.identity;
        }

        // 카드 중심 기준 -0.5~0.5 위치를 받아, 가리키는 쪽이 아래로 눌린 듯 기울인다. 시험에서 직접 부를 수 있게 공개한다.
        public void ApplyTilt(Vector2 normalized)
        {
            float magnitude = Mathf.Min(1f, normalized.magnitude * _edgeSensitivity) * _maxDegrees;
            _target.localRotation = Quaternion.AngleAxis(magnitude, new Vector3(normalized.y, -normalized.x, 0f));
        }
    }
}
