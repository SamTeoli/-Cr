using System;
using System.Linq;
using System.Reflection;
using HaveABreak.Cards;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class AllCardAndEnchantRegressionValidation
    {
        private const BindingFlags PrivateStatic =
            BindingFlags.NonPublic | BindingFlags.Static;

        [MenuItem("Have a Break/Validate All Cards C01-C12 And Enchants E01-E08")]
        private static void ValidateFromMenu()
        {
            bool valid = Validate();
            EditorUtility.DisplayDialog(
                "All Cards And Enchants Regression Validation",
                valid
                    ? "All cards C01-C12 and enchants E01-E08 passed."
                    : "Full regression failed. Check the Console.",
                "OK");
        }

        internal static bool Validate()
        {
            CardData c01 = FindCard("C01");
            CardData c04 = FindCard("C04");
            CardData c05 = FindCard("C05");
            CardData c06 = FindCard("C06");
            CardData c07 = FindCard("C07");
            CardData c08 = FindCard("C08");
            CardData c09 = FindCard("C09");
            CardData c10 = FindCard("C10");
            CardData c11 = FindCard("C11");
            CardData c12 = FindCard("C12");

            bool valid = true;
            valid &= Run("C01 Sleeper Keeper", () => C01SleeperKeeperValidation.Validate(false));
            valid &= Run("C02 Lantern Bearer", () => C02LanternBearerValidation.Validate(false));
            valid &= Run("C03 Seat Repairer", () => C03SeatRepairerValidation.Validate(false));
            valid &= Run("C04 Terminal Cat", () =>
                Invoke(typeof(C04TerminalCatValidation), "ValidateLevelThree", c04) &&
                Invoke(typeof(C04TerminalCatValidation), "ValidateLevelFive", c04));
            valid &= Run("C05 Platform Push", () =>
                Invoke(typeof(C05PlatformPushValidation), "Validate", c05, 1, false) &&
                Invoke(typeof(C05PlatformPushValidation), "Validate", c05, 5, true));
            valid &= Run("C06 Emergency Brake", () =>
                Invoke(typeof(C06EmergencyBrakeValidation), "ValidateLevelFive", c06) &&
                Invoke(typeof(C06EmergencyBrakeValidation), "ValidateImmunity", c06));
            valid &= Run("C07 Lost Ticket", () =>
                Invoke(typeof(C07LostTicketValidation), "Validate", c07, c01));
            valid &= Run("C08 Closing Door", () =>
                Invoke(typeof(C08ClosingDoorValidation), "Validate", c08));
            valid &= Run("C09 Inspection Blanket", () =>
                Invoke(typeof(C09InspectionBlanketValidation), "Validate", c09, c01));
            valid &= Run("C10 Broken Call Line", () =>
                Invoke(typeof(C10BrokenCallLineValidation), "Validate", c10));
            valid &= Run("C11 Late Night Waiting Room", () =>
                Invoke(typeof(C11LateNightWaitingRoomValidation), "Validate", c11, c01));
            valid &= Run("C12 Route Map Starlight", () =>
                Invoke(typeof(C12RouteMapStarlightValidation), "ValidateLevelOne", c12) &&
                Invoke(typeof(C12RouteMapStarlightValidation), "ValidateLevelFive", c12));
            valid &= Run("E01-E08 test enchants", () =>
                AllTestEnchantBattleEffectsValidation.Validate(false));

            if (valid)
            {
                Debug.Log("Full regression C01-C12 and E01-E08 passed.");
            }
            else
            {
                Debug.LogError("Full regression C01-C12 and E01-E08 failed.");
            }

            return valid;
        }

        private static bool Invoke(Type validationType, string methodName, params object[] arguments)
        {
            MethodInfo method = validationType.GetMethod(methodName, PrivateStatic);
            if (method == null)
            {
                throw new MissingMethodException(validationType.FullName, methodName);
            }

            object result = method.Invoke(null, arguments);
            return result is bool passed && passed;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (!passed)
                {
                    Debug.LogError($"Regression failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Regression threw an exception: {label}.\n{exception}");
                return false;
            }
        }

        private static CardData FindCard(string id)
        {
            return AssetDatabase.FindAssets("t:CardData")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<CardData>(path))
                .FirstOrDefault(card => card != null &&
                    string.Equals(card.CatalogCardId, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
