using UnityEngine;
using UnityEngine.UI;

namespace HaveABreak.Cards
{
    [DisallowMultipleComponent]
    public sealed class RuntimeBattleFieldZonePlate : MonoBehaviour
    {
        private static Sprite surfaceSprite;
        private static Sprite frameSprite;

        private RuntimeBattleFieldSlotView slotView;
        private Image background;
        private Image outerFrame;
        private Image innerFrame;
        private Image centerGlyph;
        private Text zoneMark;
        private RuntimeBattleFieldZone appliedZone;
        private bool initialized;

        public bool IsInitialized => initialized;
        public Image OuterFrame => outerFrame;
        public Image InnerFrame => innerFrame;
        public Image CenterGlyph => centerGlyph;

        public void Initialize(RuntimeBattleFieldSlotView slot)
        {
            slotView = slot ?? GetComponent<RuntimeBattleFieldSlotView>();
            background = GetComponent<Image>();
            EnsureSprites();

            if (background != null)
            {
                background.sprite = surfaceSprite;
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
            }

            outerFrame = CreateImage(
                "ZoneOuterFrame",
                transform,
                frameSprite,
                Vector2.zero,
                Vector2.zero);
            Shadow frameShadow = outerFrame.gameObject.AddComponent<Shadow>();
            frameShadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
            frameShadow.effectDistance = new Vector2(0f, -3f);
            frameShadow.useGraphicAlpha = true;

            innerFrame = CreateImage(
                "ZoneInnerFrame",
                transform,
                frameSprite,
                new Vector2(8f, 8f),
                new Vector2(-8f, -8f));

            centerGlyph = CreateImage(
                "ZoneCenterGlyph",
                transform,
                surfaceSprite,
                Vector2.zero,
                Vector2.zero,
                new Vector2(28f, 28f));
            RectTransform glyphRect = centerGlyph.rectTransform;
            glyphRect.anchorMin = new Vector2(0.5f, 0.5f);
            glyphRect.anchorMax = new Vector2(0.5f, 0.5f);
            glyphRect.pivot = new Vector2(0.5f, 0.5f);
            glyphRect.anchoredPosition = Vector2.zero;
            glyphRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            zoneMark = CreateZoneMark(transform);
            appliedZone = ResolveZone();
            ApplyStaticZoneStyle(appliedZone);
            initialized = true;
            MoveDecorationBehindContent();
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                Initialize(GetComponent<RuntimeBattleFieldSlotView>());
            }

            RuntimeBattleFieldZone zone = ResolveZone();
            RuntimeBattleFieldSlotPresentation presentation =
                slotView?.Presentation;
            // Enemy artwork replaces its presentation plate, but a summoned
            // monster card must remain visibly seated inside the monster zone.
            // Hiding an occupied player plate made the entire zone appear to
            // disappear as soon as a monster was played.
            bool hideBehindCard =
                (zone == RuntimeBattleFieldZone.Enemy &&
                 presentation?.ShowsArtwork == true);
            if (zone != appliedZone)
            {
                appliedZone = zone;
                ApplyStaticZoneStyle(zone);
            }

            bool available = slotView?.DropZone?.IsAvailableHighlighted == true;
            bool selected = slotView?.Presentation?.Selected == true;
            Color accent = selected
                ? new Color(1f, 0.78f, 0.22f, 1f)
                : available
                    ? new Color(0.22f, 0.82f, 1f, 1f)
                    : ZoneAccent(zone);

            if (background != null)
            {
                Color plateBackground = background.color;
                plateBackground.a = hideBehindCard
                    ? 0f
                    : selected || available
                        ? 1f
                        : presentation?.Occupied == true
                            ? 0.94f
                            : 0.72f;
                background.color = plateBackground;
            }

            if (outerFrame != null)
            {
                outerFrame.color = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hideBehindCard
                        ? 0f
                        : selected || available ? 1f : 0.76f);
            }
            if (innerFrame != null)
            {
                innerFrame.color = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hideBehindCard
                        ? 0f
                        : selected || available ? 0.72f : 0.34f);
            }
            if (centerGlyph != null)
            {
                centerGlyph.color = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hideBehindCard
                        ? 0f
                        : selected || available ? 0.34f : 0.12f);
            }
            if (zoneMark != null)
            {
                zoneMark.color = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    hideBehindCard
                        ? 0f
                        : selected || available ? 0.92f : 0.56f);
            }
        }

        private void ApplyStaticZoneStyle(RuntimeBattleFieldZone zone)
        {
            if (zoneMark != null)
            {
                zoneMark.text = zone switch
                {
                    RuntimeBattleFieldZone.Enemy => "ENEMY",
                    RuntimeBattleFieldZone.PlayerMonster => "MONSTER",
                    RuntimeBattleFieldZone.PlayerSkill => "SKILL",
                    _ => "ZONE"
                };
            }
        }

        private RuntimeBattleFieldZone ResolveZone()
        {
            if (slotView?.Presentation != null)
            {
                return slotView.Presentation.Zone;
            }

            string objectName = gameObject.name ?? string.Empty;
            if (objectName.StartsWith("PlayerMonster"))
            {
                return RuntimeBattleFieldZone.PlayerMonster;
            }
            if (objectName.StartsWith("PlayerSkill"))
            {
                return RuntimeBattleFieldZone.PlayerSkill;
            }
            return RuntimeBattleFieldZone.Enemy;
        }

        private static Color ZoneAccent(RuntimeBattleFieldZone zone)
        {
            return zone switch
            {
                RuntimeBattleFieldZone.Enemy =>
                    new Color(0.72f, 0.22f, 0.31f, 1f),
                RuntimeBattleFieldZone.PlayerMonster =>
                    new Color(0.19f, 0.62f, 0.86f, 1f),
                RuntimeBattleFieldZone.PlayerSkill =>
                    new Color(0.5f, 0.34f, 0.82f, 1f),
                _ => new Color(0.45f, 0.56f, 0.66f, 1f)
            };
        }

        private void MoveDecorationBehindContent()
        {
            outerFrame?.transform.SetSiblingIndex(0);
            innerFrame?.transform.SetSiblingIndex(1);
            centerGlyph?.transform.SetSiblingIndex(2);
            zoneMark?.transform.SetSiblingIndex(3);
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Vector2? size = null)
        {
            GameObject imageObject = new(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            if (size.HasValue)
            {
                rect.sizeDelta = size.Value;
            }
            return image;
        }

        private static Text CreateZoneMark(Transform parent)
        {
            GameObject textObject = new(
                "ZoneTypeMark",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 9;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.LowerCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.2f, 0f);
            rect.anchorMax = new Vector2(0.8f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 7f);
            rect.sizeDelta = new Vector2(0f, 18f);
            return text;
        }

        private static void EnsureSprites()
        {
            surfaceSprite ??= CreateChamferedSprite(false);
            frameSprite ??= CreateChamferedSprite(true);
        }

        private static Sprite CreateChamferedSprite(bool frameOnly)
        {
            const int width = 96;
            const int height = 96;
            const int corner = 14;
            const int border = 4;
            Texture2D texture = new(
                width,
                height,
                TextureFormat.RGBA32,
                false)
            {
                name = frameOnly
                    ? "RuntimeFieldZoneFrame"
                    : "RuntimeFieldZoneSurface",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color32 clear = new(0, 0, 0, 0);
            Color32 solid = new(255, 255, 255, 255);
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outer = InsideChamfer(
                        x,
                        y,
                        width,
                        height,
                        corner);
                    bool inner = x >= border && y >= border &&
                                 x < width - border &&
                                 y < height - border &&
                                 InsideChamfer(
                                     x - border,
                                     y - border,
                                     width - border * 2,
                                     height - border * 2,
                                     Mathf.Max(1, corner - border));
                    bool visible = frameOnly
                        ? outer && !inner
                        : outer;
                    pixels[y * width + x] = visible ? solid : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool InsideChamfer(
            int x,
            int y,
            int width,
            int height,
            int corner)
        {
            int dx = Mathf.Min(x, width - 1 - x);
            int dy = Mathf.Min(y, height - 1 - y);
            return dx >= 0 && dy >= 0 && dx + dy >= corner;
        }
    }

    public static class RuntimeBattleFieldZoneStyleBootstrap
    {
        private static float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Canvas.willRenderCanvases -= ApplyToLoadedFields;
            Canvas.willRenderCanvases += ApplyToLoadedFields;
            nextScanTime = 0f;
        }

        public static void ApplyToLoadedFields()
        {
            if (Application.isPlaying && Time.unscaledTime < nextScanTime)
            {
                return;
            }
            nextScanTime = Time.unscaledTime + 0.2f;

            RuntimeBattleFieldSlotView[] slots =
                Object.FindObjectsByType<RuntimeBattleFieldSlotView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (RuntimeBattleFieldSlotView slot in slots)
            {
                if (slot == null)
                {
                    continue;
                }

                RuntimeBattleFieldZonePlate plate =
                    slot.GetComponent<RuntimeBattleFieldZonePlate>();
                if (plate == null)
                {
                    plate = slot.gameObject.AddComponent<
                        RuntimeBattleFieldZonePlate>();
                }
                if (!plate.IsInitialized)
                {
                    plate.Initialize(slot);
                }
            }
        }
    }
}
