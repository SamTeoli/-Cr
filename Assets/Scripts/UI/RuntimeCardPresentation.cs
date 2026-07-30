using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    public sealed class RuntimeCardPresentation
    {
        public RuntimeCardPresentation(
            string commandId,
            string displayName,
            string contentId,
            CardType cardType,
            CardRarity rarity,
            int manaCost,
            int? attack,
            int? health,
            string effectText,
            bool selected,
            int selectionOrder,
            bool interactable,
            string blockReason,
            string accessibilityText,
            Sprite artwork = null,
            bool requiresEnemyTarget = false)
        {
            CommandId = commandId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? "이름 없는 카드"
                : displayName;
            ContentId = contentId ?? string.Empty;
            CardType = cardType;
            Rarity = rarity;
            ManaCost = Mathf.Max(0, manaCost);
            Attack = attack;
            Health = health;
            EffectText = effectText ?? string.Empty;
            Selected = selected;
            SelectionOrder = Mathf.Max(0, selectionOrder);
            Interactable = interactable;
            BlockReason = blockReason ?? string.Empty;
            AccessibilityText = accessibilityText ?? string.Empty;
            Artwork = artwork;
            RequiresEnemyTarget = requiresEnemyTarget;
        }

        public string CommandId { get; }
        public string DisplayName { get; }
        public string ContentId { get; }
        public CardType CardType { get; }
        public CardRarity Rarity { get; }
        public int ManaCost { get; }
        public int? Attack { get; }
        public int? Health { get; }
        public string EffectText { get; }
        public bool Selected { get; }
        public int SelectionOrder { get; }
        public bool Interactable { get; }
        public string BlockReason { get; }
        public string AccessibilityText { get; }
        public Sprite Artwork { get; }
        public bool RequiresEnemyTarget { get; }
        public bool HasMonsterStats => Attack.HasValue && Health.HasValue;

        public RuntimeCardPresentation WithInteraction(
            string commandId,
            bool selected,
            bool interactable,
            string blockReason,
            string accessibilitySuffix = null)
        {
            string accessibility = AccessibilityText;
            if (!string.IsNullOrWhiteSpace(accessibilitySuffix))
            {
                accessibility = string.IsNullOrWhiteSpace(accessibility)
                    ? accessibilitySuffix
                    : $"{accessibility} {accessibilitySuffix}";
            }

            return new RuntimeCardPresentation(
                commandId,
                DisplayName,
                ContentId,
                CardType,
                Rarity,
                ManaCost,
                Attack,
                Health,
                EffectText,
                selected,
                selected ? 1 : 0,
                interactable,
                blockReason,
                accessibility,
                Artwork,
                RequiresEnemyTarget);
        }

        public string TypeLabel => CardType switch
        {
            CardType.Monster => "몬스터",
            CardType.Skill => "스킬",
            CardType.Trap => "트랩",
            CardType.Barrier => "결계",
            _ => CardType.ToString()
        };

        private string LegacyTypeLabel => CardType switch
        {
            CardType.Monster => "몬스터",
            CardType.Skill => "스킬",
            CardType.Trap => "함정",
            CardType.Barrier => "장벽",
            _ => CardType.ToString()
        };

        public string RarityLabel => Rarity switch
        {
            CardRarity.Common => "일반",
            CardRarity.Rare => "희귀",
            CardRarity.Legendary => "전설",
            _ => Rarity.ToString()
        };

        public Color TypeColor => CardType switch
        {
            CardType.Monster => new Color(0.34f, 0.16f, 0.08f, 1f),
            CardType.Skill => new Color(0.05f, 0.18f, 0.34f, 1f),
            CardType.Trap => new Color(0.34f, 0.055f, 0.07f, 1f),
            CardType.Barrier => new Color(0.82f, 0.81f, 0.75f, 1f),
            _ => new Color(0.2f, 0.24f, 0.3f, 1f)
        };

        public Color RarityColor => Rarity switch
        {
            CardRarity.Common => new Color(0.42f, 0.43f, 0.45f, 1f),
            CardRarity.Rare => new Color(0.82f, 0.88f, 0.94f, 1f),
            CardRarity.Legendary => new Color(0.96f, 0.68f, 0.18f, 1f),
            _ => Color.white
        };

        public static RuntimeCardPresentation FromRunDeck(
            RunDeckSelectionOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            CardData card = option.Card.Card;
            CardLevelData level = card.GetLevelData(option.CurrentLevel);
            int manaCost = level?.ManaCost ?? card.ManaCost;
            int attack = level?.Attack ??
                         (card is MonsterCardData monster ? monster.Attack : 0);
            int health = level?.Health ??
                         (card is MonsterCardData fallbackMonster
                             ? fallbackMonster.Health
                             : 0);
            string rules = CardEffectTextFormatter.BuildCardRulesText(
                card,
                card.ResolveRulesText(option.CurrentLevel),
                option.CurrentLevel);
            bool isMonster = card.CardType == CardType.Monster;
            string accessibility =
                $"{card.DisplayName}, {card.CardType}, {card.Rarity}, " +
                $"마력 {manaCost}, 레벨 {option.CurrentLevel}";

            return new RuntimeCardPresentation(
                option.OwnedCardId,
                card.DisplayName,
                card.CatalogCardId,
                card.CardType,
                card.Rarity,
                manaCost,
                isMonster ? attack : null,
                isMonster ? health : null,
                rules,
                option.IsSelected,
                option.SelectionOrder,
                true,
                null,
                accessibility,
                card.Artwork);
        }

        public static RuntimeCardPresentation FromBattleHand(
            BattleHandCardActionOption option)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            BattleCardInstance card = option.Card;
            ResolvedCardData resolved = card.Resolved;
            bool isMonster = card.SourceCard.CardType == CardType.Monster;
            string blockReason = option.BlockReason ?? string.Empty;
            string accessibility =
                $"{card.SourceCard.DisplayName}, {card.SourceCard.CardType}, " +
                $"{card.SourceCard.Rarity}, 마력 {resolved.ManaCost}. " +
                $"{resolved.RulesText}";
            if (!string.IsNullOrWhiteSpace(blockReason))
            {
                accessibility += $" 사용 불가: {blockReason}";
            }
            bool requiresEnemyTarget =
                CardEffectRegistrationCatalog.TryFind(
                    card.SourceCard.CatalogCardId,
                    out CardEffectRegistration registration) &&
                registration.Route == CardEffectRoute.TargetedSkill;

            return new RuntimeCardPresentation(
                $"play:{option.BattleCardId}",
                card.SourceCard.DisplayName,
                card.SourceCard.CatalogCardId,
                card.SourceCard.CardType,
                card.SourceCard.Rarity,
                resolved.ManaCost,
                isMonster ? resolved.Attack : null,
                isMonster ? resolved.Health : null,
                resolved.RulesText,
                false,
                0,
                option.CanPlay,
                blockReason,
                accessibility,
                card.SourceCard.Artwork,
                requiresEnemyTarget);
        }
    }
}
