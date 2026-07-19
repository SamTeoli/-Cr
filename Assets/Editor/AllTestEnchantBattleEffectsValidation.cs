using System;
using UnityEditor;
using UnityEngine;

namespace HaveABreak.Editor
{
    internal static class AllTestEnchantBattleEffectsValidation
    {
        [MenuItem("Have a Break/Validate All Test Enchant Battle Effects E01-E08")]
        private static void ValidateFromMenu()
        {
            Validate(true);
        }

        internal static bool Validate(bool showDialog)
        {
            bool valid = true;
            valid &= Run("E01-E08 data", () => TestEnchantDataBuilder.ValidateTestEnchants(false));
            valid &= Run("E01-E08 compatibility", () => TestEnchantCompatibilityBuilder.Validate(false));
            valid &= Run("E01 Warm Seat", () => EnchantWarmSeatValidation.Validate(false));
            valid &= Run("E02 Worn Handle", () => EnchantWornHandleValidation.Validate(false));
            valid &= Run("E03 Round Trip Ticket", () => EnchantRoundTripTicketValidation.Validate(false));
            valid &= Run("E04 Backup Power", () => EnchantBackupPowerValidation.Validate(false));
            valid &= Run("E05 Rusty Announcement", () => EnchantRustyAnnouncementValidation.Validate(false));
            valid &= Run("E06 Starlight Engraving", () => EnchantStarlightEngravingValidation.Validate(false));
            valid &= Run("E07 Transfer Stamp", () => EnchantTransferStampValidation.Validate(false));
            valid &= Run("E08 Route Pin", () => EnchantRoutePinValidation.Validate(false));

            if (!valid)
            {
                Debug.LogError("All test enchant battle effects validation failed.");
            }
            else
            {
                Debug.Log("All test enchant battle effects E01-E08 passed.");
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "All Test Enchant Battle Effects Validation",
                    valid
                        ? "All test enchant battle effects E01-E08 passed."
                        : "All test enchant battle effects failed. Check the Console.",
                    "OK");
            }

            return valid;
        }

        private static bool Run(string label, Func<bool> validation)
        {
            try
            {
                bool passed = validation();
                if (!passed)
                {
                    Debug.LogError($"Enchant regression failed: {label}.");
                }

                return passed;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Enchant regression threw an exception: {label}.\n{exception}");
                return false;
            }
        }
    }
}
