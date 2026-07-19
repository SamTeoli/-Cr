using System;

namespace HaveABreak.Cards
{
    public static class C07LostTicketResolver
    {
        private const string EffectId = "C07-MAIN";

        public static bool TryResolve(
            BattleEventRecord playedEvent,
            BattleCardInstance sourceSkill,
            string selectedBanishBattleCardId,
            BattleDeckState deck,
            BattleMonsterRegistry monsters,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int drawnCount,
            out bool banished,
            out int defendedMonsterCount)
        {
            drawnCount = 0;
            banished = false;
            defendedMonsterCount = 0;
            if (playedEvent == null || sourceSkill == null || deck == null ||
                monsters == null || eventLog == null || resolutions == null ||
                playedEvent.EventType != BattleEventType.CardPlayed ||
                eventLog.Find(playedEvent.EventId) != playedEvent ||
                !string.Equals(sourceSkill.SourceCard.CatalogCardId, "C07", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(playedEvent.ActorId, sourceSkill.Ids.BattleCardId, StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(EffectId, playedEvent.EventId))
            {
                return false;
            }

            int drawAttempts = sourceSkill.CurrentLevel >= 2 ? 3 : 2;
            for (int i = 0; i < drawAttempts; i++)
            {
                if (deck.TryDraw(out BattleCardInstance drawn, out _))
                {
                    drawnCount++;
                    eventLog.Record(
                        BattleEventType.CardMoved, "C07Draw",
                        sourceSkill.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                        drawn.Ids.BattleCardId, parentEventId: playedEvent.EventId,
                        sourceEffectId: EffectId, hasZoneChange: true,
                        fromZone: CardZone.DrawPile, toZone: CardZone.Hand);
                }
            }

            bool optionalBanish = sourceSkill.CurrentLevel >= 4;
            if (string.IsNullOrWhiteSpace(selectedBanishBattleCardId))
            {
                return optionalBanish;
            }

            BattleCardInstance selected = deck.Zones.Find(selectedBanishBattleCardId);
            if (selected == null || selected.Zone != CardZone.Hand ||
                string.Equals(selected.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int selectedManaCost = selected.Resolved.ManaCost;
            if (!deck.TryBanish(selected.Ids.BattleCardId, out _))
            {
                return false;
            }

            banished = true;
            eventLog.Record(
                BattleEventType.CardMoved, "C07Banish",
                sourceSkill.Ids.BattleCardId, sourceSkill.Ids.BattleCardId,
                selected.Ids.BattleCardId, parentEventId: playedEvent.EventId,
                sourceEffectId: EffectId, hasZoneChange: true,
                fromZone: CardZone.Hand, toZone: CardZone.Banished);

            if (sourceSkill.CurrentLevel >= 5 && selectedManaCost >= 2)
            {
                foreach (BattleMonsterState monster in monsters.Monsters)
                {
                    if (monster == null || monster.Card.Zone != CardZone.MonsterField) continue;
                    monster.ApplyDefense(2);
                    defendedMonsterCount++;
                }
            }

            return true;
        }
    }
}
