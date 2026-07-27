using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Rippies.Reveal
{
    public sealed class NativeRevealBridge : MonoBehaviour
    {
        [SerializeField] private PackRipController controller;

        public void PrepareReveal(string payloadJson)
        {
            controller?.PrepareReveal(RevealPayload.FromJson(payloadJson));
        }

        public void BeginReveal(string unused)
        {
            controller?.BeginReveal();
        }

public void SkipReveal(string unused)
        {
            controller?.SkipReveal();
        }


        public void PauseReveal(string paused)
        {
            Time.timeScale = string.Equals(paused, "true", StringComparison.OrdinalIgnoreCase) ? 0f : 1f;
        }

        public void SetMuted(string muted)
        {
            AudioListener.volume = string.Equals(muted, "true", StringComparison.OrdinalIgnoreCase) ? 0f : 1f;
        }

        public void DisposeReveal(string unused)
        {
            controller?.ResetReveal();
        }

        public static void Emit(string eventName, string value)
        {
            string payload = JsonUtility.ToJson(new NativeEvent(eventName, value));

#if UNITY_IOS && !UNITY_EDITOR
            RippiesUnityEvent(payload);
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    activity.Call("onUnityRevealEvent", payload);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Android bridge unavailable: " + exception.Message);
            }
#else
            Debug.Log("[RippiesBridge] " + payload);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RippiesUnityEvent(string payload);
#endif

        [Serializable]
        private sealed class NativeEvent
        {
            public string eventName;
            public string value;

            public NativeEvent(string eventNameValue, string eventValue)
            {
                eventName = eventNameValue;
                value = eventValue;
            }
        }
    }
}