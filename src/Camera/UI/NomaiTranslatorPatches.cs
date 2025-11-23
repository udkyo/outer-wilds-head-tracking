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
                return;
            }

            var translatorUpdateMethod = AccessTools.Method(nomaiTranslatorType, "Update");
            if (translatorUpdateMethod != null)
            {
                var translatorPrefix = new HarmonyMethod(AccessTools.Method(typeof(NomaiTranslatorPatches), nameof(NomaiTranslator_Update_Prefix)));
                var translatorPostfix = new HarmonyMethod(AccessTools.Method(typeof(NomaiTranslatorPatches), nameof(NomaiTranslator_Update_Postfix)));
                harmony.Patch(translatorUpdateMethod, prefix: translatorPrefix, postfix: translatorPostfix);
            }
        }

        public static void NomaiTranslator_Update_Prefix(object __instance)
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                if (_translatorRaycastTransform == null)
                {
                    var raycastField = AccessTools.Field(__instance.GetType(), "_raycastTransform");
                    if (raycastField != null)
                    {
                        _translatorRaycastTransform = raycastField.GetValue(__instance) as UnityCoreModule::UnityEngine.Transform;
                    }
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
            catch (System.Exception)
            {
                // Silently handle errors - reflection-based patching may fail if game updates
            }
        }

        public static void NomaiTranslator_Update_Postfix()
        {
            try
            {
                if (!_translatorRotationModified) return;
                if (_translatorRaycastTransform == null) return;

                // Restore the saved rotation
                _translatorRaycastTransform.rotation = _translatorSavedRotation;
                _translatorRotationModified = false;
            }
            catch (System.Exception)
            {
                // Silently handle errors - don't crash if rotation restore fails
            }
        }
    }

    /// <summary>
    /// Patches NomaiTranslatorProp to capture canvas reference for head tracking
    /// </summary>
    [HarmonyPatch(typeof(NomaiTranslatorProp))]
    public static class TranslatorCanvasPatch
    {
        public static UnityCoreModule::UnityEngine.Transform? _canvasTransform = null;

        [HarmonyPatch("OnEquipTool")]
        [HarmonyPostfix]
        public static void OnEquipTool_Postfix(NomaiTranslatorProp __instance)
        {
            try
            {
                var canvasField = AccessTools.Field(typeof(NomaiTranslatorProp), "_canvas");
                if (canvasField == null) return;

                var canvas = canvasField.GetValue(__instance);
                if (canvas == null) return;

                var transformProp = canvas.GetType().GetProperty("transform");
                if (transformProp != null)
                {
                    _canvasTransform = transformProp.GetValue(canvas) as UnityCoreModule::UnityEngine.Transform;
                }
            }
            catch (System.Exception)
            {
                // Silently handle errors - reflection-based patching may fail if game updates
            }
        }

        [HarmonyPatch("OnFinishUnequipAnimation")]
        [HarmonyPostfix]
        public static void OnFinishUnequipAnimation_Postfix()
        {
            _canvasTransform = null!;
        }

    }
}
