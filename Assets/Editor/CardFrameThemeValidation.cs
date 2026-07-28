using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

public static class CardFrameThemeValidation
{
    [MenuItem("Have a Break/Validation/Validate Card Frame Theme")]
    public static void Validate()
    {
        CardFrameTheme theme =
            Resources.Load<CardFrameTheme>("UI/CardFrameTheme");
        Require(theme != null, "CardFrameTheme resource is missing.");

        CardRarity[] rarities =
        {
            CardRarity.Common,
            CardRarity.Rare,
            CardRarity.Legendary
        };
        foreach (CardRarity rarity in rarities)
        {
            Require(theme.GetFrame(rarity, CardType.Monster).FrameSprite != null,
                $"Monster {rarity} frame is missing.");
            Require(theme.GetFrame(rarity, CardType.Skill).FrameSprite != null,
                $"Non-monster {rarity} frame is missing.");
            Require(
                theme.GetFrame(rarity, CardType.Skill).FrameSprite ==
                theme.GetFrame(rarity, CardType.Trap).FrameSprite,
                $"Skill and Trap {rarity} frames must share the same sprite.");
            Require(
                theme.GetFrame(rarity, CardType.Trap).FrameSprite ==
                theme.GetFrame(rarity, CardType.Barrier).FrameSprite,
                $"Trap and Barrier {rarity} frames must share the same sprite.");
        }

        Debug.Log("Card frame theme validation passed: all six frames applied.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new System.InvalidOperationException(message);
        }
    }
}
