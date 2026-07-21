using System;

namespace HaveABreak.Cards
{
    [Serializable]
    public readonly struct EnemyAbilityResolutionContext
    {
        public EnemyAbilityResolutionContext(
            string abilityId, string sourceEnemyId, bool isNormalAttack,
            bool affectsFriendlySide, bool isAreaAbility,
            StatusKeyword statusKeyword =
                global::HaveABreak.Cards.StatusKeyword.None,
            int statusAmount = 0,
            int targetTieBreakerValue = 0)
        {
            AbilityId = abilityId;
            SourceEnemyId = sourceEnemyId;
            IsNormalAttack = isNormalAttack;
            AffectsFriendlySide = affectsFriendlySide;
            IsAreaAbility = isAreaAbility;
            StatusKeyword = statusKeyword;
            StatusAmount = statusAmount;
            TargetTieBreakerValue = targetTieBreakerValue;
        }

        public string AbilityId { get; }
        public string SourceEnemyId { get; }
        public bool IsNormalAttack { get; }
        public bool AffectsFriendlySide { get; }
        public bool IsAreaAbility { get; }
        public StatusKeyword StatusKeyword { get; }
        public int StatusAmount { get; }
        public int TargetTieBreakerValue { get; }
        public bool HasStatusEffect =>
            StatusKeyword !=
            global::HaveABreak.Cards.StatusKeyword.None &&
            StatusAmount > 0;
    }
}
