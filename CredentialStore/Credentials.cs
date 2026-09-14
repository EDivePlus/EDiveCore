// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EDIVE.CredentialStore
{
    [InitializeOnLoad]
    public static class Credentials
    {
        private const string LOG_PREFIX = "[CredentialStore]";

        private static readonly RuntimePlatform PLATFORM;
        private static ICredentialStore _store;

        static Credentials()
        {
            PLATFORM = Application.platform;
            _store = CreateDefault();
        }

        public static bool LogErrors { get; set; } = true;

        public static ICredentialStore Store
        {
            get => _store;
            set => _store = value ?? CreateDefault();
        }

        // Not routed through Report: an unreachable store is an expected state the caller displays,
        // not an error worth logging on every check.
        public static CredentialResult CheckAvailability()
        {
            try
            {
                return _store.CheckAvailability();
            }
            catch (Exception e)
            {
                return Unexpected(e);
            }
        }

        public static CredentialResult Get(string service, string account, out string secret)
        {
            CredentialResult result;
            try
            {
                result = _store.Get(service, account, out secret);
            }
            catch (Exception e)
            {
                secret = null;
                result = Unexpected(e);
            }
            return Report(result, "Get", service, account);
        }

        public static bool TryGet(string service, string account, out string secret) => Get(service, account, out secret);

        public static CredentialResult Contains(string service, string account)
        {
            CredentialResult result;
            try
            {
                result = _store.Contains(service, account);
            }
            catch (Exception e)
            {
                result = Unexpected(e);
            }
            return Report(result, "Contains", service, account);
        }

        public static CredentialResult Set(string service, string account, string secret)
        {
            CredentialResult result;
            try
            {
                result = _store.Set(service, account, secret);
            }
            catch (Exception e)
            {
                result = Unexpected(e);
            }
            return Report(result, "Set", service, account);
        }

        public static CredentialResult Delete(string service, string account)
        {
            CredentialResult result;
            try
            {
                result = _store.Delete(service, account);
            }
            catch (Exception e)
            {
                result = Unexpected(e);
            }
            return Report(result, "Delete", service, account);
        }

        public static CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            CredentialResult result;
            try
            {
                result = _store.List(out entries);
            }
            catch (Exception e)
            {
                entries = Array.Empty<CredentialEntry>();
                result = Unexpected(e);
            }
            entries ??= Array.Empty<CredentialEntry>();
            return Report(result, "List");
        }

        public static ICredentialStore CreateDefault() => PLATFORM switch
        {
            RuntimePlatform.WindowsEditor => new WindowsCredentialStore(),
            RuntimePlatform.OSXEditor => new MacKeychainCredentialStore(),
            RuntimePlatform.LinuxEditor => new LinuxSecretToolCredentialStore(),
            _ => new UnsupportedCredentialStore(PLATFORM.ToString())
        };

        private static CredentialResult Report(CredentialResult result, string operation, string service = null, string account = null)
        {
            if (!LogErrors || result || result.Status is CredentialStatus.NotFound or CredentialStatus.NotSupported)
                return result;

            var target = service == null && account == null ? string.Empty : $" {service}:{account}";
            Debug.LogError($"{LOG_PREFIX} {operation}{target} failed. {result}");
            return result;
        }

        private static CredentialResult Unexpected(Exception e) => CredentialResult.Failed($"{e.GetType().Name}: {e.Message}");
    }

    public readonly struct CredentialResult
    {
        public CredentialStatus Status { get; }
        public string Error { get; }

        public bool IsSuccess => Status == CredentialStatus.Success;

        public CredentialResult(CredentialStatus status, string error = null)
        {
            Status = status;
            Error = error;
        }

        public static CredentialResult Ok => new(CredentialStatus.Success);
        public static CredentialResult NotFound => new(CredentialStatus.NotFound);

        public static CredentialResult NotSupported(string error) => new(CredentialStatus.NotSupported, error);
        public static CredentialResult Invalid(string error) => new(CredentialStatus.InvalidArgument, error);
        public static CredentialResult Denied(string error) => new(CredentialStatus.AccessDenied, error);
        public static CredentialResult Unavailable(string error) => new(CredentialStatus.Unavailable, error);
        public static CredentialResult Failed(string error) => new(CredentialStatus.Failed, error);

        public static implicit operator bool(CredentialResult result) => result.IsSuccess;

        public override string ToString() => string.IsNullOrEmpty(Error) ? Status.ToString() : $"{Status}: {Error}";
    }

    public enum CredentialStatus
    {
        Success,
        NotFound,
        NotSupported,
        InvalidArgument,
        AccessDenied,
        Unavailable,
        Failed
    }

    public readonly struct CredentialEntry : IEquatable<CredentialEntry>
    {
        public string Service { get; }
        public string Account { get; }

        public CredentialEntry(string service, string account)
        {
            Service = service;
            Account = account;
        }

        public bool Equals(CredentialEntry other) => Service == other.Service && Account == other.Account;

        public override bool Equals(object obj) => obj is CredentialEntry other && Equals(other);

        public override int GetHashCode() => ((Service?.GetHashCode() ?? 0) * 397) ^ (Account?.GetHashCode() ?? 0);

        public override string ToString() => $"{Service}:{Account}";
    }
}
