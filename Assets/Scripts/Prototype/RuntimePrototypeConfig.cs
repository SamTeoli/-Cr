using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(
        fileName = "RuntimePrototypeConfig",
        menuName = "Have a Break/Runtime Prototype Config")]
    public sealed class RuntimePrototypeConfig : ScriptableObject
    {
        [SerializeField] private CardDatabase cardDatabase;
        [SerializeField] private EnchantDatabase enchantDatabase;
        [SerializeField] private ConsumableDatabase consumableDatabase;
        [SerializeField] private SituationEventDatabase situationEventDatabase;
        [SerializeField] private RunNodeGenerationConfig runNodeGenerationConfig;
        [SerializeField] private ShopEconomyConfig shopEconomyConfig;
        [SerializeField] private RestUpgradeConfig restUpgradeConfig;
        [SerializeField] private BattleRewardConfig battleRewardConfig;
        [SerializeField] private EncounterDatabase encounterDatabase;
        [SerializeField] private RunEncounterProgressionConfig encounterProgressionConfig;

        public CardDatabase CardDatabase => cardDatabase;
        public EnchantDatabase EnchantDatabase => enchantDatabase;
        public ConsumableDatabase ConsumableDatabase => consumableDatabase;
        public SituationEventDatabase SituationEventDatabase => situationEventDatabase;
        public RunNodeGenerationConfig RunNodeGenerationConfig =>
            runNodeGenerationConfig;
        public ShopEconomyConfig ShopEconomyConfig => shopEconomyConfig;
        public RestUpgradeConfig RestUpgradeConfig => restUpgradeConfig;
        public BattleRewardConfig BattleRewardConfig => battleRewardConfig;
        public EncounterDatabase EncounterDatabase => encounterDatabase;
        public RunEncounterProgressionConfig EncounterProgressionConfig =>
            encounterProgressionConfig;
        public IReadOnlyList<string> GetEncounterPool(BattleEncounterGrade grade,
            int nodeIndex = 0)
        {
            return encounterProgressionConfig != null &&
                   encounterProgressionConfig.TryGetPool(grade, nodeIndex, out var pool)
                ? pool : Array.Empty<string>();
        }

        public List<string> GetEncounterPoolValidationErrors()
        {
            return encounterProgressionConfig == null
                ? new List<string> { "Encounter progression config is missing." }
                : encounterProgressionConfig.GetValidationErrors(encounterDatabase);
        }

        public bool IsReady => cardDatabase != null && enchantDatabase != null &&
                               consumableDatabase != null &&
                               situationEventDatabase != null &&
                               runNodeGenerationConfig != null &&
                               runNodeGenerationConfig.GetValidationErrors().Count == 0 &&
                               shopEconomyConfig != null &&
                               shopEconomyConfig.GetValidationErrors().Count == 0 &&
                               restUpgradeConfig != null &&
                               restUpgradeConfig.GetValidationErrors().Count == 0 &&
                               battleRewardConfig != null &&
                               battleRewardConfig.GetValidationErrors().Count == 0 &&
                               encounterDatabase != null &&
                               GetEncounterPoolValidationErrors().Count == 0;
    }
}
