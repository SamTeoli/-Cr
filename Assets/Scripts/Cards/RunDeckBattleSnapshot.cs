using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class RunDeckBattleSnapshot
    {
        [SerializeField] private string battleInstanceId;
        [SerializeField] private List<Binding> bindings = new();
        [SerializeField] private List<BattleCardInstance> cards = new();

        private RunDeckBattleSnapshot()
        {
        }

        [Serializable]
        private sealed class Binding
        {
            [SerializeField] private RunCardInstance runCard;
            [SerializeField] private BattleCardInstance battleCard;

            public Binding(
                RunCardInstance runCard,
                BattleCardInstance battleCard)
            {
                this.runCard = runCard;
                this.battleCard = battleCard;
            }

            public RunCardInstance RunCard => runCard;
            public BattleCardInstance BattleCard => battleCard;
        }

        internal RunDeckBattleSnapshot(
            string battleInstanceId,
            IReadOnlyList<RunCardInstance> runCards)
        {
            this.battleInstanceId = battleInstanceId;
            foreach (RunCardInstance runCard in runCards)
            {
                BattleCardInstance battleCard = new(
                    runCard.Card,
                    new CardInstanceIds(
                        runCard.CatalogCardId,
                        runCard.OwnedCardId,
                        BuildBattleCardId(
                            battleInstanceId,
                            runCard.OwnedCardId)),
                    runCard.CurrentLevel,
                    CardZone.DrawPile);
                cards.Add(battleCard);
                bindings.Add(new Binding(runCard, battleCard));
            }
        }

        public string BattleInstanceId => battleInstanceId;
        public IReadOnlyList<BattleCardInstance> Cards => cards;

        public RunCardInstance FindRunCard(string battleCardId)
        {
            if (string.IsNullOrWhiteSpace(battleCardId))
            {
                return null;
            }

            Binding binding = bindings.Find(item => item != null &&
                item.BattleCard != null && string.Equals(
                    item.BattleCard.Ids.BattleCardId,
                    battleCardId,
                    StringComparison.OrdinalIgnoreCase));
            return binding?.RunCard;
        }

        public bool TryRegisterEnchants(
            BattleRuntimeState runtime,
            out RunDeckFailure failure)
        {
            if (runtime?.Deck == null || runtime.Enchants == null)
            {
                failure = RunDeckFailure.InvalidRuntime;
                return false;
            }

            foreach (Binding binding in bindings)
            {
                if (binding?.RunCard?.Enchants == null ||
                    binding.BattleCard == null ||
                    runtime.Deck.Zones.Find(
                        binding.BattleCard.Ids.BattleCardId) !=
                    binding.BattleCard ||
                    runtime.Enchants.Find(
                        binding.BattleCard.Ids.BattleCardId) != null)
                {
                    failure = RunDeckFailure.EnchantRegistrationFailed;
                    return false;
                }
            }

            foreach (Binding binding in bindings)
            {
                if (!runtime.Enchants.TryRegister(
                        binding.BattleCard,
                        binding.RunCard.Enchants))
                {
                    failure = RunDeckFailure.EnchantRegistrationFailed;
                    return false;
                }
            }

            failure = RunDeckFailure.None;
            return true;
        }

        internal static string BuildBattleCardId(
            string battleInstanceId,
            string ownedCardId)
        {
            return $"{battleInstanceId}:{ownedCardId}";
        }
    }
}
