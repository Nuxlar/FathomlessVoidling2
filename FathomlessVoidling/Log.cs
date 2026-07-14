using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace FathomlessVoidling
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        static string getLogPrefix(string callerPath, string callerMemberName, int callerLineNumber)
        {
            const string MOD_NAME = nameof(FathomlessVoidling);

            int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
            if (modNameLastPathIndex >= 0)
            {
                callerPath = callerPath.Substring(modNameLastPathIndex + MOD_NAME.Length + 1);
            }

            return $"{callerPath}:{callerLineNumber} ({callerMemberName}) ";
        }

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogError(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
        }
    }
}