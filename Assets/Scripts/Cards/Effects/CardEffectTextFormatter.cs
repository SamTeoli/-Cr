using System;
using System.Collections.Generic;
using System.Linq;

namespace HaveABreak.Cards
{
    public static class CardEffectTextFormatter
    {
        public static string BuildCardRulesText(
            CardData card,
            string fallbackRulesText = null,
            int level = 0)
        {
            if (card == null)
            {
                return fallbackRulesText ?? string.Empty;
            }

            string authoredText = card.EffectTextAsset?.ResolveRulesText(
                CardTextLocaleProvider.Current,
                level);
            if (!string.IsNullOrWhiteSpace(authoredText))
            {
                return authoredText;
            }

            List<string> lines = card.Effects
                .Where(effect => effect != null)
                .Select(effect => Format(
                    effect,
                    effect.ResolveTargetSpec(card.EffectTargetSpec)))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
            return lines.Count > 0
                ? string.Join("\n", lines)
                : fallbackRulesText ?? card.ResolveRulesText(level);
        }

        public static string Format(
            CardEffectData effect,
            EffectTargetSpec targetSpec = null)
        {
            if (effect == null)
            {
                return string.Empty;
            }

            targetSpec ??= effect.TargetSpec;
            string body = FormatOperation(effect, targetSpec);
            if (string.IsNullOrWhiteSpace(body))
            {
                body = FormatTemplate(
                    effect.Description,
                    effect,
                    targetSpec);
            }
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            return $"{FormatTrigger(effect.Trigger)}{body}";
        }

        private static string FormatOperation(
            CardEffectData effect,
            EffectTargetSpec targetSpec)
        {
            string target = FormatTarget(targetSpec);
            int value = effect.Value;
            return effect.Operation switch
            {
                EffectOperation.Damage when HasTarget(target) =>
                    $"{target}에게 {Math.Abs(value)} 피해를 준다.",
                EffectOperation.Heal when HasTarget(target) =>
                    $"{target}의 체력을 {Math.Abs(value)} 회복한다.",
                EffectOperation.Draw =>
                    $"카드를 {Math.Abs(value)}장 뽑는다.",
                EffectOperation.GainMana =>
                    $"마나를 {SignedIncrease(value)}.",
                EffectOperation.ModifyAttack when HasTarget(target) =>
                    $"{target}의 공격력을 {SignedIncrease(value)}.",
                EffectOperation.ModifyHealth when HasTarget(target) =>
                    $"{target}의 체력을 {SignedIncrease(value)}.",
                EffectOperation.ApplyStatus
                    when HasTarget(target) &&
                         effect.StatusKeyword != StatusKeyword.None =>
                    $"{target}에게 {FormatStatus(effect.StatusKeyword)} " +
                    $"{Math.Abs(value)}을 부여한다" +
                    FormatDuration(effect.Duration),
                EffectOperation.CreateCard =>
                    $"카드를 {Math.Abs(value)}장 생성한다.",
                EffectOperation.Custom =>
                    FormatTemplate(
                        effect.Description,
                        effect,
                        targetSpec),
                _ => null
            };
        }

        private static string FormatTemplate(
            string template,
            CardEffectData effect,
            EffectTargetSpec targetSpec)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return null;
            }

            string target = FormatTarget(targetSpec) ?? string.Empty;
            return template.Trim()
                .Replace(
                    "{target:object}",
                    AttachParticle(target, "을", "를"))
                .Replace(
                    "{target:topic}",
                    AttachParticle(target, "은", "는"))
                .Replace(
                    "{target:subject}",
                    AttachParticle(target, "이", "가"))
                .Replace(
                    "{target}",
                    target)
                .Replace(
                    "{value}",
                    Math.Abs(effect.Value).ToString())
                .Replace(
                    "{signedValue}",
                    effect.Value.ToString("+0;-0;0"))
                .Replace(
                    "{status}",
                    FormatStatus(effect.StatusKeyword))
                .Replace(
                    "{duration}",
                    effect.Duration.ToString());
        }

        private static string AttachParticle(
            string text,
            string withFinalConsonant,
            string withoutFinalConsonant)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            char last = text[text.Length - 1];
            bool hasFinalConsonant =
                last >= '\uAC00' &&
                last <= '\uD7A3' &&
                (last - '\uAC00') % 28 != 0;
            return text +
                   (hasFinalConsonant
                       ? withFinalConsonant
                       : withoutFinalConsonant);
        }

        private static string FormatTrigger(EffectTrigger trigger)
        {
            return trigger switch
            {
                EffectTrigger.OnUse => "사용할 때, ",
                EffectTrigger.OnSummoned => "소환될 때, ",
                EffectTrigger.OnDestroyed => "파괴될 때, ",
                EffectTrigger.OnAttack => "공격할 때, ",
                EffectTrigger.OnAttacked => "공격받을 때, ",
                EffectTrigger.TurnStart => "턴 시작 시, ",
                EffectTrigger.TurnEnd => "턴 종료 시, ",
                EffectTrigger.EnemyTurnStart => "적 턴 시작 시, ",
                EffectTrigger.Persistent => "지속: ",
                _ => string.Empty
            };
        }

        private static string FormatTarget(EffectTargetSpec spec)
        {
            if (spec == null)
            {
                return null;
            }

            string noun = spec.Kind switch
            {
                EffectTargetKind.Monster
                    when spec.Side == EffectTargetSide.Enemy => "적",
                EffectTargetKind.Monster
                    when spec.Side == EffectTargetSide.Ally => "아군 몬스터",
                EffectTargetKind.Monster => "몬스터",
                EffectTargetKind.Player => "플레이어",
                EffectTargetKind.HandCard => "손패의 다른 카드",
                EffectTargetKind.GraveyardCard => "무덤의 카드",
                EffectTargetKind.SkillFieldCard => "스킬 필드의 카드",
                EffectTargetKind.Zone => "구역",
                _ => "대상"
            };
            string unit = spec.Kind == EffectTargetKind.HandCard ||
                          spec.Kind == EffectTargetKind.GraveyardCard ||
                          spec.Kind == EffectTargetKind.SkillFieldCard
                ? "장"
                : "개";
            if (spec.MaximumCount == 1)
            {
                return $"{noun} 1{unit}";
            }

            if (spec.Optional && spec.MinimumCount <= 1)
            {
                return $"최대 {spec.MaximumCount}{unit}의 {noun}";
            }

            return spec.MinimumCount == spec.MaximumCount
                ? $"{noun} {spec.MaximumCount}{unit}"
                : $"{noun} {spec.MinimumCount}~{spec.MaximumCount}{unit}";
        }

        private static string FormatStatus(StatusKeyword status)
        {
            return status switch
            {
                StatusKeyword.Injury => "부상",
                StatusKeyword.Weaken => "약화",
                StatusKeyword.Vulnerable => "취약",
                StatusKeyword.Bind => "속박",
                StatusKeyword.Stun => "기절",
                _ => status.ToString()
            };
        }

        private static string SignedIncrease(int value)
        {
            return value >= 0
                ? $"{value} 증가시킨다"
                : $"{Math.Abs(value)} 감소시킨다";
        }

        private static string FormatDuration(int duration)
        {
            return duration > 0
                ? $"({duration}턴)."
                : ".";
        }

        private static bool HasTarget(string target)
        {
            return !string.IsNullOrWhiteSpace(target);
        }
    }
}
