using System;
using System.Collections.Generic;
using System.Linq;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class TestEnchantDataBuilder
    {
        private const string EnchantFolder = "Assets/GameData/Enchants";
        private const string DatabasePath = "Assets/GameData/EnchantDatabase.asset";
        private const string SourceVersion = "시험 콘텐츠 / v0.4 / 2026-07-18";

        private sealed class Definition
        {
            public string Id;
            public string Name;
            public CardRarity Rarity;
            public EnchantApplicationType Type;
            public string Role;
            public CardType[] CompatibleTypes;
            public string CompatibilityRule;
            public string RulesText;
            public string SourceDocument;
            public EnchantEffectData[] Effects;
        }

        public static void RebuildTestEnchants(bool showDialog)
        {
            EnsureFolder(EnchantFolder);
            List<EnchantData> assets = new();
            foreach (Definition definition in CreateDefinitions())
            {
                string path = $"{EnchantFolder}/{definition.Id}_{definition.Name}.asset";
                EnchantData asset = AssetDatabase.LoadAssetAtPath<EnchantData>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<EnchantData>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.EditorInitialize(
                    definition.Id,
                    definition.Name,
                    definition.Rarity,
                    definition.CompatibleTypes,
                    false,
                    definition.Type,
                    definition.Role,
                    definition.CompatibilityRule,
                    definition.RulesText,
                    SourceVersion,
                    definition.SourceDocument,
                    definition.Effects);
                EditorUtility.SetDirty(asset);
                assets.Add(asset);
            }

            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<EnchantDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.EditorSetEnchants(assets.OrderBy(asset => asset.DefinitionId));
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Test Enchant Data",
                    "Created or rebuilt E01-E08 and EnchantDatabase.",
                    "OK");
            }
        }

        public static bool ValidateTestEnchants(bool showDialog)
        {
            Definition[] definitions = CreateDefinitions();
            List<EnchantData> all = AssetDatabase.FindAssets("t:EnchantData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<EnchantData>(path))
                .Where(asset => asset != null)
                .ToList();
            bool valid = true;

            foreach (Definition expected in definitions)
            {
                List<EnchantData> matches = all.Where(asset => string.Equals(
                    asset.DefinitionId, expected.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                valid &= matches.Count == 1;
                if (matches.Count != 1)
                {
                    Debug.LogError($"Expected exactly one enchant '{expected.Id}', found {matches.Count}.");
                    continue;
                }

                EnchantData actual = matches[0];
                bool entryValid = actual.DisplayName == expected.Name &&
                                  actual.Rarity == expected.Rarity &&
                                  actual.ApplicationType == expected.Type &&
                                  actual.Role == expected.Role &&
                                  actual.AdditionalCompatibilityRule == expected.CompatibilityRule &&
                                  actual.RulesText == expected.RulesText &&
                                  actual.SourceVersion == SourceVersion &&
                                  actual.SourceDocument == expected.SourceDocument &&
                                  !actual.AllowDuplicateOnSameCard &&
                                  actual.CompatibleCardTypes.SequenceEqual(expected.CompatibleTypes) &&
                                  EffectsMatch(actual.Effects, expected.Effects);
                valid &= entryValid;
                if (!entryValid)
                {
                    Debug.LogError($"Enchant data mismatch: {expected.Id} {expected.Name}", actual);
                }
            }

            bool idsUnique = all
                .Where(asset => definitions.Any(definition => string.Equals(
                    definition.Id, asset.DefinitionId, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(asset => asset.DefinitionId, StringComparer.OrdinalIgnoreCase)
                .All(group => group.Count() == 1);
            valid &= idsUnique;

            EnchantDatabase database = AssetDatabase.LoadAssetAtPath<EnchantDatabase>(DatabasePath);
            bool databaseValid = database != null && database.Enchants.Count == definitions.Length &&
                                 definitions.All(definition => database.Find(definition.Id) != null);
            valid &= databaseValid;
            if (!databaseValid)
            {
                Debug.LogError("EnchantDatabase must contain E01-E08 exactly once.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Test Enchant Validation",
                    valid ? "Test enchants E01-E08 passed." : "Test enchant validation failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool EffectsMatch(
            IReadOnlyList<EnchantEffectData> actual,
            IReadOnlyList<EnchantEffectData> expected)
        {
            if (actual.Count != expected.Count)
            {
                return false;
            }

            for (int i = 0; i < actual.Count; i++)
            {
                if (actual[i] == null || expected[i] == null ||
                    actual[i].Trigger != expected[i].Trigger ||
                    actual[i].ConditionAndTarget != expected[i].ConditionAndTarget ||
                    actual[i].Resolution != expected[i].Resolution ||
                    actual[i].Limitation != expected[i].Limitation)
                {
                    return false;
                }
            }

            return true;
        }

        private static Definition[] CreateDefinitions()
        {
            CardType[] allTypes =
                { CardType.Monster, CardType.Skill, CardType.Trap, CardType.Barrier };
            return new[]
            {
                Define(
                    "E01", "따뜻한 좌석", CardRarity.Common,
                    EnchantApplicationType.StaticModifier, "생존",
                    new[] { CardType.Monster }, "기본 생명력이 존재",
                    "최대 생명력 +2.",
                    "https://docs.google.com/document/d/1kn9eWeo6TRRtRzyjoq5DTPChUGqVTXLu67qKeWB6KW8/edit",
                    Effect("정적 적용", "장착 카드가 몬스터이고 인첸트가 활성",
                        "최대 생명력 +2. 장착 시 현재 생명력도 +2.",
                        "제거·비활성 시 최대 생명력 -2 후 현재 생명력 상한 정리")),
                Define(
                    "E02", "닳은 손잡이", CardRarity.Common,
                    EnchantApplicationType.PostTrigger, "공격 / 반격",
                    new[] { CardType.Monster }, "기본 공격 능력 존재",
                    "공격을 완료한 뒤 반격 1을 얻는다. 플레이어 턴당 1회.",
                    "https://docs.google.com/document/d/1JBdtUGW3HOGgY6H9sBqHswpIY185WVvuCZewgaHUOh4/edit",
                    Effect("공격 완료 사건", "장착 몬스터가 공격을 실제 완료 / 플레이어 턴당 1회",
                        "장착 몬스터에게 반격 1을 부여합니다.",
                        "공격이 취소·무효·대상 상실로 완료되지 않으면 미발동")),
                Define(
                    "E03", "구겨진 왕복 승차권", CardRarity.Rare,
                    EnchantApplicationType.PostTrigger, "자원 / 드로우",
                    allTypes, "본 효과 완료 사건 존재",
                    "장착 카드의 본 효과 뒤 현재 마력이 0이면 카드 1장을 드로우한다. 턴당 1회.",
                    "https://docs.google.com/document/d/1icMWm_AT1lqImoEMU1FApTjQpE4h0iVGlpRiHWxZbFQ/edit",
                    Effect("본 효과 완료 사건", "장착 카드 본 효과 완료 / 현재 마력 0 / 플레이어 턴당 1회",
                        "카드 1장을 드로우합니다.",
                        "패 10장이면 드로우 실패하고 카드는 덱 맨 위에 남음")),
                Define(
                    "E04", "예비 전원", CardRarity.Common,
                    EnchantApplicationType.StaticModifier, "자원 / 비용 감소",
                    new[] { CardType.Skill }, "수록 기본 비용 2 이상",
                    "비용이 1 감소한다. 이 효과로는 1 미만이 되지 않는다.",
                    "https://docs.google.com/document/d/1QXaRyXQHN0lbaosr86JN08mlePuDNsI-Pi6Yq33l774/edit",
                    Effect("카드 비용 계산", "활성 상태의 장착 스킬",
                        "마력 비용을 1 낮추고 E04 적용 직후 최소 1로 제한합니다.",
                        "카드 자체의 비용 설정 효과가 있으면 설정 뒤에 적용합니다.")),
                Define(
                    "E05", "녹슨 안내방송", CardRarity.Common,
                    EnchantApplicationType.PostTrigger, "상태 / 트랩 특화",
                    new[] { CardType.Trap }, "적 대상·영향 효과 존재",
                    "장착 트랩이 적에게 영향을 준 뒤 영향을 받은 각 적에게 약화 1을 부여한다.",
                    "https://docs.google.com/document/d/1P8QU173hx_PV3AJi741zU1bNKVhpUEvT0QMA9NXJcg0/edit",
                    Effect("트랩 본 효과 완료 사건", "장착 트랩이 적 1개 이상에게 실제 영향",
                        "영향을 받은 각 적에게 약화 1을 부여합니다.",
                        "트랩 효과가 무효화되거나 모든 대상이 사라지면 미발동합니다.")),
                Define(
                    "E06", "별빛 각인", CardRarity.Rare,
                    EnchantApplicationType.StaticModifier, "결계 특화 / 수치 강화",
                    new[] { CardType.Barrier }, "수치형 반복 효과 존재",
                    "반복 효과의 첫 수치가 1 증가한다.",
                    "https://docs.google.com/document/d/1eSWNDwZRF41ryU0JS0QxO43i5OqLeqaBQQPgNpXCYtw/edit",
                    Effect("반복 효과 명령 생성", "활성 상태의 장착 결계 / 수치형 반복 효과",
                        "해당 반복 효과의 첫 수치를 1 증가시킵니다.",
                        "대상 수·발동 횟수·조건 기준값은 증가하지 않습니다.")),
                Define(
                    "E07", "환승 도장", CardRarity.Rare,
                    EnchantApplicationType.PreReplacement, "묘지·순환",
                    new[] { CardType.Skill, CardType.Trap }, "정상 처리 후 묘지 이동",
                    "전투당 처음 묘지로 갈 때 대신 드로우 더미 맨 아래로 간다.",
                    "https://docs.google.com/document/d/1veq88O0iS2-4FS_PJQypJMXbGj3FICYs0I5nLdmwaPk/edit",
                    Effect("장착 카드 묘지 이동 직전", "정상 처리 후 묘지 이동 / 전투당 첫 1회",
                        "묘지 대신 드로우 더미 맨 아래로 이동시킵니다.",
                        "묘지 이동 사건은 발생하지 않습니다."),
                    Effect("전투 시작 사건", "장착 카드의 새 전투카드",
                        "전투당 사용 표식을 미사용으로 초기화합니다.",
                        "전투 종료 시 표식을 폐기합니다.")),
                Define(
                    "E08", "노선 고정핀", CardRarity.Legendary,
                    EnchantApplicationType.RuleChange, "대상 방식 / 위치",
                    allTypes, "적 1개 고정 대상 효과",
                    "적 고정 대상 효과가 위치 대상 효과로 바뀐다.",
                    "https://docs.google.com/document/d/1NjzwzmYLOkf9A1vEPp18Ycq_e5MuQ628HOS7RU1BD4Q/edit",
                    Effect("대상 선언 사건", "장착 카드의 적 1개 고정 대상 효과",
                        "선택한 적의 현재 위치를 기록하고 위치 대상으로 변경합니다.",
                        "선언 당시 빈 위치를 선택할 수 없습니다."),
                    Effect("효과 해결 사건", "기록한 위치에 적 존재",
                        "현재 그 위치를 점유한 적에게 효과를 적용합니다.",
                        "빈 위치면 실패하며 다른 위치로 재탐색하지 않습니다."))
            };
        }

        private static Definition Define(
            string id,
            string name,
            CardRarity rarity,
            EnchantApplicationType type,
            string role,
            CardType[] compatibleTypes,
            string compatibilityRule,
            string rulesText,
            string sourceDocument,
            params EnchantEffectData[] effects)
        {
            return new Definition
            {
                Id = id,
                Name = name,
                Rarity = rarity,
                Type = type,
                Role = role,
                CompatibleTypes = compatibleTypes,
                CompatibilityRule = compatibilityRule,
                RulesText = rulesText,
                SourceDocument = sourceDocument,
                Effects = effects
            };
        }

        private static EnchantEffectData Effect(
            string trigger,
            string condition,
            string resolution,
            string limitation)
        {
            return new EnchantEffectData(trigger, condition, resolution, limitation);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
