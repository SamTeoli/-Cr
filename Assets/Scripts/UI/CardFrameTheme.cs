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

        [Header("Skill Frames")]
        [SerializeField] private RarityFrame nonMonsterCommon = new();
        [SerializeField] private RarityFrame nonMonsterRare = new();
        [SerializeField] private RarityFrame nonMonsterLegendary = new();

        [Header("Trap Frames")]
        [SerializeField] private RarityFrame trapCommon = new();
        [SerializeField] private RarityFrame trapRare = new();
        [SerializeField] private RarityFrame trapLegendary = new();

        [Header("Barrier Frames")]
        [SerializeField] private RarityFrame barrierCommon = new();
        [SerializeField] private RarityFrame barrierRare = new();
        [SerializeField] private RarityFrame barrierLegendary = new();

        public RarityFrame GetFrame(CardRarity rarity)
        {
            return GetFrame(rarity, CardType.Monster);
        }

        public RarityFrame GetFrame(CardRarity rarity, CardType cardType)
        {
            RarityFrame commonFrame;
            RarityFrame rareFrame;
            RarityFrame legendaryFrame;
            switch (cardType)
            {
                case CardType.Skill:
                    commonFrame = nonMonsterCommon;
                    rareFrame = nonMonsterRare;
                    legendaryFrame = nonMonsterLegendary;
                    break;
                case CardType.Trap:
                    commonFrame = trapCommon;
                    rareFrame = trapRare;
                    legendaryFrame = trapLegendary;
                    break;
                case CardType.Barrier:
                    commonFrame = barrierCommon;
                    rareFrame = barrierRare;
                    legendaryFrame = barrierLegendary;
                    break;
                default:
                    commonFrame = common;
                    rareFrame = rare;
                    legendaryFrame = legendary;
                    break;
            }

            return rarity switch
            {
                CardRarity.Rare => rareFrame,
                CardRarity.Legendary => legendaryFrame,
                _ => commonFrame
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
            trapCommon.EditorInitialize(CardRarity.Common, Color.white);
            trapRare.EditorInitialize(CardRarity.Rare, Color.white);
            trapLegendary.EditorInitialize(CardRarity.Legendary, Color.white);
            barrierCommon.EditorInitialize(CardRarity.Common, Color.white);
            barrierRare.EditorInitialize(CardRarity.Rare, Color.white);
            barrierLegendary.EditorInitialize(CardRarity.Legendary, Color.white);
        }

        public void EditorAssignFrames(
            Sprite monsterCommon,
            Sprite monsterRare,
            Sprite monsterLegendary,
            Sprite sharedCommon,
            Sprite sharedRare,
            Sprite sharedLegendary,
            Sprite trapCommonSprite,
            Sprite trapRareSprite,
            Sprite trapLegendarySprite,
            Sprite barrierCommonSprite,
            Sprite barrierRareSprite,
            Sprite barrierLegendarySprite)
        {
            common.EditorAssignSprite(monsterCommon);
            rare.EditorAssignSprite(monsterRare);
            legendary.EditorAssignSprite(monsterLegendary);
            nonMonsterCommon.EditorAssignSprite(sharedCommon);
            nonMonsterRare.EditorAssignSprite(sharedRare);
            nonMonsterLegendary.EditorAssignSprite(sharedLegendary);
            trapCommon.EditorAssignSprite(trapCommonSprite);
            trapRare.EditorAssignSprite(trapRareSprite);
            trapLegendary.EditorAssignSprite(trapLegendarySprite);
            barrierCommon.EditorAssignSprite(barrierCommonSprite);
            barrierRare.EditorAssignSprite(barrierRareSprite);
            barrierLegendary.EditorAssignSprite(barrierLegendarySprite);
        }
#endif
    }
}
