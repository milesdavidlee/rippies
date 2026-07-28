using System;
using UnityEngine;

namespace Rippies.Reveal
{
    public enum RipState
    {
        Loading,
        Presenting,
        Ready,
        Grabbing,
        Tearing,
        SealBroken,
        Opening,
        Revealing,
        Complete,
        Recovery
    }

    [Serializable]
    public sealed class CardPayload
    {
        public string id = "card_demo_001";
        public string name = "Neon Warden";
        public string grade = "Prototype 001";
        public string rarityTier = "rare";
        public string archetype = "Guardian";
        public string accentHex = "#37F4D1";
        public string flavorText = "Protect the signal.";
        public int attack = 72;
        public int defense = 88;
        public int speed = 64;
        public int luck = 79;
        public string frontImageUrl = "";
        public string backImageUrl = "";
    }

    [Serializable]
    public sealed class RevealPayload
    {
        public string orderId = "ord_demo_001";
        public string revealId = "rev_demo_001";
        public string packTypeId = "rippies_genesis";
        public string assetVersion = "1";
        public string presentationMode = "reveal";
        public string inspectionCardId = "";
        public CardPayload[] cards = Array.Empty<CardPayload>();
        public CardPayload card = new CardPayload();
        public string receiptSignature = "local-demo";

        public CardPayload[] Cards =>
            cards != null && cards.Length > 0
                ? cards
                : card == null
                    ? Array.Empty<CardPayload>()
                    : new[] { card };

        public CardPayload PrimaryCard =>
            Cards.Length > 0 ? Cards[0] : card;

        public bool IsInspectionMode =>
            string.Equals(
                presentationMode,
                "inspection",
                StringComparison.OrdinalIgnoreCase);

        public CardPayload InspectionCard
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(inspectionCardId))
                {
                    foreach (CardPayload candidate in Cards)
                    {
                        if (candidate != null &&
                            string.Equals(
                                candidate.id,
                                inspectionCardId,
                                StringComparison.Ordinal))
                        {
                            return candidate;
                        }
                    }
                }

                return PrimaryCard;
            }
        }

        public CardPayload[] PresentationCards
        {
            get
            {
                CardPayload[] source = Cards;
                if (!IsInspectionMode || source.Length <= 1)
                {
                    return source;
                }

                CardPayload selected = InspectionCard;
                var ordered = new CardPayload[source.Length];
                ordered[0] = selected;
                int outputIndex = 1;
                foreach (CardPayload candidate in source)
                {
                    if (candidate == null || ReferenceEquals(candidate, selected))
                    {
                        continue;
                    }

                    ordered[outputIndex++] = candidate;
                }

                if (outputIndex == ordered.Length)
                {
                    return ordered;
                }

                Array.Resize(ref ordered, outputIndex);
                return ordered;
            }
        }

        public static RevealPayload FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new RevealPayload();
            }

            try
            {
                RevealPayload payload = JsonUtility.FromJson<RevealPayload>(json);
                return payload ?? new RevealPayload();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Invalid reveal payload. Using demo data. " + exception.Message);
                return new RevealPayload();
            }
        }
    }

    public static class DemoCardFactory
    {
        private static readonly string[] Adjectives =
        {
            "Neon", "Solar", "Void", "Prism", "Chrome", "Quantum", "Midnight", "Turbo"
        };

        private static readonly string[] Subjects =
        {
            "Warden", "Fox", "Ronin", "Oracle", "Titan", "Phantom", "Racer", "Dragon"
        };

        private static readonly string[] Archetypes =
        {
            "Guardian", "Striker", "Mystic", "Velocity", "Wildcard"
        };

        private static readonly string[] Flavor =
        {
            "Protect the signal.", "Outrun the impossible.", "Luck favors the luminous.",
            "Built beyond the edge.", "Every pull changes the story.", "Nothing stays sealed forever."
        };

        private static readonly string[] AccentPalette =
        {
            "#37F4D1", "#4DA3FF", "#B96CFF", "#FF4FD8", "#FFB84D",
            "#FF5C5C", "#F4F15A", "#62E36F", "#FF7A45", "#54D8FF"
        };

        private static int lastAccentIndex = -1;

        private static int sequence;

        public static RevealPayload CreateRandom()
        {
            sequence++;
            int seed = unchecked(Environment.TickCount * 397 ^ sequence * 7919);
            var random = new System.Random(seed);
            string stamp = DateTime.UtcNow.ToString("HHmmssfff");
            var cards = new CardPayload[5];
            for (int index = 0; index < cards.Length; index++)
            {
                cards[index] = CreateCard(random, stamp, index);
            }

            return new RevealPayload
            {
                orderId = "ord_demo_" + stamp,
                revealId = "rev_demo_" + stamp + "_" + sequence,
                packTypeId = "rippies_genesis",
                assetVersion = "prototype-3-five-card",
                cards = cards,
                card = cards[0],
                receiptSignature = "local-random-demo"
            };
        }

        private static CardPayload CreateCard(
            System.Random random,
            string stamp,
            int index)
        {
            string rarity = RollRarity(random);
            int rarityBonus = rarity == "grail" ? 24 :
                rarity == "legendary" ? 18 :
                rarity == "epic" ? 12 :
                rarity == "rare" ? 6 : 0;
            return new CardPayload
            {
                id = "card_demo_" + stamp + "_" + sequence + "_" + (index + 1),
                name = Adjectives[random.Next(Adjectives.Length)] + " " +
                    Subjects[random.Next(Subjects.Length)],
                grade = "PROTOTYPE " + random.Next(1, 1000).ToString("000"),
                rarityTier = rarity,
                archetype = Archetypes[random.Next(Archetypes.Length)],
                accentHex = AccentFor(random),
                flavorText = Flavor[random.Next(Flavor.Length)],
                attack = RollStat(random, rarityBonus),
                defense = RollStat(random, rarityBonus),
                speed = RollStat(random, rarityBonus),
                luck = RollStat(random, rarityBonus)
            };
        }

        private static int RollStat(System.Random random, int bonus)
        {
            return Mathf.Clamp(random.Next(42, 82) + bonus, 1, 99);
        }

        private static string RollRarity(System.Random random)
        {
            int roll = random.Next(100);
            if (roll >= 97) return "grail";
            if (roll >= 88) return "legendary";
            if (roll >= 70) return "epic";
            if (roll >= 42) return "rare";
            return "common";
        }

        private static string AccentFor(System.Random random)
        {
            int index = random.Next(AccentPalette.Length - 1);
            if (index >= lastAccentIndex)
            {
                index++;
            }

            index %= AccentPalette.Length;
            lastAccentIndex = index;
            return AccentPalette[index];
        }
    }
}
