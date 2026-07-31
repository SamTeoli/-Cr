using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HaveABreak.Editor
{
    internal static class RuntimeEnemyArtworkValidation
    {
        [MenuItem("Have a Break/Tests/Validate Runtime Enemy Artwork")]
        private static void RunFromMenu()
        {
            Validate();
        }

        internal static bool Validate()
        {
            GameObject host = new(
                "Runtime Enemy Artwork Validation",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(RuntimeBattleFieldView));

            try
            {
                RuntimeBattleFieldView field =
                    host.GetComponent<RuntimeBattleFieldView>();
                field.Initialize(_ => { });
                field.Bind(CreateArtworkPresentation());
                RuntimeBattleFieldZoneStyleBootstrap.ApplyToLoadedFields();
                RuntimeEnemyArtworkBootstrap.ApplyToLoadedFields();
                Canvas.ForceUpdateCanvases();

                bool allCatalogEntries =
                    RuntimeEnemyArtworkCatalog.ArtworkIds.Count == 12 &&
                    RuntimeEnemyArtworkCatalog.ArtworkIds.All(id =>
                    {
                        Sprite sprite =
                            RuntimeEnemyArtworkCatalog.LoadArtwork(id);
                        if (sprite?.texture == null)
                        {
                            return false;
                        }
                        Color corner = sprite.texture.GetPixel(0, 0);
                        return sprite.texture.width ==
                                   RuntimeEnemyArtworkCatalog.TextureSize &&
                               sprite.texture.height ==
                                   RuntimeEnemyArtworkCatalog.TextureSize &&
                               corner.a <= 0.01f;
                    });

                RuntimeEnemyArtworkSlotView[] artworkViews =
                    field.EnemySlots
                        .Select(slot => slot.GetComponent<
                            RuntimeEnemyArtworkSlotView>())
                        .ToArray();
                bool fieldApplied = artworkViews.Length == 3 &&
                                    artworkViews.All(view =>
                                        view != null &&
                                        view.IsShowingArtwork &&
                                        view.ArtworkImage != null &&
                                        view.ArtworkImage.preserveAspect &&
                                        !view.ArtworkImage.raycastTarget &&
                                        view.InformationBackdrop != null &&
                                        view.InformationBackdrop.gameObject
                                            .activeSelf);
                bool distinct = artworkViews
                    .Select(view => view.ArtworkImage.sprite.name)
                    .Distinct()
                    .Count() == 3;

                field.Bind(RuntimeBattleFieldPresentation.Empty);
                foreach (RuntimeEnemyArtworkSlotView view in artworkViews)
                {
                    view.ApplyNow();
                }
                bool emptyHidden = artworkViews.All(view =>
                    !view.IsShowingArtwork &&
                    !view.InformationBackdrop.gameObject.activeSelf);

                bool valid = allCatalogEntries && fieldApplied && distinct &&
                             emptyHidden;
                if (valid)
                {
                    Debug.Log(
                        "Runtime enemy artwork validation passed: all 12 " +
                        "transparent sprites render distinctly and enemy " +
                        "slots hide artwork again when empty.");
                }
                else
                {
                    Debug.LogError(
                        "Runtime enemy artwork validation failed. " +
                        $"catalog={allCatalogEntries}, field={fieldApplied}, " +
                        $"distinct={distinct}, emptyHidden={emptyHidden}");
                }
                return valid;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static RuntimeBattleFieldPresentation
            CreateArtworkPresentation()
        {
            RuntimeBattleFieldSlotPresentation[] enemies =
            {
                CreateEnemy(0, "TEST-ENEMY-LEFT", "시험 적 · 좌측"),
                CreateEnemy(1, "TEST-ENEMY-CENTER", "시험 적 · 중앙"),
                CreateEnemy(2, "TEST-ENEMY-RIGHT", "시험 적 · 우측")
            };
            return new RuntimeBattleFieldPresentation(enemies, null, null);
        }

        private static RuntimeBattleFieldSlotPresentation CreateEnemy(
            int index,
            string enemyId,
            string title)
        {
            return new RuntimeBattleFieldSlotPresentation(
                RuntimeBattleFieldZone.Enemy,
                index,
                $"{title}\nHP 12/12 · 공격 1",
                "다음 행동: 공격",
                true,
                false,
                true,
                string.Empty,
                $"enemy:{enemyId}",
                null,
                enemyId);
        }
    }
}
