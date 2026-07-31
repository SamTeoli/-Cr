using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaveABreak.Cards
{
    public static class RuntimeEnemyArtworkCatalog
    {
        public const int TextureSize = 256;

        private enum ArtworkFamily
        {
            TicketHusk,
            GhostConductor,
            RailGolem
        }

        private readonly struct ArtworkSpec
        {
            public ArtworkSpec(
                string id,
                ArtworkFamily family,
                int grade,
                Color accent)
            {
                Id = id;
                Family = family;
                Grade = Mathf.Clamp(grade, 0, 3);
                Accent = accent;
            }

            public string Id { get; }
            public ArtworkFamily Family { get; }
            public int Grade { get; }
            public Color Accent { get; }
        }

        private static readonly ArtworkSpec[] Specs =
        {
            new("PT01_Left", ArtworkFamily.TicketHusk, 0,
                new Color(0.93f, 0.68f, 0.26f, 1f)),
            new("PT02_Center", ArtworkFamily.GhostConductor, 0,
                new Color(0.28f, 0.72f, 1f, 1f)),
            new("PT03_Right", ArtworkFamily.RailGolem, 0,
                new Color(1f, 0.56f, 0.16f, 1f)),
            new("PT10_Left", ArtworkFamily.TicketHusk, 1,
                new Color(1f, 0.24f, 0.24f, 1f)),
            new("PT10_Center", ArtworkFamily.GhostConductor, 1,
                new Color(0.22f, 1f, 0.78f, 1f)),
            new("PT10_Right", ArtworkFamily.RailGolem, 1,
                new Color(1f, 0.18f, 0.12f, 1f)),
            new("PT20_Left", ArtworkFamily.TicketHusk, 2,
                new Color(0.66f, 0.31f, 1f, 1f)),
            new("PT20_Center", ArtworkFamily.GhostConductor, 2,
                new Color(0.38f, 1f, 0.34f, 1f)),
            new("PT20_Right", ArtworkFamily.RailGolem, 2,
                new Color(0.14f, 1f, 0.78f, 1f)),
            new("PT30_Left", ArtworkFamily.TicketHusk, 3,
                new Color(1f, 0.83f, 0.26f, 1f)),
            new("PT30_Center", ArtworkFamily.GhostConductor, 3,
                new Color(0.42f, 0.72f, 1f, 1f)),
            new("PT30_Right", ArtworkFamily.RailGolem, 3,
                new Color(1f, 0.28f, 0.64f, 1f))
        };

        private static readonly Dictionary<string, Sprite> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<string> ArtworkIds { get; } =
            Array.ConvertAll(Specs, spec => spec.Id);

        public static Sprite Load(string enemyId)
        {
            string artworkId = ResolveArtworkId(enemyId);
            return LoadArtwork(artworkId);
        }

        public static Sprite LoadArtwork(string artworkId)
        {
            if (string.IsNullOrWhiteSpace(artworkId))
            {
                return null;
            }

            if (Cache.TryGetValue(artworkId, out Sprite cached))
            {
                return cached;
            }

            ArtworkSpec? spec = FindSpec(artworkId);
            if (!spec.HasValue)
            {
                return null;
            }

            Sprite sprite = Render(spec.Value);
            Cache[spec.Value.Id] = sprite;
            return sprite;
        }

        public static string ResolveArtworkId(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return string.Empty;
            }

            ArtworkSpec? exact = FindSpec(enemyId);
            if (exact.HasValue)
            {
                return exact.Value.Id;
            }

            string upper = enemyId.ToUpperInvariant();
            int grade = upper.Contains("FINALBOSS")
                ? 3
                : upper.Contains("MIDBOSS")
                    ? 2
                    : upper.Contains("ELITE")
                        ? 1
                        : 0;
            string suffix = upper.Contains("LEFT")
                ? "Left"
                : upper.Contains("CENTER")
                    ? "Center"
                    : upper.Contains("RIGHT")
                        ? "Right"
                        : string.Empty;
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return string.Empty;
            }

            string prefix = grade switch
            {
                1 => "PT10_",
                2 => "PT20_",
                3 => "PT30_",
                _ => suffix == "Left"
                    ? "PT01_"
                    : suffix == "Center"
                        ? "PT02_"
                        : "PT03_"
            };
            return prefix + suffix;
        }

        private static ArtworkSpec? FindSpec(string id)
        {
            foreach (ArtworkSpec spec in Specs)
            {
                if (string.Equals(
                        spec.Id,
                        id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return spec;
                }
            }
            return null;
        }

        private static Sprite Render(ArtworkSpec spec)
        {
            PixelCanvas canvas = new(TextureSize, TextureSize);
            DrawGradeAura(canvas, spec);
            switch (spec.Family)
            {
                case ArtworkFamily.TicketHusk:
                    DrawTicketHusk(canvas, spec);
                    break;
                case ArtworkFamily.GhostConductor:
                    DrawGhostConductor(canvas, spec);
                    break;
                case ArtworkFamily.RailGolem:
                    DrawRailGolem(canvas, spec);
                    break;
            }
            DrawGradeOrnaments(canvas, spec);

            Texture2D texture = new(
                TextureSize,
                TextureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = $"RuntimeEnemyArtwork_{spec.Id}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(canvas.Pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = spec.Id;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static void DrawGradeAura(
            PixelCanvas canvas,
            ArtworkSpec spec)
        {
            Color glow = WithAlpha(spec.Accent, 0.04f + spec.Grade * 0.018f);
            for (int radius = 102; radius >= 58; radius -= 8)
            {
                canvas.FillEllipse(128f, 132f, radius, radius, glow);
            }
            if (spec.Grade > 0)
            {
                canvas.DrawRing(
                    128f,
                    132f,
                    96f + spec.Grade * 6f,
                    2f + spec.Grade,
                    WithAlpha(spec.Accent, 0.32f));
            }
        }

        private static void DrawTicketHusk(
            PixelCanvas canvas,
            ArtworkSpec spec)
        {
            Color cloth = Color.Lerp(
                new Color(0.10f, 0.11f, 0.15f, 1f),
                spec.Accent,
                0.14f + spec.Grade * 0.035f);
            Color paper = Color.Lerp(
                new Color(0.72f, 0.68f, 0.55f, 1f),
                spec.Accent,
                0.12f);
            Color shadow = new(0.015f, 0.018f, 0.026f, 0.98f);

            canvas.FillTriangle(
                new Vector2(128f, 34f),
                new Vector2(47f, 210f),
                new Vector2(209f, 210f),
                cloth);
            canvas.FillEllipse(128f, 105f, 61f, 63f, shadow);
            canvas.FillTriangle(
                new Vector2(128f, 41f),
                new Vector2(72f, 118f),
                new Vector2(184f, 118f),
                Color.Lerp(cloth, paper, 0.22f));

            canvas.DrawLine(76f, 126f, 34f, 218f, 22f, cloth);
            canvas.DrawLine(180f, 126f, 222f, 218f, 22f, cloth);
            canvas.DrawLine(91f, 180f, 72f, 232f, 24f, cloth);
            canvas.DrawLine(165f, 180f, 184f, 232f, 24f, cloth);
            canvas.DrawLine(34f, 218f, 20f, 238f, 5f, paper);
            canvas.DrawLine(222f, 218f, 236f, 238f, 5f, paper);

            canvas.FillEllipse(108f, 102f, 8f, 8f, Color.white);
            canvas.FillEllipse(148f, 102f, 8f, 8f, Color.white);
            canvas.FillEllipse(108f, 102f, 15f, 15f,
                WithAlpha(spec.Accent, 0.22f));
            canvas.FillEllipse(148f, 102f, 15f, 15f,
                WithAlpha(spec.Accent, 0.22f));

            int shardCount = 9 + spec.Grade * 5;
            for (int index = 0; index < shardCount; index++)
            {
                float angle = index * 2.399963f + spec.Grade * 0.3f;
                float radius = 35f + index % 5 * 17f;
                float x = 128f + Mathf.Cos(angle) * radius;
                float y = 120f + Mathf.Sin(angle) * radius * 0.9f;
                canvas.DrawLine(
                    x - 8f,
                    y - 3f,
                    x + 8f,
                    y + 3f,
                    5f,
                    WithAlpha(paper, 0.78f));
            }
        }

        private static void DrawGhostConductor(
            PixelCanvas canvas,
            ArtworkSpec spec)
        {
            Color coat = Color.Lerp(
                new Color(0.035f, 0.075f, 0.13f, 1f),
                spec.Accent,
                0.20f + spec.Grade * 0.04f);
            Color flame = Color.Lerp(Color.white, spec.Accent, 0.72f);
            Color metal = new(0.24f, 0.28f, 0.34f, 1f);

            canvas.FillTriangle(
                new Vector2(128f, 66f),
                new Vector2(54f, 221f),
                new Vector2(202f, 221f),
                WithAlpha(coat, 0.96f));
            canvas.FillEllipse(128f, 76f, 35f, 39f,
                WithAlpha(flame, 0.34f));
            canvas.FillEllipse(128f, 80f, 22f, 27f,
                WithAlpha(flame, 0.78f));
            canvas.FillEllipse(128f, 82f, 11f, 14f, Color.white);

            canvas.FillRect(92f, 45f, 164f, 57f, metal);
            canvas.FillRect(104f, 31f, 152f, 48f,
                Color.Lerp(metal, spec.Accent, 0.18f));
            canvas.DrawLine(78f, 114f, 31f, 177f, 17f, coat);
            canvas.DrawLine(178f, 114f, 221f, 161f, 17f, coat);
            canvas.DrawLine(221f, 161f, 222f, 204f, 8f, metal);
            canvas.FillRect(205f, 181f, 239f, 225f,
                new Color(0.055f, 0.075f, 0.09f, 0.96f));
            canvas.FillRect(210f, 187f, 234f, 219f,
                WithAlpha(flame, 0.84f));
            canvas.DrawRing(222f, 203f, 18f, 3f,
                WithAlpha(spec.Accent, 0.88f));

            for (int index = 0; index < 8 + spec.Grade * 3; index++)
            {
                float angle = index * 0.8f;
                float startX = 128f + Mathf.Cos(angle) * 28f;
                float startY = 86f + Mathf.Sin(angle) * 31f;
                float endX = startX + Mathf.Cos(angle + 0.5f) *
                    (34f + spec.Grade * 8f);
                float endY = startY + Mathf.Sin(angle + 0.5f) *
                    (34f + spec.Grade * 8f);
                canvas.DrawLine(
                    startX,
                    startY,
                    endX,
                    endY,
                    5f,
                    WithAlpha(flame, 0.34f));
            }
        }

        private static void DrawRailGolem(
            PixelCanvas canvas,
            ArtworkSpec spec)
        {
            Color armor = Color.Lerp(
                new Color(0.16f, 0.17f, 0.19f, 1f),
                spec.Accent,
                0.22f + spec.Grade * 0.035f);
            Color dark = new(0.035f, 0.04f, 0.05f, 1f);
            Color light = Color.Lerp(Color.white, spec.Accent, 0.84f);

            canvas.FillRect(82f, 74f, 174f, 169f, armor);
            canvas.FillRect(96f, 48f, 160f, 91f, dark);
            canvas.FillEllipse(128f, 70f, 25f, 25f,
                WithAlpha(light, 0.95f));
            canvas.FillEllipse(128f, 70f, 37f, 37f,
                WithAlpha(spec.Accent, 0.18f));

            canvas.DrawLine(87f, 95f, 43f, 184f, 25f, armor);
            canvas.DrawLine(169f, 95f, 213f, 184f, 25f, armor);
            canvas.FillEllipse(45f, 188f, 21f, 25f, dark);
            canvas.FillRect(197f, 168f, 234f, 215f, armor);
            canvas.DrawLine(103f, 162f, 87f, 225f, 28f, armor);
            canvas.DrawLine(153f, 162f, 169f, 225f, 28f, armor);
            canvas.FillRect(65f, 218f, 108f, 239f, dark);
            canvas.FillRect(148f, 218f, 191f, 239f, dark);

            canvas.FillRect(84f, 111f, 172f, 124f,
                WithAlpha(spec.Accent, 0.78f));
            canvas.FillRect(89f, 129f, 167f, 140f, dark);
            canvas.DrawLine(91f, 137f, 165f, 113f, 5f,
                WithAlpha(spec.Accent, 0.68f));

            int beaconCount = 2 + spec.Grade;
            for (int index = 0; index < beaconCount; index++)
            {
                float x = 94f + index * (68f / Mathf.Max(1, beaconCount - 1));
                canvas.FillRect(x - 6f, 35f, x + 6f, 54f, dark);
                canvas.FillEllipse(x, 36f, 8f, 10f,
                    WithAlpha(light, 0.96f));
            }
        }

        private static void DrawGradeOrnaments(
            PixelCanvas canvas,
            ArtworkSpec spec)
        {
            if (spec.Grade >= 1)
            {
                int segments = 5 + spec.Grade * 2;
                for (int index = 0; index < segments; index++)
                {
                    float angle = index * Mathf.PI * 2f / segments;
                    float x1 = 128f + Mathf.Cos(angle) * 104f;
                    float y1 = 132f + Mathf.Sin(angle) * 104f;
                    float x2 = 128f + Mathf.Cos(angle) *
                        (111f + spec.Grade * 3f);
                    float y2 = 132f + Mathf.Sin(angle) *
                        (111f + spec.Grade * 3f);
                    canvas.DrawLine(
                        x1,
                        y1,
                        x2,
                        y2,
                        3f + spec.Grade,
                        WithAlpha(spec.Accent, 0.72f));
                }
            }

            if (spec.Grade == 3)
            {
                canvas.DrawLine(89f, 42f, 105f, 17f, 5f,
                    WithAlpha(spec.Accent, 0.90f));
                canvas.DrawLine(105f, 17f, 128f, 40f, 5f,
                    WithAlpha(spec.Accent, 0.90f));
                canvas.DrawLine(128f, 40f, 151f, 17f, 5f,
                    WithAlpha(spec.Accent, 0.90f));
                canvas.DrawLine(151f, 17f, 167f, 42f, 5f,
                    WithAlpha(spec.Accent, 0.90f));
                canvas.DrawLine(89f, 42f, 167f, 42f, 5f,
                    WithAlpha(spec.Accent, 0.90f));
                canvas.DrawRing(128f, 132f, 119f, 2.5f,
                    WithAlpha(spec.Accent, 0.44f));
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private sealed class PixelCanvas
        {
            private readonly int width;
            private readonly int height;

            public PixelCanvas(int width, int height)
            {
                this.width = width;
                this.height = height;
                Pixels = new Color[width * height];
            }

            public Color[] Pixels { get; }

            public void FillRect(
                float left,
                float bottom,
                float right,
                float top,
                Color color)
            {
                int minX = Mathf.Clamp(Mathf.FloorToInt(left), 0, width - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt(right), 0, width - 1);
                int minY = Mathf.Clamp(Mathf.FloorToInt(bottom), 0, height - 1);
                int maxY = Mathf.Clamp(Mathf.CeilToInt(top), 0, height - 1);
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Blend(x, y, color);
                    }
                }
            }

            public void FillEllipse(
                float centerX,
                float centerY,
                float radiusX,
                float radiusY,
                Color color)
            {
                int minX = Mathf.Clamp(
                    Mathf.FloorToInt(centerX - radiusX), 0, width - 1);
                int maxX = Mathf.Clamp(
                    Mathf.CeilToInt(centerX + radiusX), 0, width - 1);
                int minY = Mathf.Clamp(
                    Mathf.FloorToInt(centerY - radiusY), 0, height - 1);
                int maxY = Mathf.Clamp(
                    Mathf.CeilToInt(centerY + radiusY), 0, height - 1);
                float rx = Mathf.Max(0.001f, radiusX);
                float ry = Mathf.Max(0.001f, radiusY);
                for (int y = minY; y <= maxY; y++)
                {
                    float ny = (y - centerY) / ry;
                    for (int x = minX; x <= maxX; x++)
                    {
                        float nx = (x - centerX) / rx;
                        if (nx * nx + ny * ny <= 1f)
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            public void DrawRing(
                float centerX,
                float centerY,
                float radius,
                float thickness,
                Color color)
            {
                float outer = radius + thickness * 0.5f;
                float inner = Mathf.Max(0f, radius - thickness * 0.5f);
                int minX = Mathf.Clamp(
                    Mathf.FloorToInt(centerX - outer), 0, width - 1);
                int maxX = Mathf.Clamp(
                    Mathf.CeilToInt(centerX + outer), 0, width - 1);
                int minY = Mathf.Clamp(
                    Mathf.FloorToInt(centerY - outer), 0, height - 1);
                int maxY = Mathf.Clamp(
                    Mathf.CeilToInt(centerY + outer), 0, height - 1);
                float outerSquared = outer * outer;
                float innerSquared = inner * inner;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float distance = dx * dx + dy * dy;
                        if (distance <= outerSquared &&
                            distance >= innerSquared)
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            public void DrawLine(
                float x1,
                float y1,
                float x2,
                float y2,
                float thickness,
                Color color)
            {
                float radius = thickness * 0.5f;
                int minX = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Min(x1, x2) - radius),
                    0,
                    width - 1);
                int maxX = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Max(x1, x2) + radius),
                    0,
                    width - 1);
                int minY = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Min(y1, y2) - radius),
                    0,
                    height - 1);
                int maxY = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Max(y1, y2) + radius),
                    0,
                    height - 1);
                Vector2 start = new(x1, y1);
                Vector2 end = new(x2, y2);
                Vector2 segment = end - start;
                float lengthSquared = Mathf.Max(0.001f, segment.sqrMagnitude);
                float radiusSquared = radius * radius;
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Vector2 point = new(x, y);
                        float t = Mathf.Clamp01(
                            Vector2.Dot(point - start, segment) /
                            lengthSquared);
                        Vector2 closest = start + segment * t;
                        if ((point - closest).sqrMagnitude <= radiusSquared)
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            public void FillTriangle(
                Vector2 a,
                Vector2 b,
                Vector2 c,
                Color color)
            {
                int minX = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))),
                    0,
                    width - 1);
                int maxX = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))),
                    0,
                    width - 1);
                int minY = Mathf.Clamp(
                    Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))),
                    0,
                    height - 1);
                int maxY = Mathf.Clamp(
                    Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))),
                    0,
                    height - 1);
                float area = Edge(a, b, c);
                if (Mathf.Abs(area) < 0.001f)
                {
                    return;
                }
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        Vector2 point = new(x, y);
                        float w0 = Edge(b, c, point);
                        float w1 = Edge(c, a, point);
                        float w2 = Edge(a, b, point);
                        bool inside = area > 0f
                            ? w0 >= 0f && w1 >= 0f && w2 >= 0f
                            : w0 <= 0f && w1 <= 0f && w2 <= 0f;
                        if (inside)
                        {
                            Blend(x, y, color);
                        }
                    }
                }
            }

            private static float Edge(Vector2 a, Vector2 b, Vector2 c)
            {
                return (c.x - a.x) * (b.y - a.y) -
                       (c.y - a.y) * (b.x - a.x);
            }

            private void Blend(int x, int y, Color source)
            {
                if (x < 0 || x >= width || y < 0 || y >= height ||
                    source.a <= 0f)
                {
                    return;
                }

                int index = y * width + x;
                Color destination = Pixels[index];
                float outputAlpha = source.a +
                    destination.a * (1f - source.a);
                if (outputAlpha <= 0.0001f)
                {
                    Pixels[index] = Color.clear;
                    return;
                }

                Color output = new(
                    (source.r * source.a +
                     destination.r * destination.a * (1f - source.a)) /
                    outputAlpha,
                    (source.g * source.a +
                     destination.g * destination.a * (1f - source.a)) /
                    outputAlpha,
                    (source.b * source.a +
                     destination.b * destination.a * (1f - source.a)) /
                    outputAlpha,
                    outputAlpha);
                Pixels[index] = output;
            }
        }
    }
}
