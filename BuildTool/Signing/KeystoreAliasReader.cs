// Author: František Holubec
// Created: 20.07.2026

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    public static class KeystoreAliasReader
    {
        public static bool TryReadAliases(string keystorePath, string storePassword, out List<string> aliases, out string error)
        {
            aliases = new List<string>();
            error = null;

            if (string.IsNullOrEmpty(keystorePath))
                return false;
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

            var keytool = FindKeytool();
            if (keytool == null)
            {
                error = "Could not locate 'keytool' Install a JDK or set JAVA_HOME.";
                return false;
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = keytool,
                    Arguments = $"-list -keystore \"{keystorePath}\"",
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

                process.StandardInput.WriteLine(storePassword);
                process.StandardInput.Close();

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                if (process.ExitCode != 0)
                {
                    var combined = stdout + "\n" + stderr;
                    error = combined.IndexOf("password was incorrect", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "Incorrect store password"
                        : FirstMeaningfulLine(combined) ?? "Could not read the keystore";
                    return false;
                }

                foreach (var line in stdout.Split('\n'))
                {
                    if (!line.Contains("Entry"))
                        continue;
                    var alias = line.Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(alias))
                        aliases.Add(alias);
                }
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public static bool VerifyKeyPassword(string keystorePath, string storePassword, string alias, string keyPassword, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath) || string.IsNullOrEmpty(storePassword)
                || string.IsNullOrEmpty(alias) || string.IsNullOrEmpty(keyPassword))
                return false;

            var keytool = FindKeytool();
            if (keytool == null)
            {
                error = "Could not locate 'keytool' Install a JDK or set JAVA_HOME.";
                return false;
            }

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = keytool,
                    Arguments = $"-certreq -alias \"{alias}\" -keystore \"{keystorePath}\"",
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

                process.StandardInput.WriteLine(storePassword);
                process.StandardInput.WriteLine(keyPassword);
                process.StandardInput.Close();

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                    return true;

                var combined = stdout + "\n" + stderr;
                var wrongKey = combined.IndexOf("Cannot recover key", StringComparison.OrdinalIgnoreCase) >= 0
                    || combined.IndexOf("UnrecoverableKey", StringComparison.OrdinalIgnoreCase) >= 0
                    || combined.IndexOf("Get Key failed", StringComparison.OrdinalIgnoreCase) >= 0;
                error = wrongKey ? "Incorrect key password" : FirstMeaningfulLine(combined) ?? "Could not verify key password";
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
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
                const string prefix = "keytool error: ";
                return line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? line[prefix.Length..] : line;
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
            return exe;
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
