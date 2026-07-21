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
        [SerializeField] private EncounterDatabase encounterDatabase;
        [SerializeField] private string normalEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-01";
        [SerializeField] private string eliteEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-ELITE";
        [SerializeField] private string midBossEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-MIDBOSS";
        [SerializeField] private string finalBossEncounterId =
            "TEST-ENCOUNTER-PROTOTYPE-FINALBOSS";

        public CardDatabase CardDatabase => cardDatabase;
        public EnchantDatabase EnchantDatabase => enchantDatabase;
        public EncounterDatabase EncounterDatabase => encounterDatabase;
        public string NormalEncounterId => normalEncounterId;
        public string EliteEncounterId => eliteEncounterId;
        public string MidBossEncounterId => midBossEncounterId;
        public string FinalBossEncounterId => finalBossEncounterId;
        public bool IsReady => cardDatabase != null && enchantDatabase != null &&
                               encounterDatabase != null;
    }
}
