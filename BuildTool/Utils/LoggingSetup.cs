using UnityEngine;

namespace EDIVE.BuildTool.Utils
{
    [System.Serializable]
    public class LoggingSetup
    {
        [SerializeField]
        private StackTraceLogType _Error = StackTraceLogType.ScriptOnly;
        [SerializeField]
        private StackTraceLogType _Assert = StackTraceLogType.ScriptOnly;
        [SerializeField]
        private StackTraceLogType _Warning = StackTraceLogType.ScriptOnly;
        [SerializeField]
        private StackTraceLogType _Log = StackTraceLogType.ScriptOnly;
        [SerializeField]
        private StackTraceLogType _Exception = StackTraceLogType.ScriptOnly;

        public LoggingSetup() { }
        public LoggingSetup(StackTraceLogType error, StackTraceLogType assert, StackTraceLogType warning, StackTraceLogType log, StackTraceLogType exception)
        {
            _Error = error;
            _Assert = assert;
            _Warning = warning;
            _Log = log;
            _Exception = exception;
        }

        public void Apply()
        {
            Debug.Log($"[LoggingSetup] Applying: Error:{_Error}, Assert:{_Assert}, Warning:{_Warning}, Log:{_Log}, Exception:{_Exception}");
            Application.SetStackTraceLogType(LogType.Error, _Error);
            Application.SetStackTraceLogType(LogType.Assert, _Assert);
            Application.SetStackTraceLogType(LogType.Warning, _Warning);
            Application.SetStackTraceLogType(LogType.Log, _Log);
            Application.SetStackTraceLogType(LogType.Exception, _Exception);
        }

        public static LoggingSetup GetCurrent() =>
            new(Application.GetStackTraceLogType(LogType.Error),
                Application.GetStackTraceLogType(LogType.Assert),
                Application.GetStackTraceLogType(LogType.Warning),
                Application.GetStackTraceLogType(LogType.Log),
                Application.GetStackTraceLogType(LogType.Exception));
    }
}
