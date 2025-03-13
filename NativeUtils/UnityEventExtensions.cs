using System.Text;
using UnityEngine.Events;

namespace EDIVE.NativeUtils
{
    public static class UnityEventExtensions
    {
        public static string GetSummary(this UnityEventBase unityEvent)
        {
            var count = unityEvent.GetPersistentEventCount();
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                var target = unityEvent.GetPersistentTarget(i);
                var methodName = unityEvent.GetPersistentMethodName(i);
                stringBuilder.Append($"{(i == 0 ? "" : ", ")} {(target ? target.name : "no target")}: {methodName}");
            }
            return stringBuilder.ToString();
        }
    }
}
