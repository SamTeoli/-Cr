using System;

namespace HaveABreak.Cards
{
    public static class EnchantFixedTargetResolver
    {
        public static bool TryDeclare(
            string sourceBattleCardId,
            string targetEnemyId,
            BattleEnemyPositionState positions,
            BattleCardEnchantRegistry enchants,
            out EnchantFixedTargetDeclaration declaration)
        {
            declaration = default;
            if (string.IsNullOrWhiteSpace(sourceBattleCardId) ||
                string.IsNullOrWhiteSpace(targetEnemyId) || positions == null)
            {
                return false;
            }

            EnemyFieldPosition? position = positions.FindPosition(targetEnemyId);
            if (!position.HasValue)
            {
                return false;
            }

            bool targetsPosition = HasActiveRoutePin(
                enchants?.Find(sourceBattleCardId));
            declaration = new EnchantFixedTargetDeclaration(
                sourceBattleCardId.Trim(),
                targetEnemyId.Trim(),
                targetsPosition,
                position.Value);
            return true;
        }

        public static string Resolve(
            EnchantFixedTargetDeclaration declaration,
            BattleEnemyPositionState positions)
        {
            if (positions == null)
            {
                return null;
            }

            if (declaration.TargetsPosition)
            {
                return positions.GetOccupant(declaration.Position);
            }

            return positions.FindPosition(declaration.DeclaredEnemyId).HasValue
                ? declaration.DeclaredEnemyId
                : null;
        }

        private static bool HasActiveRoutePin(RunCardEnchantState enchants)
        {
            if (enchants == null)
            {
                return false;
            }

            foreach (RunEnchantSlot slot in enchants.Slots)
            {
                if (!slot.IsEmpty && slot.Active && string.Equals(
                        slot.Enchant.DefinitionId,
                        "E08",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
