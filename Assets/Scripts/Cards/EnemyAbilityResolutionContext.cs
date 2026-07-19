using System;

namespace HaveABreak.Cards
{
    [Serializable]
    public readonly struct EnemyAbilityResolutionContext
    {
        public EnemyAbilityResolutionContext(
            string abilityId, string sourceEnemyId, bool isNormalAttack,
            bool affectsFriendlySide, bool isAreaAbility)
        {
            AbilityId = abilityId;
            SourceEnemyId = sourceEnemyId;
            IsNormalAttack = isNormalAttack;
            AffectsFriendlySide = affectsFriendlySide;
            IsAreaAbility = isAreaAbility;
        }

        public string AbilityId { get; }
        public string SourceEnemyId { get; }
        public bool IsNormalAttack { get; }
        public bool AffectsFriendlySide { get; }
        public bool IsAreaAbility { get; }
    }
}
