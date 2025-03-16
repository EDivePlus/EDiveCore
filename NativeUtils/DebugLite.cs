// Author: František Holubec
// Created: 09.03.2025

// ReSharper disable once CheckNamespace
public static class DebugLite
{
    private static string GetString(object message)
    {
        if (message == null)
            return "Null";
        return message is System.IFormattable formattable ? formattable.ToString((string) null, (System.IFormatProvider) System.Globalization.CultureInfo.InvariantCulture) : message.ToString();
    }
    
    [System.Diagnostics.Conditional("VERBOSE_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, UnityEngine.LogOption.NoStacktrace, context, "{0}", (object) GetString(message));
    }

    [System.Diagnostics.Conditional("VERBOSE_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, UnityEngine.LogOption.NoStacktrace, null, format, args);
    }
    
    [System.Diagnostics.Conditional("VERBOSE_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Log, UnityEngine.LogOption.NoStacktrace, context, format, args);
    }
    
    [System.Diagnostics.Conditional("ERROR_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Error, UnityEngine.LogOption.NoStacktrace, context, "{0}", (object) GetString(message));
    }

    [System.Diagnostics.Conditional("ERROR_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogErrorFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Error, UnityEngine.LogOption.NoStacktrace, null, format, args);
    }
    
    [System.Diagnostics.Conditional("ERROR_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Error, UnityEngine.LogOption.NoStacktrace, context, format, args);
    }
    
    [System.Diagnostics.Conditional("WARNING_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Warning, UnityEngine.LogOption.NoStacktrace, context, "{0}", (object) GetString(message));
    }

    [System.Diagnostics.Conditional("WARNING_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarningFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Warning, UnityEngine.LogOption.NoStacktrace, null, format, args);
    }
    
    [System.Diagnostics.Conditional("WARNING_LOGS")]
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(UnityEngine.LogType.Warning, UnityEngine.LogOption.NoStacktrace, context, format, args);
    }
}
