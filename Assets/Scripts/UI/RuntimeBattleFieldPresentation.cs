using System;

namespace HaveABreak.Cards
{
    public enum RuntimeBattleFieldZone
    {
        Enemy,
        PlayerMonster,
        PlayerSkill
    }

    public sealed class RuntimeBattleFieldSlotPresentation
    {
        public RuntimeBattleFieldSlotPresentation(
            RuntimeBattleFieldZone zone,
            int index,
            string title,
            string detail,
            bool occupied,
            bool selected,
            bool interactable,
            string clickCommandId,
            string dropCommandId)
        {
            Zone = zone;
            Index = Math.Max(0, index);
            Title = title ?? string.Empty;
            Detail = detail ?? string.Empty;
            Occupied = occupied;
            Selected = selected;
            Interactable = interactable;
            ClickCommandId = clickCommandId ?? string.Empty;
            DropCommandId = dropCommandId ?? string.Empty;
        }

        public RuntimeBattleFieldZone Zone { get; }
        public int Index { get; }
        public string Title { get; }
        public string Detail { get; }
        public bool Occupied { get; }
        public bool Selected { get; }
        public bool Interactable { get; }
        public string ClickCommandId { get; }
        public string DropCommandId { get; }
        public bool AcceptsCards => !string.IsNullOrWhiteSpace(DropCommandId);
    }

    public sealed class RuntimeBattleFieldPresentation
    {
        public const int SlotCount = 3;

        public RuntimeBattleFieldPresentation(
            RuntimeBattleFieldSlotPresentation[] enemies,
            RuntimeBattleFieldSlotPresentation[] monsters,
            RuntimeBattleFieldSlotPresentation[] skills)
        {
            Enemies = Normalize(
                enemies,
                RuntimeBattleFieldZone.Enemy,
                "빈 적 칸");
            Monsters = Normalize(
                monsters,
                RuntimeBattleFieldZone.PlayerMonster,
                "빈 몬스터존");
            Skills = Normalize(
                skills,
                RuntimeBattleFieldZone.PlayerSkill,
                "빈 스킬존");
        }

        public RuntimeBattleFieldSlotPresentation[] Enemies { get; }
        public RuntimeBattleFieldSlotPresentation[] Monsters { get; }
        public RuntimeBattleFieldSlotPresentation[] Skills { get; }

        public static RuntimeBattleFieldPresentation Empty { get; } =
            new(null, null, null);

        public static RuntimeBattleFieldPresentation FromSnapshot(
            BattleScreenSnapshot snapshot)
        {
            if (snapshot?.Available != true)
            {
                return Empty;
            }

            RuntimeBattleFieldSlotPresentation[] enemies =
                new RuntimeBattleFieldSlotPresentation[SlotCount];
            RuntimeBattleFieldSlotPresentation[] monsters =
                new RuntimeBattleFieldSlotPresentation[SlotCount];
            RuntimeBattleFieldSlotPresentation[] skills =
                new RuntimeBattleFieldSlotPresentation[SlotCount];

            for (int index = 0; index < SlotCount; index++)
            {
                BattleEnemyDisplayOption enemy =
                    index < snapshot.Enemies.Length
                        ? snapshot.Enemies[index]
                        : null;
                bool enemyOccupied = enemy?.IsOccupied == true;
                string enemyCommand = enemy?.CanSelect == true
                    ? $"enemy:{enemy.EnemyId}"
                    : string.Empty;
                enemies[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.Enemy,
                    index,
                    enemyOccupied ? enemy.DisplayText : "빈 적 칸",
                    enemyOccupied
                        ? JoinDetail(enemy.IntentText, enemy.StatusText)
                        : string.Empty,
                    enemyOccupied,
                    enemy?.IsSelected == true,
                    enemy?.CanSelect == true,
                    enemyCommand,
                    enemyCommand);

                BattleMonsterDisplayOption monster =
                    index < snapshot.Monsters.Length
                        ? snapshot.Monsters[index]
                        : null;
                bool monsterOccupied = monster?.IsOccupied == true;
                monsters[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.PlayerMonster,
                    index,
                    monsterOccupied ? monster.DisplayText : "빈 몬스터존",
                    monsterOccupied
                        ? JoinDetail(monster.StatusText, monster.BlockReason)
                        : "몬스터 카드를 놓아 소환",
                    monsterOccupied,
                    false,
                    monster?.CanAttack == true,
                    monster?.CanAttack == true
                        ? $"attack:{monster.BattleCardId}"
                        : string.Empty,
                    snapshot.SessionFinished
                        ? string.Empty
                        : $"field:monster:{index}");

                BattleInstalledCardDisplayOption installed =
                    index < snapshot.InstalledCards.Length
                        ? snapshot.InstalledCards[index]
                        : null;
                bool installedOccupied = installed != null;
                skills[index] = new RuntimeBattleFieldSlotPresentation(
                    RuntimeBattleFieldZone.PlayerSkill,
                    index,
                    installedOccupied ? installed.DisplayName : "빈 스킬존",
                    installedOccupied
                        ? installed.DisplayText
                        : "스킬·트랩·결계 카드를 놓아 설치",
                    installedOccupied,
                    false,
                    false,
                    string.Empty,
                    snapshot.SessionFinished
                        ? string.Empty
                        : $"field:skill:{index}");
            }

            return new RuntimeBattleFieldPresentation(
                enemies,
                monsters,
                skills);
        }

        private static RuntimeBattleFieldSlotPresentation[] Normalize(
            RuntimeBattleFieldSlotPresentation[] source,
            RuntimeBattleFieldZone zone,
            string emptyTitle)
        {
            RuntimeBattleFieldSlotPresentation[] normalized =
                new RuntimeBattleFieldSlotPresentation[SlotCount];
            for (int index = 0; index < SlotCount; index++)
            {
                normalized[index] = source != null && index < source.Length &&
                                    source[index] != null
                    ? source[index]
                    : new RuntimeBattleFieldSlotPresentation(
                        zone,
                        index,
                        emptyTitle,
                        string.Empty,
                        false,
                        false,
                        false,
                        string.Empty,
                        string.Empty);
            }
            return normalized;
        }

        private static string JoinDetail(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first))
            {
                return second ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(second))
            {
                return first;
            }
            return $"{first}\n{second}";
        }
    }
}
