using UnityEngine;

namespace HaveABreak.Cards
{
    public enum EffectTargetTiming
    {
        BeforeCardPlay = 0,
        AfterPlacement = 1,
        OnActivation = 2,
        OnResolution = 3
    }

    public enum EffectTargetSide
    {
        Self = 0,
        Ally = 1,
        Enemy = 2,
        Any = 3
    }

    public enum EffectTargetKind
    {
        Monster = 0,
        Player = 1,
        Zone = 2,
        HandCard = 3,
        GraveyardCard = 4,
        SkillFieldCard = 5
    }

    public enum EffectTargetFallbackPolicy
    {
        FailEffect = 0,
        SkipStep = 1,
        AllowReselect = 2
    }

    [CreateAssetMenu(
        fileName = "EffectTargetSpec",
        menuName = "Have a Break/Cards/Effect Target Spec")]
    public sealed class EffectTargetSpec : ScriptableObject
    {
        [SerializeField] private string definitionId;
        [SerializeField] private EffectTargetTiming timing =
            EffectTargetTiming.OnActivation;
        [SerializeField] private EffectTargetSide side =
            EffectTargetSide.Enemy;
        [SerializeField] private EffectTargetKind kind =
            EffectTargetKind.Monster;
        [SerializeField, Min(0)] private int minimumCount = 1;
        [SerializeField, Min(1)] private int maximumCount = 1;
        [SerializeField] private bool optional;
        [SerializeField] private bool allowDuplicate;
        [SerializeField] private bool requireAlive = true;
        [SerializeField] private EffectTargetFallbackPolicy fallbackPolicy =
            EffectTargetFallbackPolicy.FailEffect;

        public string DefinitionId => definitionId;
        public EffectTargetTiming Timing => timing;
        public EffectTargetSide Side => side;
        public EffectTargetKind Kind => kind;
        public int MinimumCount => minimumCount;
        public int MaximumCount => maximumCount;
        public bool Optional => optional;
        public bool AllowDuplicate => allowDuplicate;
        public bool RequireAlive => requireAlive;
        public EffectTargetFallbackPolicy FallbackPolicy => fallbackPolicy;

        internal void Initialize(
            string id,
            EffectTargetTiming targetTiming,
            EffectTargetSide targetSide,
            EffectTargetKind targetKind,
            int minimum,
            int maximum,
            bool isOptional,
            bool duplicatesAllowed,
            bool aliveRequired,
            EffectTargetFallbackPolicy fallback)
        {
            definitionId = id?.Trim();
            timing = targetTiming;
            side = targetSide;
            kind = targetKind;
            minimumCount = Mathf.Max(0, minimum);
            maximumCount = Mathf.Max(1, maximum);
            optional = isOptional;
            allowDuplicate = duplicatesAllowed;
            requireAlive = aliveRequired;
            fallbackPolicy = fallback;
            Normalize();
        }

        private void OnValidate()
        {
            definitionId = definitionId?.Trim();
            Normalize();
        }

        private void Normalize()
        {
            minimumCount = Mathf.Max(0, minimumCount);
            maximumCount = Mathf.Max(1, maximumCount);
            if (maximumCount < minimumCount)
            {
                maximumCount = minimumCount;
            }
        }
    }

    public static class BuiltInEffectTargetSpecs
    {
        private static EffectTargetSpec enemyMonsterSingleAfterPlacement;
        private static EffectTargetSpec enemyMonsterSingleOnActivation;
        private static EffectTargetSpec enemyMonsterOneOrTwoOnActivation;
        private static EffectTargetSpec enemyMonsterSingleOnResolution;
        private static EffectTargetSpec allyMonsterSingleOnResolution;
        private static EffectTargetSpec handCardSingleOnActivation;
        private const string ResourceRoot =
            "GameData/EffectTargets/";

        public static EffectTargetSpec EnemyMonsterSingleAfterPlacement =>
            enemyMonsterSingleAfterPlacement ??=
                CreateEnemyMonsterSingle(
                    "TARGET-ENEMY-MONSTER-SINGLE-AFTER-PLACEMENT",
                    "EnemyMonsterSingleAfterPlacement",
                    EffectTargetTiming.AfterPlacement);

        public static EffectTargetSpec EnemyMonsterSingleOnActivation =>
            enemyMonsterSingleOnActivation ??=
                CreateEnemyMonsterSingle(
                    "TARGET-ENEMY-MONSTER-SINGLE-ON-ACTIVATION",
                    "EnemyMonsterSingleOnActivation",
                    EffectTargetTiming.OnActivation);

        public static EffectTargetSpec HandCardSingleOnActivation =>
            handCardSingleOnActivation ??= CreateSingle(
                "TARGET-HAND-CARD-SINGLE-ON-ACTIVATION",
                "HandCardSingleOnActivation",
                EffectTargetTiming.OnActivation,
                EffectTargetSide.Self,
                EffectTargetKind.HandCard,
                false);

        public static EffectTargetSpec EnemyMonsterOneOrTwoOnActivation =>
            enemyMonsterOneOrTwoOnActivation ??= Create(
                "TARGET-ENEMY-MONSTER-ONE-OR-TWO-ON-ACTIVATION",
                "EnemyMonsterOneOrTwoOnActivation",
                EffectTargetTiming.OnActivation,
                EffectTargetSide.Enemy,
                EffectTargetKind.Monster,
                1,
                2,
                false,
                false,
                true);

        public static EffectTargetSpec EnemyMonsterSingleOnResolution =>
            enemyMonsterSingleOnResolution ??=
                CreateEnemyMonsterSingle(
                    "TARGET-ENEMY-MONSTER-SINGLE-ON-RESOLUTION",
                    "EnemyMonsterSingleOnResolution",
                    EffectTargetTiming.OnResolution);

        public static EffectTargetSpec AllyMonsterSingleOnResolution =>
            allyMonsterSingleOnResolution ??= CreateSingle(
                "TARGET-ALLY-MONSTER-SINGLE-ON-RESOLUTION",
                "AllyMonsterSingleOnResolution",
                EffectTargetTiming.OnResolution,
                EffectTargetSide.Ally,
                EffectTargetKind.Monster,
                true);

        private static EffectTargetSpec CreateEnemyMonsterSingle(
            string definitionId,
            string objectName,
            EffectTargetTiming timing)
        {
            return CreateSingle(
                definitionId,
                objectName,
                timing,
                EffectTargetSide.Enemy,
                EffectTargetKind.Monster,
                true);
        }

        private static EffectTargetSpec CreateSingle(
            string definitionId,
            string objectName,
            EffectTargetTiming timing,
            EffectTargetSide side,
            EffectTargetKind kind,
            bool requireAlive)
        {
            return Create(
                definitionId,
                objectName,
                timing,
                side,
                kind,
                1,
                1,
                false,
                false,
                requireAlive);
        }

        private static EffectTargetSpec Create(
            string definitionId,
            string objectName,
            EffectTargetTiming timing,
            EffectTargetSide side,
            EffectTargetKind kind,
            int minimum,
            int maximum,
            bool optional,
            bool allowDuplicate,
            bool requireAlive)
        {
            EffectTargetSpec asset = Resources.Load<EffectTargetSpec>(
                ResourceRoot + objectName);
            if (asset != null)
            {
                return asset;
            }

            EffectTargetSpec spec = ScriptableObject
                .CreateInstance<EffectTargetSpec>();
            spec.name = objectName;
            spec.hideFlags = HideFlags.HideAndDontSave;
            spec.Initialize(
                definitionId,
                timing,
                side,
                kind,
                minimum,
                maximum,
                optional,
                allowDuplicate,
                requireAlive,
                EffectTargetFallbackPolicy.FailEffect);
            return spec;
        }
    }
}
