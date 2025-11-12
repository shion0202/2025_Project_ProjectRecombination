using UnityEngine;

namespace Kamgam.PowerPivot
{
    public class Logger
    {
        public delegate void LogCallback(string msg, LogLevel logLevel);
        
        public const string Prefix = "PowerPivot: ";
        public static LogLevel CurrentLogLevel = LogLevel.Warning;

        /// <summary>
        /// Optional: leave as is or set to NULL to not use it.<br />
        /// Set this to a function which returns the log level (from settings for example).<br />
        /// This will be called before every log.
        /// <example>
        /// [RuntimeInitializeOnLoadMethod]
        /// private static void HookUpToLogger()
        /// {
        ///     Logger.OnGetLogLevel = () => GetOrCreateSettings().LogLevel;
        /// }
        /// </example>
        /// </summary>
        public static System.Func<LogLevel> OnGetLogLevel = null;

        public enum LogLevel
		{
			Log     =  0,
			Warning =  1,
			Error   =  2,
			Message =  3,
			NoLogs  = 99
		}

        public static bool IsLogLevelVisible(LogLevel logLevel)
        {
            return (int)logLevel >= (int)CurrentLogLevel;
        }

        public static void UpdateCurrentLogLevel()
        {
            if (OnGetLogLevel != null)
            {
                CurrentLogLevel = OnGetLogLevel();
            }
        }

        public static void Log(string message)
        {
            UpdateCurrentLogLevel();
            if(IsLogLevelVisible(LogLevel.Log))
                Debug.Log(Prefix + message);
        }

        public static void LogWarning(string message)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Warning))
                Debug.LogWarning(Prefix + message);
        }

        public static void LogError(string message)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Error))
                Debug.LogError(Prefix + message);
        }

        public static void LogMessage(string message)
        {
            UpdateCurrentLogLevel();
            if (IsLogLevelVisible(LogLevel.Message))
                Debug.Log(Prefix + message);
        }
    }
}
