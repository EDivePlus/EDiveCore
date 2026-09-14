// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EDIVE.CredentialStore
{
    public sealed class MacKeychainCredentialStore : ICredentialStore
    {
        private const string SECURITY = "/System/Library/Frameworks/Security.framework/Security";
        private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        public string Name => "macOS Keychain (legacy)";

        // Looks up an entry that should not exist: "not found" proves the keychain answered.
        public CredentialResult CheckAvailability()
        {
            var probe = Encoding.UTF8.GetBytes("edive.availability");
            var status = FindItem(probe, probe, out var item);
            if (status == MacSecurityStatus.SUCCESS)
                CFRelease(item);

            return status is MacSecurityStatus.SUCCESS or MacSecurityStatus.ITEM_NOT_FOUND
                ? CredentialResult.Ok
                : MacSecurityStatus.ToResult(status);
        }

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            var serviceBytes = Encoding.UTF8.GetBytes(service);
            var accountBytes = Encoding.UTF8.GetBytes(account);
            var status = SecKeychainFindGenericPassword(IntPtr.Zero,
                (uint)serviceBytes.Length, serviceBytes,
                (uint)accountBytes.Length, accountBytes,
                out var length, out var data, IntPtr.Zero);

            if (status != MacSecurityStatus.SUCCESS)
                return MacSecurityStatus.ToResult(status);

            try
            {
                var bytes = new byte[length];
                if (length > 0)
                    Marshal.Copy(data, bytes, 0, bytes.Length);
                secret = Encoding.UTF8.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                return CredentialResult.Ok;
            }
            finally
            {
                if (data != IntPtr.Zero)
                    SecKeychainItemFreeContent(IntPtr.Zero, data);
            }
        }

        public CredentialResult Contains(string service, string account)
        {
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            var status = FindItem(Encoding.UTF8.GetBytes(service), Encoding.UTF8.GetBytes(account), out var item);
            if (status != MacSecurityStatus.SUCCESS)
                return MacSecurityStatus.ToResult(status);

            CFRelease(item);
            return CredentialResult.Ok;
        }

        public CredentialResult Set(string service, string account, string secret)
        {
            var result = CredentialUtils.ValidateNamesAndSecret(ref service, ref account, secret);
            if (!result)
                return result;

            var serviceBytes = Encoding.UTF8.GetBytes(service);
            var accountBytes = Encoding.UTF8.GetBytes(account);
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            try
            {
                var status = FindItem(serviceBytes, accountBytes, out var item);
                if (status == MacSecurityStatus.ITEM_NOT_FOUND)
                {
                    status = SecKeychainAddGenericPassword(IntPtr.Zero,
                        (uint)serviceBytes.Length, serviceBytes,
                        (uint)accountBytes.Length, accountBytes,
                        (uint)secretBytes.Length, secretBytes,
                        IntPtr.Zero);

                    if (status != MacSecurityStatus.DUPLICATE_ITEM)
                        return MacSecurityStatus.ToResult(status);

                    status = FindItem(serviceBytes, accountBytes, out item);
                }

                if (status != MacSecurityStatus.SUCCESS)
                    return MacSecurityStatus.ToResult(status);

                try
                {
                    return MacSecurityStatus.ToResult(SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)secretBytes.Length, secretBytes));
                }
                finally
                {
                    CFRelease(item);
                }
            }
            finally
            {
                Array.Clear(secretBytes, 0, secretBytes.Length);
            }
        }

        public CredentialResult Delete(string service, string account)
        {
            var result = CredentialUtils.ValidateNames(ref service, ref account);
            if (!result)
                return result;

            var status = FindItem(Encoding.UTF8.GetBytes(service), Encoding.UTF8.GetBytes(account), out var item);
            if (status != MacSecurityStatus.SUCCESS)
                return MacSecurityStatus.ToResult(status);

            try
            {
                return MacSecurityStatus.ToResult(SecKeychainItemDelete(item));
            }
            finally
            {
                CFRelease(item);
            }
        }

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            return CredentialResult.NotSupported($"No list. Use {nameof(MacSecItemCredentialStore)}.");
        }

        private static int FindItem(byte[] serviceBytes, byte[] accountBytes, out IntPtr item) =>
            SecKeychainFindGenericPasswordItem(IntPtr.Zero,
                (uint)serviceBytes.Length, serviceBytes,
                (uint)accountBytes.Length, accountBytes,
                IntPtr.Zero, IntPtr.Zero, out item);

        [DllImport(SECURITY)]
        private static extern int SecKeychainFindGenericPassword(IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            out uint passwordLength, out IntPtr passwordData,
            IntPtr itemRef);

        [DllImport(SECURITY, EntryPoint = "SecKeychainFindGenericPassword")]
        private static extern int SecKeychainFindGenericPasswordItem(IntPtr keychainOrArray,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            IntPtr passwordLength, IntPtr passwordData,
            out IntPtr itemRef);

        [DllImport(SECURITY)]
        private static extern int SecKeychainAddGenericPassword(IntPtr keychain,
            uint serviceNameLength, byte[] serviceName,
            uint accountNameLength, byte[] accountName,
            uint passwordLength, byte[] passwordData,
            IntPtr itemRef);

        [DllImport(SECURITY)]
        private static extern int SecKeychainItemModifyAttributesAndData(IntPtr itemRef, IntPtr attrList, uint length, byte[] data);

        [DllImport(SECURITY)]
        private static extern int SecKeychainItemDelete(IntPtr itemRef);

        [DllImport(SECURITY)]
        private static extern int SecKeychainItemFreeContent(IntPtr attrList, IntPtr data);

        [DllImport(CORE_FOUNDATION)]
        private static extern void CFRelease(IntPtr cf);
    }
}
