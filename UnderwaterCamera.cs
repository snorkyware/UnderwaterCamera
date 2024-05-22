using System;
using BepInEx;
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
            Version = "0.0.8908.35621";

        private static void Log(string msg) => Debug.Log("[" + Guid + "]" + msg);

        private void Awake()
        {
            new Harmony(Guid + ".harmony").PatchAll();
            Log("Awake");
        }
        
        // Underwater Camera
        // A patch that entirely reimplements the method is surely easier and more stable than transpilation, in all but
        // the most trivial cases.
        [HarmonyPatch(typeof(GameCamera))]
        private class GameCameraPatch
        {
            private static readonly Type GameCameraType = typeof(GameCamera);
                    
            private delegate Vector3 GetOffsetedEyePosDelegate(GameCamera instance);
            private static readonly GetOffsetedEyePosDelegate GetOffsetedEyePos = AccessTools.MethodDelegate
                <GetOffsetedEyePosDelegate>(AccessTools.Method(GameCameraType, "GetOffsetedEyePos"));
            
            private delegate void CollideRay2Delegate(GameCamera instance, Vector3 eyePos, Vector3 offsetedEyePos, 
                ref Vector3 end);
            private static readonly CollideRay2Delegate CollideRay2 = AccessTools.MethodDelegate<CollideRay2Delegate>
                (AccessTools.Method(GameCameraType, "CollideRay2"));
    
            private delegate void UpdateNearClippingDelegate(GameCamera instance, Vector3 eyePos, Vector3 camPos, 
                float dt);
            private static readonly UpdateNearClippingDelegate UpdateNearClipping = AccessTools.MethodDelegate
                <UpdateNearClippingDelegate>(AccessTools.Method(GameCameraType, "UpdateNearClipping"));
    
            private delegate void ApplyCameraTiltDelegate(GameCamera instance, Player player, float dt, 
                ref Quaternion rot);
            private static readonly ApplyCameraTiltDelegate ApplyCameraTilt = AccessTools.MethodDelegate
                <ApplyCameraTiltDelegate>(AccessTools.Method(GameCameraType, "ApplyCameraTilt"));

            [HarmonyPrefix, HarmonyPatch("GetCameraPosition")]
            private static bool GetCameraPositionPrefix(GameCamera __instance, float ___m_distance, 
                ref bool ___m_waterClipping, bool ___m_shipCameraTilt, float dt, out Vector3 pos, out Quaternion rot)
            {
                Player localPlayer = Player.m_localPlayer;
                if (localPlayer == null)
                {
                    Transform transform = __instance.transform;
                    pos = transform.position;
                    rot = transform.rotation;
                }
                else
                {
                    Vector3 eyePos;
                    Vector3 forward = -localPlayer.m_eye.transform.forward;
                    float a = ___m_distance;
                    if (localPlayer.InIntro())
                    {
                        eyePos = localPlayer.transform.position;
                        a = __instance.m_flyingDistance;
                    }
                    else
                    {
                        eyePos = GetOffsetedEyePos(__instance);
                        if (__instance.m_smoothYTilt)
                            a = Mathf.Lerp(a, 1.5f, Utils.SmoothStep(0.0f, -0.5f, forward.y));
                    }
                    Vector3 end = eyePos + forward * a;
                    CollideRay2(__instance, localPlayer.m_eye.position, eyePos, ref end);
                    UpdateNearClipping(__instance, eyePos, end, dt);
                    // Water level clamping removed.
                    ___m_waterClipping = false;
                    pos = end;
                    rot = localPlayer.m_eye.transform.rotation;
                    if (___m_shipCameraTilt) ApplyCameraTilt(__instance, localPlayer, dt, ref rot);
                }
                return false;
            }
        }
        
        

    }
}
