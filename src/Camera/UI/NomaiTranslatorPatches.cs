extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Camera.UI
{
    /// <summary>
    /// Patches for Nomai Translator tool - ensures translator raycasting uses head direction
    /// </summary>
    public static class NomaiTranslatorPatches
    {
        private static UnityCoreModule::UnityEngine.Transform? _translatorRaycastTransform = null;
        private static Quaternion _translatorSavedRotation = Quaternion.identity;
        private static bool _translatorRotationModified = false;

        public static void ApplyPatches(Harmony harmony)
        {
            var nomaiTranslatorType = AccessTools.TypeByName("NomaiTranslator");
            if (nomaiTranslatorType == null)
            {
                throw new InvalidOperationException("Could not find NomaiTranslator type!");
            }

            var translatorUpdateMethod = AccessTools.Method(nomaiTranslatorType, "Update");
            if (translatorUpdateMethod == null)
            {
                throw new InvalidOperationException("Could not find NomaiTranslator.Update method!");
            }

            var translatorPrefix = AccessTools.Method(typeof(NomaiTranslatorPatches), nameof(NomaiTranslator_Update_Prefix));
            var translatorPostfix = AccessTools.Method(typeof(NomaiTranslatorPatches), nameof(NomaiTranslator_Update_Postfix));

            if (translatorPrefix == null || translatorPostfix == null)
            {
                throw new InvalidOperationException("Could not find NomaiTranslatorPatches prefix/postfix methods!");
            }

            harmony.Patch(translatorUpdateMethod,
                prefix: new HarmonyMethod(translatorPrefix),
                postfix: new HarmonyMethod(translatorPostfix));
        }

        public static void NomaiTranslator_Update_Prefix(object __instance)
        {
            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            if (_translatorRaycastTransform == null)
            {
                var raycastField = AccessTools.Field(__instance.GetType(), "_raycastTransform");
                if (raycastField == null)
                {
                    throw new InvalidOperationException("Could not find _raycastTransform field on NomaiTranslator!");
                }
                _translatorRaycastTransform = raycastField.GetValue(__instance) as UnityCoreModule::UnityEngine.Transform;
            }

            if (_translatorRaycastTransform == null) return;

            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
            if (headTracking == Quaternion.identity) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            if (baseRotation == default || baseRotation == Quaternion.identity) return;

            _translatorSavedRotation = _translatorRaycastTransform.rotation;
            _translatorRaycastTransform.rotation = baseRotation * headTracking;
            _translatorRotationModified = true;
        }

        public static void NomaiTranslator_Update_Postfix()
        {
            if (!_translatorRotationModified) return;
            if (_translatorRaycastTransform == null) return;

            _translatorRaycastTransform.rotation = _translatorSavedRotation;
            _translatorRotationModified = false;
        }
    }
}
