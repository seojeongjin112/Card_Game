using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Poker_Card
{
    public class EnemyHandView : MonoBehaviour
    {
        [SerializeField] private Image[] _faces;
        [SerializeField] private Sprite _backSprite;
        [SerializeField] private Color _dimmedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        public void ShowBacks()
        {
            foreach (var face in _faces)
            {
                face.sprite = _backSprite;
                face.color = Color.white;
            }
        }

        // 최고 5장은 밝게, 고르지 않은 카드는 어둡게 해서 상대가 낸 패가 한눈에 보이게 한다.
        public void Reveal(IReadOnlyList<PokerCardData> hand, IReadOnlyList<PokerCardData> best)
        {
            for (int i = 0; i < _faces.Length && i < hand.Count; i++)
            {
                _faces[i].sprite = hand[i].Sprite;
                _faces[i].color = Contains(best, hand[i]) ? Color.white : _dimmedColor;
            }
        }

        private static bool Contains(IReadOnlyList<PokerCardData> cards, PokerCardData card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (ReferenceEquals(cards[i], card))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
