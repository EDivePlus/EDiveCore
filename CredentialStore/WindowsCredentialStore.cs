// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EDIVE.CredentialStore
{
    public sealed class WindowsCredentialStore : ICredentialStore
    {
        public const int MAX_SECRET_BYTES = 5 * 512;
        public const string TARGET_PREFIX = "EDIVE:";

        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;

        private const int ERROR_ACCESS_DENIED = 5;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INVALID_FLAGS = 1004;
        private const int ERROR_NOT_FOUND = 1168;
        private const int ERROR_NO_SUCH_LOGON_SESSION = 1312;

        public string Name => "Windows Credential Manager";

        // Enumerating our own prefix exercises the vault without touching any entry.
        public CredentialResult CheckAvailability()
        {
            if (!CredEnumerate(TARGET_PREFIX + "*", 0, out _, out var list))
            {
                var error = Marshal.GetLastWin32Error();
                return error == ERROR_NOT_FOUND ? CredentialResult.Ok : FromError(error);
            }

            CredFree(list);
            return CredentialResult.Ok;
        }

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            if (!CredRead(Target(service, account), CRED_TYPE_GENERIC, 0, out var handle))
                return FromError(Marshal.GetLastWin32Error());

            try
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(handle);
                secret = credential.CredentialBlobSize == 0 || credential.CredentialBlob == IntPtr.Zero
                    ? string.Empty
                    : Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
                return CredentialResult.Ok;
            }
            finally
            {
                CredFree(handle);
            }
        }

        public CredentialResult Contains(string service, string account)
        {
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            if (!CredRead(Target(service, account), CRED_TYPE_GENERIC, 0, out var handle))
                return FromError(Marshal.GetLastWin32Error());

            CredFree(handle);
            return CredentialResult.Ok;
        }

        public CredentialResult Set(string service, string account, string secret)
        {
            var result = CredentialUtils.ValidateNamesAndSecret(ref service, ref account, secret);
            if (!result)
                return result;

            var bytes = Encoding.Unicode.GetBytes(secret);
            if (bytes.Length > MAX_SECRET_BYTES)
            {
                var length = bytes.Length;
                Array.Clear(bytes, 0, length);
                return CredentialResult.Invalid($"Secret too long. {length} of max {MAX_SECRET_BYTES} bytes.");
            }

            var blob = Marshal.AllocHGlobal(Math.Max(bytes.Length, 1));
            try
            {
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                var credential = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = Target(service, account),
                    CredentialBlob = blob,
                    CredentialBlobSize = bytes.Length,
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    UserName = account
                };

                return CredWrite(ref credential, 0) ? CredentialResult.Ok : FromError(Marshal.GetLastWin32Error());
            }
            finally
            {
                // Zero the managed copy, then write those zeros over the unmanaged blob before freeing it.
                Array.Clear(bytes, 0, bytes.Length);
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                Marshal.FreeHGlobal(blob);
            }
        }

        public CredentialResult Delete(string service, string account)
        {
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            return CredDelete(Target(service, account), CRED_TYPE_GENERIC, 0) ? CredentialResult.Ok : FromError(Marshal.GetLastWin32Error());
        }

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            if (!CredEnumerate(TARGET_PREFIX + "*", 0, out var count, out var list))
            {
                var error = Marshal.GetLastWin32Error();
                return error == ERROR_NOT_FOUND ? CredentialResult.Ok : FromError(error);
            }

            try
            {
                var found = new List<CredentialEntry>(count);
                for (var i = 0; i < count; i++)
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(Marshal.ReadIntPtr(list, i * IntPtr.Size));
                    if (TryParseTarget(credential.TargetName, out var entry))
                        found.Add(entry);
                }
                entries = found;
                return CredentialResult.Ok;
            }
            finally
            {
                CredFree(list);
            }
        }

        private static string Target(string service, string account) => TARGET_PREFIX + service + ":" + account;

        private static bool TryParseTarget(string target, out CredentialEntry entry)
        {
            entry = default;
            if (target == null || !target.StartsWith(TARGET_PREFIX, StringComparison.Ordinal))
                return false;

            // Split on the first separator only, which is why a service may not contain one.
            var separator = target.IndexOf(':', TARGET_PREFIX.Length);
            if (separator <= TARGET_PREFIX.Length || separator == target.Length - 1)
                return false;

            entry = new CredentialEntry(target.Substring(TARGET_PREFIX.Length, separator - TARGET_PREFIX.Length), target.Substring(separator + 1));
            return true;
        }

        private static CredentialResult FromError(int error) => error switch
        {
            ERROR_NOT_FOUND => CredentialResult.NotFound,
            ERROR_ACCESS_DENIED => CredentialResult.Denied(Message(error)),
            ERROR_NO_SUCH_LOGON_SESSION => CredentialResult.Unavailable("No logon session."),
            ERROR_INVALID_PARAMETER or ERROR_INVALID_FLAGS => CredentialResult.Invalid(Message(error)),
            _ => CredentialResult.Failed(Message(error))
        };

        private static string Message(int error) => $"{new Win32Exception(error).Message} ({error})";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite([In] ref CREDENTIAL credential, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, int type, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredEnumerateW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);
    }
}
