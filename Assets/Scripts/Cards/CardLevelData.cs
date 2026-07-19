using System;
using UnityEngine;

namespace HaveABreak.Cards
{
    [Serializable]
    public sealed class CardLevelData
    {
        [SerializeField, Range(1, 5)] private int level = 1;
        [SerializeField, Min(0)] private int manaCost;
        [SerializeField, Min(0)] private int attack;
        [SerializeField, Min(0)] private int health;
        [SerializeField, TextArea(2, 6)] private string rulesText;

        public int Level => level;
        public int ManaCost => manaCost;
        public int Attack => attack;
        public int Health => health;
        public string RulesText => rulesText;
    }
}
