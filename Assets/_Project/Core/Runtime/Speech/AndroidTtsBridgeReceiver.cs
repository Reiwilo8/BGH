using System;
using UnityEngine;

namespace Project.Core.Speech
{
    public sealed class AndroidTtsBridgeReceiver : MonoBehaviour
    {
        public Action<string> OnEvent;

        public void OnTtsEvent(string msg)
        {
            OnEvent?.Invoke(msg ?? "");
        }
    }
}