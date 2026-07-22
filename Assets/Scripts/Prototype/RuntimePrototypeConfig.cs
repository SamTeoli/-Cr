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
        [SerializeField] private EncounterDatabase encounterDatabase;
        [SerializeField] private List<string> normalEncounterIds = new();
        [SerializeField] private List<string> eliteEncounterIds = new();
        [SerializeField] private List<string> midBossEncounterIds = new();
        [SerializeField] private List<string> finalBossEncounterIds = new();

        public CardDatabase CardDatabase => cardDatabase;
        public EnchantDatabase EnchantDatabase => enchantDatabase;
        public ConsumableDatabase ConsumableDatabase => consumableDatabase;
        public SituationEventDatabase SituationEventDatabase => situationEventDatabase;
        public EncounterDatabase EncounterDatabase => encounterDatabase;
        public IReadOnlyList<string> GetEncounterPool(BattleEncounterGrade grade)
        {
            IReadOnlyList<string> pool = grade switch
            {
                BattleEncounterGrade.Elite => eliteEncounterIds,
                BattleEncounterGrade.MidBoss => midBossEncounterIds,
                BattleEncounterGrade.FinalBoss => finalBossEncounterIds,
                _ => normalEncounterIds
            };
            return pool ?? Array.Empty<string>();
        }

        public List<string> GetEncounterPoolValidationErrors()
        {
            return RunEncounterPoolService.Validate(
                encounterDatabase,
                new Dictionary<BattleEncounterGrade, IReadOnlyList<string>>
                {
                    [BattleEncounterGrade.Normal] = normalEncounterIds,
                    [BattleEncounterGrade.Elite] = eliteEncounterIds,
                    [BattleEncounterGrade.MidBoss] = midBossEncounterIds,
                    [BattleEncounterGrade.FinalBoss] = finalBossEncounterIds
                });
        }

        public bool IsReady => cardDatabase != null && enchantDatabase != null &&
                               consumableDatabase != null &&
                               situationEventDatabase != null &&
                               encounterDatabase != null &&
                               GetEncounterPoolValidationErrors().Count == 0;
    }
}
