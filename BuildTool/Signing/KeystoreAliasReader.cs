// Author: František Holubec
// Created: 20.07.2026

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    public static class KeystoreAliasReader
    {
        private const int TIMEOUT_MS = 15000;

        private static MethodInfo _unityAliasReader;

        public static bool IsAvailable => FindUnityAliasReader() != null || FindKeytool() != null;

        public static List<string> ReadAliasesOrEmpty(string keystorePath, string storePassword)
        {
            return TryReadAliases(keystorePath, storePassword, out var aliases, out _) ? aliases : new List<string>();
        }

        public static bool TryReadAliases(string keystorePath, string storePassword, out List<string> aliases, out string error)
        {
            aliases = new List<string>();
            error = null;

            if (string.IsNullOrEmpty(keystorePath))
            {
                error = "No keystore path is set";
                return false;
            }
            if (!File.Exists(keystorePath))
            {
                error = "Keystore file not found";
                return false;
            }
            if (string.IsNullOrEmpty(storePassword))
            {
                error = "Store password is required";
                return false;
            }

            // The same call the Android publishing settings use, keytool is only a fallback.
            if (TryReadAliasesWithUnity(keystorePath, storePassword, out aliases, out error))
                return true;

            if (!string.IsNullOrEmpty(error))
                return false;

            if (!TryRunKeytool($"-list -keystore \"{keystorePath}\"", out var output, out error, storePassword))
                return false;

            foreach (var line in output.Split('\n'))
            {
                if (line.IndexOf("Entry", StringComparison.Ordinal) < 0)
                    continue;
                var alias = line.Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(alias))
                    aliases.Add(alias);
            }

            if (aliases.Count > 0)
                return true;

            error = "Keystore contains no keys";
            return false;
        }

        public static bool VerifyKeyPassword(string keystorePath, string storePassword, string alias, string keyPassword, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath))
            {
                error = "Keystore file not found";
                return false;
            }
            if (string.IsNullOrEmpty(storePassword) || string.IsNullOrEmpty(keyPassword))
            {
                error = "Both passwords are required";
                return false;
            }
            if (string.IsNullOrEmpty(alias))
            {
                error = "No key alias is set";
                return false;
            }

            // PKCS12 holds one password for the whole file, keytool never asks for the key separately.
            if (IsPkcs12(keystorePath))
            {
                if (keyPassword == storePassword)
                    return true;

                error = "A PKCS12 keystore uses the store password for its keys";
                return false;
            }

            // -certreq is used only because it needs the key password, unlike -list.
            if (TryRunKeytool($"-certreq -alias \"{alias}\" -keystore \"{keystorePath}\"", out var output, out error, storePassword, keyPassword))
                return true;

            var wrongKey = output != null
                && (output.IndexOf("Cannot recover key", StringComparison.OrdinalIgnoreCase) >= 0
                    || output.IndexOf("UnrecoverableKey", StringComparison.OrdinalIgnoreCase) >= 0
                    || output.IndexOf("Get Key failed", StringComparison.OrdinalIgnoreCase) >= 0);

            if (wrongKey)
                error = "Incorrect key password";
            return false;
        }

        // JKS and JCEKS start with their own magic number, PKCS12 is DER and starts with a SEQUENCE.
        private static bool IsPkcs12(string keystorePath)
        {
            try
            {
                using var stream = File.OpenRead(keystorePath);
                return stream.ReadByte() == 0x30;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static bool TryReadAliasesWithUnity(string keystorePath, string storePassword, out List<string> aliases, out string error)
        {
            aliases = new List<string>();
            error = null;

            var reader = FindUnityAliasReader();
            if (reader == null)
                return false;

            try
            {
                if (reader.Invoke(null, new object[] {keystorePath, storePassword, false}) is not string[] result || result.Length == 0)
                    return false;

                aliases.AddRange(result);
                return true;
            }
            catch (TargetInvocationException e)
            {
                error = DescribeUnityFailure(e.InnerException?.Message);
                return false;
            }
        }

        private static string DescribeUnityFailure(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Could not read the keystore";

            if (message.IndexOf("password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Incorrect store password";

            // The failure dumps the whole command line and environment, only the stderr block is useful.
            const string marker = "stderr[";
            var start = message.IndexOf(marker, StringComparison.Ordinal);
            var end = start < 0 ? -1 : message.IndexOf(']', start);
            if (end > start)
                message = message.Substring(start + marker.Length, end - start - marker.Length);

            return FirstMeaningfulLine(message) ?? "Could not read the keystore";
        }

        private static MethodInfo FindUnityAliasReader()
        {
            if (_unityAliasReader != null)
                return _unityAliasReader;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEditor.Android.Utils");
                if (type == null)
                    continue;
                _unityAliasReader = type.GetMethod("GetAvailableSigningKeyAlias", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    null, new[] {typeof(string), typeof(string), typeof(bool)}, null);
                break;
            }
            return _unityAliasReader;
        }

        private static bool TryRunKeytool(string arguments, out string output, out string error, params string[] input)
        {
            output = null;
            error = null;

            var keytool = FindKeytool();
            if (keytool == null)
            {
                error = "Could not locate 'keytool'. Install a JDK or set JAVA_HOME.";
                return false;
            }

            try
            {
                // The output is parsed below, so the JVM locale is pinned to English.
                var info = new ProcessStartInfo
                {
                    FileName = keytool,
                    Arguments = $"-J-Duser.language=en -J-Duser.country=US {arguments}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(info);
                if (process == null)
                {
                    error = "Could not start 'keytool'";
                    return false;
                }

                // Both pipes are drained while keytool runs, a full one would block it.
                var buffer = new StringBuilder();
                process.OutputDataReceived += (_, e) => AppendLine(buffer, e.Data);
                process.ErrorDataReceived += (_, e) => AppendLine(buffer, e.Data);
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                foreach (var line in input)
                    process.StandardInput.WriteLine(line);
                process.StandardInput.Close();

                if (!process.WaitForExit(TIMEOUT_MS))
                {
                    process.Kill();
                    error = "'keytool' did not respond";
                    return false;
                }
                process.WaitForExit();

                lock (buffer)
                {
                    output = buffer.ToString();
                }

                if (process.ExitCode == 0)
                    return true;

                error = output.IndexOf("password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "Incorrect store password"
                    : FirstMeaningfulLine(output) ?? "Could not read the keystore";
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static void AppendLine(StringBuilder buffer, string data)
        {
            if (data == null)
                return;
            lock (buffer)
            {
                buffer.AppendLine(data);
            }
        }

        private static string FirstMeaningfulLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                var isPrompt = line.IndexOf("Enter ", StringComparison.OrdinalIgnoreCase) >= 0
                    && line.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0;
                if (string.IsNullOrEmpty(line) || isPrompt)
                    continue;

                foreach (var prefix in new[] {"keytool error: ", "Error: "})
                {
                    if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        line = line[prefix.Length..];
                }

                // Drops the java.io.IOException style prefix the JDK puts in front of the message.
                const string exception = "Exception: ";
                var thrown = line.IndexOf(exception, StringComparison.Ordinal);
                return thrown < 0 ? line : line[(thrown + exception.Length)..];
            }
            return null;
        }

        private static string FindKeytool()
        {
            var exe = Application.platform == RuntimePlatform.WindowsEditor ? "keytool.exe" : "keytool";

            var jdk = GetAndroidJdkRoot();
            if (!string.IsNullOrEmpty(jdk))
            {
                var jdkTool = Path.Combine(jdk, "bin", exe);
                if (File.Exists(jdkTool))
                    return jdkTool;
            }

            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome))
            {
                var homeTool = Path.Combine(javaHome, "bin", exe);
                if (File.Exists(homeTool))
                    return homeTool;
            }
            return FindOnPath(exe);
        }

        private static string FindOnPath(string exe)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var directory in path.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                try
                {
                    var candidate = Path.Combine(directory.Trim(), exe);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException) { }
            }
            return null;
        }

        private static string GetAndroidJdkRoot()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                if (type == null)
                    continue;
                var property = type.GetProperty("jdkRootPath", BindingFlags.Public | BindingFlags.Static);
                return property?.GetValue(null) as string;
            }
            return null;
        }
    }
}
