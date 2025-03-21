// Author: František Holubec
// Created: 14.04.2022

using System;

namespace EDIVE.Utils
{
    /// <summary>
    /// Interface for reporting Service Messages to TeamCity
    /// See https://www.jetbrains.com/help/teamcity/service-messages.html#Blocks+of+Service+Messages
    /// </summary>
    public static class TeamCityServiceMessages
    {
        /// <summary>
        /// For reporting of a simple message. Similar to Debug.Log()
        /// </summary>
        /// <param name="text">Text of the message </param>
        /// <param name="messageType">Type of message, can be Normal, Warning or Failure</param>
        public static void MessageLog(string text, ServiceMessageLogType messageType = ServiceMessageLogType.Normal)
        {
            Console.WriteLine($"##teamcity[message text='{ReformatText(text)}' status='{messageType.ToString().ToUpper()}']");
        }

        /// <summary>
        /// For reporting of an error message. Similar to Debug.LogException()
        /// Fails the build if "Fail build if an error message is logged by build runner" box is checked on the Build Failure Conditions page of the build configuration
        /// </summary>
        /// <param name="exception">Exception causing this failure, used for extracting stacktrace</param>
        public static void MessageException(Exception exception)
        {
            Console.WriteLine($"##teamcity[message text='{ReformatText(exception.Message)}' errorDetails='{ReformatText(exception.StackTrace)}' status='ERROR']");
        }
        
        /// <summary>
        /// Marks long-running parts in a build script with single message
        /// Will be shown on the projects' dashboard for the corresponding build and on the Build Results page.
        /// Will be shown until another progress message occurs
        /// </summary>
        /// <param name="message">Progress message</param>
        public static void MessageProgressSingle(string message)
        {
            Console.WriteLine($"##teamcity[progressMessage '{ReformatText(message)}']");
        }
        
        /// <summary>
        /// Marks start of long-running parts in a build script
        /// Will be shown on the projects' dashboard for the corresponding build and on the Build Results page.
        /// </summary>
        /// <param name="message">Progress message</param>
        public static void BeginMessageProgress(string message)
        {
            Console.WriteLine($"##teamcity[progressStart '{ReformatText(message)}']");
        }
        
        /// <summary>
        /// Marks end of long-running parts in a build script
        /// </summary>
        /// <param name="message">
        /// Progress message
        /// Should be the same as from BeginMessageProgress
        /// </param>
        public static void EndMessageProgress(string message)
        {
            Console.WriteLine($"##teamcity[progressFinish '{ReformatText(message)}']");
        }
        
        /// <summary>
        /// Opens a block - group of messages in the build log
        /// </summary>
        /// <param name="name">Name of the group</param>
        /// <param name="description">Description text</param>
        public static void BeginMessageBlock(string name, string description = null)
        {
            Console.WriteLine($"##teamcity[blockOpened name='{ReformatText(name)}'{(description != null ? $" description='{ReformatText(description)}'" : "")}]");
        }
        
        /// <summary>
        /// Closes a block - group of messages in the build log
        /// </summary>
        /// <param name="name">Name of the group</param>
        public static void EndMessageBlock(string name)
        {
            Console.WriteLine($"##teamcity[blockClosed name='{ReformatText(name)}']");
        }
        
        /// <summary>
        /// Cancel a build, for example, if a build cannot proceed normally due to the environment.
        /// </summary>
        /// <param name="comment">A human-readable plain text describing the problem.</param>
        /// <param name="reAddToQueue">Should re-add the build to the queue after canceling it?</param>
        public static void MessageBuildStop(string comment, bool reAddToQueue = false)
        {
            Console.WriteLine($"##teamcity[buildStop comment='{ReformatText(comment)}' readdToQueue='{(reAddToQueue ? "true" : "false")}']");
        }

        /// <summary>
        /// Fail Build, for example, if fatal error occurs
        /// Build problems affect the build status text and appear on the Build Results page
        /// </summary>
        /// <param name="description">
        /// Description text
        /// The text is limited to 4000 symbols, and will be truncated if the limit is exceeded.
        /// </param>
        /// <param name="identity">
        /// Unique problem ID. Must be a valid Java ID up to 60 characters.
        /// If omitted, the identity is calculated based on the description text.
        /// </param>
        public static void MessageBuildProblem(string description, string identity = null)
        {
            Console.WriteLine($"##teamcity[buildProblem description='{ReformatText(description)}'{(identity != null ? $" identity='{identity}'" : "")}]");
        }

        /// <summary>
        /// Update build parameters.
        /// The changed build parameters will be available in the build steps following the current one.
        /// The parameters need to be defined in the Parameters section of the build configuration.
        /// </summary>
        /// <param name="name">Name of the parameter without prefix (system or env)</param>
        /// <param name="value">Value of the parameter</param>
        /// <param name="parameterType">Parameter type specifying the prefix for parameter</param>
        public static void MessageSetParameter(string name, string value, ParameterType parameterType = ParameterType.ConfigurationParameter)
        {
            var prefix = parameterType switch 
            {
                ParameterType.SystemProperty => "system.",
                ParameterType.EnvironmentVariable => "env.",
                _ => ""
            };
            Console.WriteLine($"##teamcity[setParameter name='{prefix}{ReformatText(name)}' value='{ReformatText(value)}']");
        }

        /// <summary>
        /// Changes format of text to match TeamCity format
        /// TeamCity uses a vertical bar '|' as an escape character instead of '\'".
        /// </summary>
        /// <param name="text">Text to format</param>
        /// <returns>Formatted text</returns>
        private static string ReformatText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text
                .Replace("|", "||")
                .Replace("\\", "|")
                .Replace("'", "|'")
                .Replace("[", "|[")
                .Replace("]", "|]");
        }
    }

    public enum ServiceMessageLogType
    {
        Normal,
        Warning,
        Failure
    }
    
    public enum ParameterType
    {
        ConfigurationParameter,
        EnvironmentVariable,
        SystemProperty
    }
}
