using System;

namespace HaveABreak.Cards
{
    public static class C11LateNightWaitingRoomResolver
    {
        private const string EffectId = "C11-TURN-START";

        public static bool TryResolve(
            BattleCardInstance sourceBarrier,
            int playerTurn,
            BattleDeckState deck,
            BattleMonsterRegistry monsters,
            BattleEventLog eventLog,
            BattleEffectResolutionTracker resolutions,
            out int drawnCount,
            out string defendedMonsterId)
        {
            drawnCount = 0;
            defendedMonsterId = null;
            if (sourceBarrier == null || playerTurn < 1 || deck == null ||
                monsters == null || eventLog == null || resolutions == null ||
                sourceBarrier.Zone != CardZone.SkillField ||
                !string.Equals(sourceBarrier.SourceCard.CatalogCardId, "C11",
                    StringComparison.OrdinalIgnoreCase) ||
                !resolutions.TryBegin(
                    EffectId, $"PLAYER-TURN-{playerTurn}:{sourceBarrier.Ids.BattleCardId}"))
            {
                return false;
            }

            int threshold = sourceBarrier.CurrentLevel >= 2 ? 5 : 4;
            if (deck.Zones.Count(CardZone.Hand) > threshold)
            {
                return true;
            }

            int attempts = sourceBarrier.CurrentLevel >= 5 ? 2 : 1;
            for (int i = 0; i < attempts; i++)
            {
                if (deck.TryDraw(out BattleCardInstance drawn, out _))
                {
                    drawnCount++;
                    eventLog.Record(
                        BattleEventType.CardMoved, "C11TurnStartDraw",
                        sourceBarrier.Ids.BattleCardId, sourceBarrier.Ids.BattleCardId,
                        drawn.Ids.BattleCardId, sourceEffectId: EffectId,
                        hasZoneChange: true, fromZone: CardZone.DrawPile, toZone: CardZone.Hand);
                }
            }

            if (sourceBarrier.CurrentLevel >= 4 && drawnCount > 0)
            {
                BattleMonsterState lowest = null;
                foreach (BattleMonsterState monster in monsters.Monsters)
                {
                    if (monster == null || monster.Card.Zone != CardZone.MonsterField) continue;
                    if (lowest == null || monster.CurrentHealth < lowest.CurrentHealth ||
                        monster.CurrentHealth == lowest.CurrentHealth &&
                        string.Compare(monster.BattleCardId, lowest.BattleCardId,
                            StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        lowest = monster;
                    }
                }

                if (lowest != null)
                {
                    lowest.ApplyDefense(1);
                    defendedMonsterId = lowest.BattleCardId;
                }
            }

            return true;
        }
    }
}
