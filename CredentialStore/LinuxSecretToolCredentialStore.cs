// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EDIVE.CredentialStore
{
    public sealed class LinuxSecretToolCredentialStore : ICredentialStore
    {
        public const string MARKER_ATTRIBUTE = "application";
        public const string MARKER_VALUE = "EDIVE.CredentialStore";

        private const string SECRET_TOOL = "secret-tool";

        private static readonly TimeSpan STREAM_TIMEOUT = TimeSpan.FromSeconds(5);

        public LinuxSecretToolCredentialStore(TimeSpan? timeout = null)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(60);
        }

        public TimeSpan Timeout { get; }

        public string Name => "Linux Secret Service";

        // Deliberately without --unlock, so a probe never raises an unlock prompt.
        public CredentialResult CheckAvailability()
        {
            var result = Run(null, out var run, "search", "--all", MARKER_ATTRIBUTE, MARKER_VALUE);
            if (!result)
                return result;

            return run.ExitCode != 0 && HasToolError(run.Error) ? FromFailure("search", run) : CredentialResult.Ok;
        }

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            result = Run(null, out var run, "lookup", "service", service, "account", account);
            if (!result)
                return result;

            if (run.ExitCode != 0)
                return run.Error.Length == 0 ? CredentialResult.NotFound : FromFailure("lookup", run);

            secret = Encoding.UTF8.GetString(run.Output);
            Array.Clear(run.Output, 0, run.Output.Length);
            return CredentialResult.Ok;
        }

        public CredentialResult Contains(string service, string account) => Get(service, account, out _);

        public CredentialResult Set(string service, string account, string secret)
        {
            var result = CredentialUtils.ValidateNamesAndSecret(ref service, ref account, secret);
            if (!result)
                return result;

            result = Run(secret, out var run, "store", $"--label={service}:{account}",
                "service", service, "account", account, MARKER_ATTRIBUTE, MARKER_VALUE);
            if (!result)
                return result;

            return run.ExitCode == 0 ? CredentialResult.Ok : FromFailure("store", run);
        }

        public CredentialResult Delete(string service, string account)
        {
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            result = Contains(service, account);
            if (!result)
                return result;

            result = Run(null, out var run, "clear", "service", service, "account", account);
            if (!result)
                return result;

            return run.ExitCode == 0 ? CredentialResult.Ok : FromFailure("clear", run);
        }

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            var result = Run(null, out var run, "search", "--all", "--unlock", MARKER_ATTRIBUTE, MARKER_VALUE);
            if (!result)
                return result;

            var text = Encoding.UTF8.GetString(run.Output) + "\n" + run.Error;
            Array.Clear(run.Output, 0, run.Output.Length);

            if (run.ExitCode != 0 && HasToolError(run.Error))
                return FromFailure("search", run);

            entries = ParseSearch(text);
            return CredentialResult.Ok;
        }

        private CredentialResult Run(string input, out ProcessOutput output, params string[] args)
        {
            output = default;
            var startInfo = new ProcessStartInfo
            {
                FileName = SECRET_TOOL,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            foreach (var arg in args)
                startInfo.ArgumentList.Add(arg);

            // Force the C locale so the output stays in the English form ParseSearch expects.
            startInfo.EnvironmentVariables.Remove("LANGUAGE");
            startInfo.EnvironmentVariables["LC_ALL"] = "C.UTF-8";

            Process process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception)
            {
                return CredentialResult.Unavailable($"{SECRET_TOOL} missing. Install libsecret-tools.");
            }
            catch (InvalidOperationException e)
            {
                return CredentialResult.Failed($"{SECRET_TOOL} start failed. {e.Message}");
            }

            if (process == null)
                return CredentialResult.Failed($"{SECRET_TOOL} start failed.");

            using (process)
            {
                var stdout = new MemoryStream();
                var outputTask = process.StandardOutput.BaseStream.CopyToAsync(stdout);
                var errorTask = process.StandardError.ReadToEndAsync();

                try
                {
                    if (input != null)
                    {
                        var bytes = Encoding.UTF8.GetBytes(input);
                        process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                        Array.Clear(bytes, 0, bytes.Length);
                    }
                    process.StandardInput.Close();
                }
                catch (IOException) { }

                if (!process.WaitForExit((int)Timeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (SystemException) { }
                    return CredentialResult.Unavailable($"{SECRET_TOOL} timeout {Timeout.TotalSeconds:0} s. Keyring locked?");
                }

                try
                {
                    Task.WaitAll(new Task[] {outputTask, errorTask}, STREAM_TIMEOUT);
                }
                catch (AggregateException) { }

                if (outputTask.Status != TaskStatus.RanToCompletion)
                    return CredentialResult.Failed($"{SECRET_TOOL} output lost.");

                var error = errorTask.Status == TaskStatus.RanToCompletion ? errorTask.Result.Trim() : string.Empty;
                output = new ProcessOutput(process.ExitCode, stdout.ToArray(), error);
                Array.Clear(stdout.GetBuffer(), 0, (int)stdout.Length);
                return CredentialResult.Ok;
            }
        }

        private static List<CredentialEntry> ParseSearch(string text)
        {
            var entries = new List<CredentialEntry>();
            string service = null;
            string account = null;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (TryAttribute(line, "service", out var value))
                {
                    if (service != null)
                        Flush();
                    service = value;
                }
                else if (TryAttribute(line, "account", out value))
                {
                    if (account != null)
                        Flush();
                    account = value;
                }

                if (service != null && account != null)
                    Flush();
            }
            return entries;

            void Flush()
            {
                if (service != null && account != null)
                    entries.Add(new CredentialEntry(service, account));
                service = null;
                account = null;
            }
        }

        private static bool TryAttribute(string line, string name, out string value)
        {
            var prefix = "attribute." + name + " = ";
            value = line.StartsWith(prefix, StringComparison.Ordinal) ? line.Substring(prefix.Length) : null;
            return value != null;
        }

        private static bool HasToolError(string error)
        {
            foreach (var line in error.Split('\n'))
            {
                if (line.StartsWith(SECRET_TOOL + ":", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static CredentialResult FromFailure(string operation, ProcessOutput run)
        {
            var message = $"{SECRET_TOOL} {operation} failed ({run.ExitCode}). {run.Error}";
            if (Has(run.Error, "dismissed") || Has(run.Error, "locked"))
                return CredentialResult.Denied(message);
            if (Has(run.Error, "D-Bus") || Has(run.Error, "org.freedesktop.secrets"))
                return CredentialResult.Unavailable(message);
            return CredentialResult.Failed(message);
        }

        private static bool Has(string text, string value) => text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        private readonly struct ProcessOutput
        {
            public readonly int ExitCode;
            public readonly byte[] Output;
            public readonly string Error;

            public ProcessOutput(int exitCode, byte[] output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }
        }
    }
}
