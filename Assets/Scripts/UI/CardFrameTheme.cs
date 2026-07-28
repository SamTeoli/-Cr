using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "CardFrameTheme",
        menuName = "Have a Break/UI/Card Frame Theme")]
    public sealed class CardFrameTheme : ScriptableObject
    {
        [Serializable]
        public sealed class RarityFrame
        {
            [SerializeField] private CardRarity rarity;
            [SerializeField] private Sprite frameSprite;
            [SerializeField] private Color fallbackColor = Color.white;
            public CardRarity Rarity => rarity;
            public Sprite FrameSprite => frameSprite;
            public Color FallbackColor => fallbackColor;
#if UNITY_EDITOR
            public void EditorInitialize(CardRarity value, Color color)
            {
                rarity = value;
                fallbackColor = color;
            }

            public void EditorAssignSprite(Sprite value)
            {
                frameSprite = value;
            }
#endif
        }

        [Header("Monster Frames")]
        [SerializeField] private RarityFrame common = new();
        [SerializeField] private RarityFrame rare = new();
        [SerializeField] private RarityFrame legendary = new();

        [Header("Skill, Trap, and Barrier Frames")]
        [SerializeField] private RarityFrame nonMonsterCommon = new();
        [SerializeField] private RarityFrame nonMonsterRare = new();
        [SerializeField] private RarityFrame nonMonsterLegendary = new();

        public RarityFrame GetFrame(CardRarity rarity)
        {
            return GetFrame(rarity, CardType.Monster);
        }

        public RarityFrame GetFrame(CardRarity rarity, CardType cardType)
        {
            bool usesMonsterStats = cardType == CardType.Monster;
            return rarity switch
            {
                CardRarity.Rare => usesMonsterStats ? rare : nonMonsterRare,
                CardRarity.Legendary => usesMonsterStats
                    ? legendary
                    : nonMonsterLegendary,
                _ => usesMonsterStats ? common : nonMonsterCommon
            };
        }

#if UNITY_EDITOR
        public void EditorInitializeDefaults()
        {
            common.EditorInitialize(CardRarity.Common,
                new Color(0.23f, 0.24f, 0.25f, 1f));
            rare.EditorInitialize(CardRarity.Rare,
                new Color(0.18f, 0.48f, 0.72f, 1f));
            legendary.EditorInitialize(CardRarity.Legendary,
                new Color(0.82f, 0.55f, 0.12f, 1f));
            nonMonsterCommon.EditorInitialize(CardRarity.Common,
                new Color(0.23f, 0.24f, 0.25f, 1f));
            nonMonsterRare.EditorInitialize(CardRarity.Rare,
                new Color(0.18f, 0.48f, 0.72f, 1f));
            nonMonsterLegendary.EditorInitialize(CardRarity.Legendary,
                new Color(0.82f, 0.55f, 0.12f, 1f));
        }

        public void EditorAssignFrames(
            Sprite monsterCommon,
            Sprite monsterRare,
            Sprite monsterLegendary,
            Sprite sharedCommon,
            Sprite sharedRare,
            Sprite sharedLegendary)
        {
            common.EditorAssignSprite(monsterCommon);
            rare.EditorAssignSprite(monsterRare);
            legendary.EditorAssignSprite(monsterLegendary);
            nonMonsterCommon.EditorAssignSprite(sharedCommon);
            nonMonsterRare.EditorAssignSprite(sharedRare);
            nonMonsterLegendary.EditorAssignSprite(sharedLegendary);
        }
#endif
    }
}
