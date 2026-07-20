using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class BattleRuntimeState
    {
        [SerializeField] private BattleDeckState deck;
        [SerializeField] private BattleCardEnchantRegistry enchants;
        [SerializeField] private BattleNextSkillModifierState nextSkillModifiers;
        [SerializeField] private BattleCardPlayState cardPlay;
        [SerializeField] private BattleTurnState turn;
        [SerializeField] private BattleMonsterRegistry monsters;
        [SerializeField] private BattleEnemyTracker livingEnemies;
        [SerializeField] private BattleEnemyPositionState enemyPositions;
        [SerializeField] private BattleEnemyMovementLockState enemyMovementLocks;
        [SerializeField] private BattleEnemyStatusRegistry enemyStatuses;
        [SerializeField] private List<BattleEnemyRuntimeState> enemies;
        [SerializeField] private BattleEventLog eventLog;
        [SerializeField] private BattleEffectResolutionTracker effectResolutions;
        [SerializeField] private BattleCardTurnTriggerState cardTurnTriggers;
        [SerializeField] private BattleDefenseRetentionState defenseRetention;
        [SerializeField] private BattleRuntimeTrapRegistry trapInstallations;

        private BattleRuntimeState()
        {
        }

        public BattleRuntimeState(
            IEnumerable<BattleCardInstance> deckCards,
            int shuffleSeed,
            int maximumMana = BattleManaState.DefaultMaximumMana)
        {
            enchants = new BattleCardEnchantRegistry();
            nextSkillModifiers = new BattleNextSkillModifierState();
            deck = new BattleDeckState(
                deckCards ?? throw new ArgumentNullException(nameof(deckCards)),
                shuffleSeed);
            cardPlay = new BattleCardPlayState(
                deck, maximumMana, enchants, nextSkillModifiers);
            turn = new BattleTurnState(cardPlay);
            monsters = new BattleMonsterRegistry();
            livingEnemies = new BattleEnemyTracker();
            enemyPositions = new BattleEnemyPositionState();
            enemyMovementLocks = new BattleEnemyMovementLockState();
            enemyStatuses = new BattleEnemyStatusRegistry();
            enemies = new List<BattleEnemyRuntimeState>();
            eventLog = new BattleEventLog();
            effectResolutions = new BattleEffectResolutionTracker();
            cardTurnTriggers = new BattleCardTurnTriggerState();
            defenseRetention = new BattleDefenseRetentionState();
            trapInstallations = new BattleRuntimeTrapRegistry();
        }

        public BattleDeckState Deck => deck;
        public BattleCardEnchantRegistry Enchants => enchants;
        public BattleNextSkillModifierState NextSkillModifiers => nextSkillModifiers;
        public BattleCardPlayState CardPlay => cardPlay;
        public BattleTurnState Turn => turn;
        public BattleMonsterRegistry Monsters => monsters;
        public BattleEnemyTracker LivingEnemies => livingEnemies;
        public BattleEnemyPositionState EnemyPositions => enemyPositions;
        public BattleEnemyMovementLockState EnemyMovementLocks => enemyMovementLocks;
        public BattleEnemyStatusRegistry EnemyStatuses => enemyStatuses;
        public IReadOnlyList<BattleEnemyRuntimeState> Enemies => enemies;
        public BattleEventLog EventLog => eventLog;
        public BattleEffectResolutionTracker EffectResolutions => effectResolutions;
        public BattleCardTurnTriggerState CardTurnTriggers => cardTurnTriggers;
        public BattleDefenseRetentionState DefenseRetention => defenseRetention;
        public BattleRuntimeTrapRegistry TrapInstallations => trapInstallations;

        public bool TryAddEnemy(
            string enemyId,
            int attack,
            int health,
            EnemyFieldPosition position,
            out BattleEnemyRuntimeState enemy)
        {
            enemy = null;
            if (string.IsNullOrWhiteSpace(enemyId) || health <= 0 ||
                livingEnemies.Contains(enemyId) ||
                enemyStatuses.Find(enemyId) != null ||
                enemyPositions.FindPosition(enemyId).HasValue ||
                !string.IsNullOrWhiteSpace(enemyPositions.GetOccupant(position)))
            {
                return false;
            }

            enemy = new BattleEnemyRuntimeState(enemyId, attack, health);
            if (!livingEnemies.TryAdd(enemy.EnemyId) ||
                !enemyStatuses.TryAdd(enemy.EnemyId, out _) ||
                !enemyPositions.TryPlace(enemy.EnemyId, position))
            {
                return false;
            }

            enemies.Add(enemy);
            return true;
        }

        public BattleEnemyRuntimeState FindEnemy(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return null;
            }

            return enemies.Find(item => item != null && string.Equals(
                item.EnemyId, enemyId, StringComparison.OrdinalIgnoreCase));
        }

        public bool TryRegisterFieldMonster(
            string battleCardId,
            out BattleMonsterState monster)
        {
            monster = null;
            BattleCardInstance card = deck.Zones.Find(battleCardId);
            return card != null &&
                   card.Zone == CardZone.MonsterField &&
                   monsters.TryAdd(
                       card, enchants.Find(battleCardId), out monster);
        }
    }
}
