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
        public CardPayload card = new CardPayload();
        public string receiptSignature = "local-demo";

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
            string rarity = RollRarity(random);
            int rarityBonus = rarity == "grail" ? 24 :
                rarity == "legendary" ? 18 :
                rarity == "epic" ? 12 :
                rarity == "rare" ? 6 : 0;
            string stamp = DateTime.UtcNow.ToString("HHmmssfff");

            var card = new CardPayload
            {
                id = "card_demo_" + stamp + "_" + sequence,
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

            return new RevealPayload
            {
                orderId = "ord_demo_" + stamp,
                revealId = "rev_demo_" + stamp + "_" + sequence,
                packTypeId = "rippies_genesis",
                assetVersion = "prototype-2",
                card = card,
                receiptSignature = "local-random-demo"
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
