using UnityEngine;

namespace HaveABreak.Cards
{
    [CreateAssetMenu(fileName = "CardLayoutSettings",
        menuName = "Have a Break/UI/Card Layout Settings")]
    public sealed class CardLayoutSettings : ScriptableObject
    {
        [Header("Normalized Rects (X, Y, Width, Height)")]
        [SerializeField] private Rect mana = new(0.025f, 0.865f, 0.17f, 0.11f);
        [SerializeField] private Rect cardName = new(0.21f, 0.875f, 0.56f, 0.09f);
        [SerializeField] private Rect artwork = new(0.04f, 0.35f, 0.92f, 0.50f);
        [SerializeField] private Rect rulesPanel = new(0.04f, 0.095f, 0.92f, 0.235f);
        [SerializeField] private Rect effect = new(0.075f, 0.12f, 0.85f, 0.185f);
        [SerializeField] private Rect attack = new(0.055f, 0.035f, 0.40f, 0.045f);
        [SerializeField] private Rect health = new(0.545f, 0.035f, 0.40f, 0.045f);
        [SerializeField] private Rect cardType = new(0.79f, 0.885f, 0.16f, 0.07f);
        [SerializeField] private Rect selection = new(0.60f, 0.765f, 0.26f, 0.05f);
        [SerializeField] private Rect blockReason = new(0.10f, 0.39f, 0.80f, 0.08f);

        [Header("Font")]
        [SerializeField] private string koreanOsFontName = "Malgun Gothic";
        [SerializeField, Range(8, 40)] private int manaSize = 22;
        [SerializeField, Range(8, 40)] private int nameSize = 16;
        [SerializeField, Range(8, 40)] private int effectSize = 12;
        [SerializeField, Range(8, 40)] private int statSize = 10;
        [SerializeField, Range(8, 40)] private int typeSize = 9;

        public Rect Mana => mana;
        public Rect CardName => cardName;
        public Rect Artwork => artwork;
        public Rect RulesPanel => rulesPanel;
        public Rect Effect => effect;
        public Rect Attack => attack;
        public Rect Health => health;
        public Rect CardType => cardType;
        public Rect Selection => selection;
        public Rect BlockReason => blockReason;
        public string KoreanOsFontName => koreanOsFontName;
        public int ManaSize => manaSize;
        public int NameSize => nameSize;
        public int EffectSize => effectSize;
        public int StatSize => statSize;
        public int TypeSize => typeSize;

#if UNITY_EDITOR
        public void EditorCapture(
            Rect manaRect,
            Rect nameRect,
            Rect artworkRect,
            Rect rulesRect,
            Rect effectRect,
            Rect attackRect,
            Rect healthRect,
            Rect typeRect,
            Rect selectionRect,
            Rect blockRect,
            int manaFontSize,
            int nameFontSize,
            int effectFontSize,
            int statFontSize,
            int typeFontSize)
        {
            SetIfUsable(ref mana, manaRect);
            SetIfUsable(ref cardName, nameRect);
            SetIfUsable(ref artwork, artworkRect);
            SetIfUsable(ref rulesPanel, rulesRect);
            SetIfUsable(ref effect, effectRect);
            SetIfUsable(ref attack, attackRect);
            SetIfUsable(ref health, healthRect);
            SetIfUsable(ref cardType, typeRect);
            SetIfUsable(ref selection, selectionRect);
            SetIfUsable(ref blockReason, blockRect);
            manaSize = manaFontSize;
            nameSize = nameFontSize;
            effectSize = effectFontSize;
            statSize = statFontSize;
            typeSize = typeFontSize;
        }

        private static void SetIfUsable(ref Rect target, Rect value)
        {
            if (value.width > 0.0001f && value.height > 0.0001f)
            {
                target = value;
            }
        }
#endif
    }
}
