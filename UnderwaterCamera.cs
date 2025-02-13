using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace UnderwaterCamera
{
    [BepInPlugin(Guid, Name, Version)]
    public class UnderwaterCamera : BaseUnityPlugin
    {
        private const string 
            Guid = "com.snorkyware.valheim.underwatercamera",
            Name = "UnderwaterCamera",
            Version = "0.0.8909.37615";
        
        private static void Log(string msg) => Debug.Log("[" + Name + "]" + msg);

        private static ConfigEntry<bool> _flyModeOnly;
        
        private void Awake()
        {
            new Harmony(Guid + ".harmony").PatchAll(typeof(UnderwaterCamera));
            _flyModeOnly = Config.Bind("General", "flyModeOnly", true, new ConfigDescription(
                "Only activate the ability to see underwater when in fly mode"));
            Log("Awake");
        }

        // Underwater Camera
        // A patch that entirely reimplements the method is surely easier and more stable than transpilation,
        // in all but the most trivial cases.
        [HarmonyPrefix, HarmonyPatch(typeof(GameCamera), nameof(GameCamera.GetCameraPosition))]
        private static bool GetCameraPositionPrefix(float dt, ref Vector3 pos, ref Quaternion rot,
            GameCamera __instance)
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer == null)
            {
                Transform transform = __instance.transform;
                pos = transform.position;
                rot = transform.rotation;
                return false;
            }

            if (_flyModeOnly.Value && !localPlayer.IsDebugFlying()) return true;

            Vector3 eyePos;
            Vector3 forward = -localPlayer.m_eye.transform.forward;
            float a = __instance.m_distance;
            if (localPlayer.InIntro())
            {
                eyePos = localPlayer.transform.position;
                a = __instance.m_flyingDistance;
            }
            else
            {
                eyePos = __instance.GetOffsetedEyePos();
                if (__instance.m_smoothYTilt)
                    a = Mathf.Lerp(a, 1.5f, Utils.SmoothStep(0.0f, -0.5f, forward.y));
            }
            Vector3 end = eyePos + forward * a;
            __instance.CollideRay2(localPlayer.m_eye.position, eyePos, ref end);
            __instance.UpdateNearClipping(eyePos, end, dt);
            
            // Water level clamping removed.
            
            __instance.m_waterClipping = false;
            pos = end;
            rot = localPlayer.m_eye.transform.rotation;
            if (__instance.m_shipCameraTilt) __instance.ApplyCameraTilt(localPlayer, dt, ref rot);
            
            return false;
        }
    }
}
