using System.Collections.Generic;
using System.Text;
using Core;
using UnityEngine;

namespace Effect_Card
{
    // 플레이어가 덱 화면에서 만든 효과 덱을 기기에 저장하고 불러온다. 전투는 이 덱으로 시작한다.
    // 저장 값은 "문양숫자" 번호를 쉼표로 이은 글자(예: 0-14,1-2)라서, 카드 이름이 바뀌어도 깨지지 않는다.
    public static class PlayerDeckStore
    {
        private const string Key = "player_effect_deck";
        private const char Separator = ',';
        private const char PartSeparator = '-';

        // 프로토타입의 기본 덱: 스페이드 중심 26장.
        private static readonly string[] DefaultCodes =
        {
            "♠A", "♠2", "♠3", "♠4", "♠5", "♠6", "♠7", "♠8", "♠9", "♠J", "♠Q", "♠K",
            "♥2", "♥3", "♥5", "♥6", "♥7", "♥8", "♥Q",
            "♣2", "♣6", "♣7", "♣8",
            "◆4", "◆9", "◆K"
        };

        // 한 번도 저장한 적이 없으면 기본 덱을 돌려준다. 저장한 덱은 아직 26장이 안 돼도 그대로 돌려준다(편집 중일 수 있다).
        public static List<EffectCardData> LoadEditable(EffectCardDatabase database)
        {
            if (!PlayerPrefs.HasKey(Key))
            {
                return CreateDefault(database);
            }

            string saved = PlayerPrefs.GetString(Key, string.Empty);
            var deck = new List<EffectCardData>();
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (var part in saved.Split(Separator))
                {
                    var pieces = part.Split(PartSeparator);
                    if (pieces.Length != 2 || !int.TryParse(pieces[0], out int suit) || !int.TryParse(pieces[1], out int rank))
                    {
                        continue;
                    }

                    var card = database.Find((Suit)suit, (Rank)rank);
                    if (card != null && !deck.Contains(card))
                    {
                        deck.Add(card);
                    }
                }
            }

            return deck;
        }

        // 전투용: 규칙을 모두 만족하는 완성된 덱이면 그것을, 아니면 기본 덱을 돌려준다.
        public static List<EffectCardData> LoadForBattle(EffectCardDatabase database, EffectRuleSet ruleSet)
        {
            var deck = LoadEditable(database);
            return IsComplete(deck, ruleSet) ? deck : CreateDefault(database);
        }

        public static List<EffectCardData> CreateDefault(EffectCardDatabase database)
        {
            var deck = new List<EffectCardData>();
            foreach (var code in DefaultCodes)
            {
                var card = FindByCode(database, code);
                if (card != null)
                {
                    deck.Add(card);
                }
            }

            return deck;
        }

        public static void Save(IReadOnlyList<EffectCardData> deck)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < deck.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(Separator);
                }

                builder.Append((int)deck[i].Suit).Append(PartSeparator).Append((int)deck[i].Rank);
            }

            PlayerPrefs.SetString(Key, builder.ToString());
            PlayerPrefs.Save();
        }

        // 장수가 가득 차고 영웅·전설 제한을 넘지 않는 덱인지 확인한다.
        public static bool IsComplete(IReadOnlyList<EffectCardData> deck, EffectRuleSet ruleSet)
        {
            if (deck.Count != ruleSet.DeckMax)
            {
                return false;
            }

            var rules = new EffectDeckRules(ruleSet);
            var check = new List<EffectCardData>();
            foreach (var card in deck)
            {
                if (!rules.CanAdd(check, card))
                {
                    return false;
                }

                check.Add(card);
            }

            return true;
        }

        private static EffectCardData FindByCode(EffectCardDatabase database, string code)
        {
            Suit suit;
            switch (code[0])
            {
                case '♠': suit = Suit.Spade; break;
                case '♥': suit = Suit.Heart; break;
                case '♣': suit = Suit.Clover; break;
                default: suit = Suit.Diamond; break;
            }

            string rankText = code.Substring(1);
            Rank rank;
            switch (rankText)
            {
                case "A": rank = Rank.Ace; break;
                case "J": rank = Rank.Jack; break;
                case "Q": rank = Rank.Queen; break;
                case "K": rank = Rank.King; break;
                default: rank = (Rank)int.Parse(rankText); break;
            }

            return database.Find(suit, rank);
        }
    }
}
