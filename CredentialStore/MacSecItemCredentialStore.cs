// Author: František Holubec
// Created: 14.09.2026

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EDIVE.CredentialStore
{
    public sealed class MacSecItemCredentialStore : ICredentialStore
    {
        private const string SECURITY = "/System/Library/Frameworks/Security.framework/Security";
        private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        private const string LIB_SYSTEM = "/usr/lib/libSystem.dylib";

        private const int RTLD_NOW = 2;
        private const uint CF_STRING_ENCODING_UTF8 = 0x08000100;
        private const int CF_NUMBER_SINT32_TYPE = 3;
        // Tags items as ours so List can filter them. Items written by MacKeychainCredentialStore
        // carry no creator, so List will not see them.
        private const int MARKER_CREATOR = ('E' << 24) | ('D' << 16) | ('I' << 8) | 'V';

        public string Name => "macOS Keychain";

        public static bool IsSupported => Sec.IsLoaded;

        public CredentialResult CheckAvailability() => Sec.IsLoaded ? CredentialResult.Ok : NotLoaded();

        public CredentialResult Get(string service, string account, out string secret)
        {
            secret = null;
            var result = Prepare(ref service, ref account);
            if (!result)
                return result;

            using var cf = new CFScope();
            var query = ItemQuery(cf, service, account);
            cf.Add(query, Sec.ReturnData, Sec.True);
            cf.Add(query, Sec.MatchLimit, Sec.MatchLimitOne);
            if (cf.Failed)
                return AllocFailed();

            var status = SecItemCopyMatching(query, out var data);
            cf.Track(data);
            if (status != MacSecurityStatus.SUCCESS)
                return MacSecurityStatus.ToResult(status);

            if (data == IntPtr.Zero || CFGetTypeID(data) != CFDataGetTypeID())
                return CredentialResult.Failed("Keychain bad data.");

            var length = (int)CFDataGetLength(data);
            var bytes = new byte[length];
            if (length > 0)
                Marshal.Copy(CFDataGetBytePtr(data), bytes, 0, length);
            secret = Encoding.UTF8.GetString(bytes);
            Array.Clear(bytes, 0, length);
            return CredentialResult.Ok;
        }

        public CredentialResult Contains(string service, string account)
        {
            var result = Prepare(ref service, ref account);
            if (!result)
                return result;

            using var cf = new CFScope();
            var query = ItemQuery(cf, service, account);
            cf.Add(query, Sec.ReturnAttributes, Sec.True);
            cf.Add(query, Sec.MatchLimit, Sec.MatchLimitOne);
            if (cf.Failed)
                return AllocFailed();

            var status = SecItemCopyMatching(query, out var attributes);
            cf.Track(attributes);
            return MacSecurityStatus.ToResult(status);
        }

        public CredentialResult Set(string service, string account, string secret)
        {
            if (!Sec.IsLoaded)
                return NotLoaded();

            var result = CredentialUtils.ValidateNamesAndSecret(ref service, ref account, secret);
            if (!result)
                return result;

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            try
            {
                using var cf = new CFScope();
                var data = cf.Data(secretBytes);
                var label = cf.String(service + ":" + account);
                var creator = cf.Number(MARKER_CREATOR);

                var update = cf.Dictionary();
                cf.Add(update, Sec.ValueData, data);
                cf.Add(update, Sec.AttrLabel, label);
                cf.Add(update, Sec.AttrCreator, creator);

                var query = ItemQuery(cf, service, account);

                var add = ItemQuery(cf, service, account);
                cf.Add(add, Sec.ValueData, data);
                cf.Add(add, Sec.AttrLabel, label);
                cf.Add(add, Sec.AttrCreator, creator);

                if (cf.Failed)
                    return AllocFailed();

                var status = SecItemUpdate(query, update);
                if (status != MacSecurityStatus.ITEM_NOT_FOUND)
                    return MacSecurityStatus.ToResult(status);

                status = SecItemAdd(add, IntPtr.Zero);
                if (status == MacSecurityStatus.DUPLICATE_ITEM)
                    status = SecItemUpdate(query, update);
                return MacSecurityStatus.ToResult(status);
            }
            finally
            {
                Array.Clear(secretBytes, 0, secretBytes.Length);
            }
        }

        public CredentialResult Delete(string service, string account)
        {
            var result = Prepare(ref service, ref account);
            if (!result)
                return result;

            using var cf = new CFScope();
            var query = ItemQuery(cf, service, account);
            if (cf.Failed)
                return AllocFailed();

            return MacSecurityStatus.ToResult(SecItemDelete(query));
        }

        public CredentialResult List(out IReadOnlyList<CredentialEntry> entries)
        {
            entries = Array.Empty<CredentialEntry>();
            if (!Sec.IsLoaded)
                return NotLoaded();

            using var cf = new CFScope();
            var query = cf.Dictionary();
            cf.Add(query, Sec.Class, Sec.ClassGenericPassword);
            cf.Add(query, Sec.AttrCreator, cf.Number(MARKER_CREATOR));
            cf.Add(query, Sec.ReturnAttributes, Sec.True);
            cf.Add(query, Sec.MatchLimit, Sec.MatchLimitAll);
            if (cf.Failed)
                return AllocFailed();

            var status = SecItemCopyMatching(query, out var items);
            cf.Track(items);
            if (status == MacSecurityStatus.ITEM_NOT_FOUND)
                return CredentialResult.Ok;
            if (status != MacSecurityStatus.SUCCESS)
                return MacSecurityStatus.ToResult(status);

            var found = new List<CredentialEntry>();
            if (items != IntPtr.Zero && CFGetTypeID(items) == CFArrayGetTypeID())
            {
                var count = CFArrayGetCount(items);
                for (long i = 0; i < count; i++)
                    AddEntry(found, CFArrayGetValueAtIndex(items, i));
            }
            else
            {
                AddEntry(found, items);
            }

            entries = found;
            return CredentialResult.Ok;
        }

        private static CredentialResult Prepare(ref string service, ref string account) =>
            Sec.IsLoaded ? CredentialUtils.ValidateNames(ref service, ref account) : NotLoaded();

        private static CredentialResult NotLoaded() => CredentialResult.Unavailable("Security framework not loaded.");

        private static CredentialResult AllocFailed() => CredentialResult.Failed("CoreFoundation alloc failed.");

        private static IntPtr ItemQuery(CFScope cf, string service, string account)
        {
            var query = cf.Dictionary();
            cf.Add(query, Sec.Class, Sec.ClassGenericPassword);
            cf.Add(query, Sec.AttrService, cf.String(service));
            cf.Add(query, Sec.AttrAccount, cf.String(account));
            return query;
        }

        private static void AddEntry(List<CredentialEntry> entries, IntPtr attributes)
        {
            if (attributes == IntPtr.Zero || CFGetTypeID(attributes) != CFDictionaryGetTypeID())
                return;

            var service = ToManagedString(CFDictionaryGetValue(attributes, Sec.AttrService));
            var account = ToManagedString(CFDictionaryGetValue(attributes, Sec.AttrAccount));
            if (service != null && account != null)
                entries.Add(new CredentialEntry(service, account));
        }

        private static string ToManagedString(IntPtr cfString)
        {
            if (cfString == IntPtr.Zero || CFGetTypeID(cfString) != CFStringGetTypeID())
                return null;

            var size = CFStringGetMaximumSizeForEncoding(CFStringGetLength(cfString), CF_STRING_ENCODING_UTF8) + 1;
            if (size <= 1)
                return size == 1 ? string.Empty : null;

            var buffer = new byte[size];
            if (!CFStringGetCString(cfString, buffer, size, CF_STRING_ENCODING_UTF8))
                return null;

            var end = Array.IndexOf(buffer, (byte)0);
            return Encoding.UTF8.GetString(buffer, 0, end < 0 ? buffer.Length : end);
        }

        private sealed class CFScope : IDisposable
        {
            private readonly List<IntPtr> _objects = new();

            public bool Failed { get; private set; }

            public IntPtr Track(IntPtr cf)
            {
                if (cf != IntPtr.Zero)
                    _objects.Add(cf);
                return cf;
            }

            public IntPtr Dictionary() => Create(CFDictionaryCreateMutable(IntPtr.Zero, 0, Sec.KeyCallBacks, Sec.ValueCallBacks));

            public IntPtr Data(byte[] bytes) => Create(CFDataCreate(IntPtr.Zero, bytes, bytes.Length));

            public IntPtr Number(int value) => Create(CFNumberCreate(IntPtr.Zero, CF_NUMBER_SINT32_TYPE, ref value));

            public IntPtr String(string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value);
                return Create(CFStringCreateWithBytes(IntPtr.Zero, bytes, bytes.Length, CF_STRING_ENCODING_UTF8, false));
            }

            public void Add(IntPtr dictionary, IntPtr key, IntPtr value)
            {
                if (dictionary == IntPtr.Zero || key == IntPtr.Zero || value == IntPtr.Zero)
                {
                    Failed = true;
                    return;
                }
                CFDictionaryAddValue(dictionary, key, value);
            }

            public void Dispose()
            {
                foreach (var cf in _objects)
                    CFRelease(cf);
                _objects.Clear();
            }

            private IntPtr Create(IntPtr cf)
            {
                if (cf == IntPtr.Zero)
                    Failed = true;
                return Track(cf);
            }
        }

        private static class Sec
        {
            public static readonly bool IsLoaded;
            public static readonly IntPtr Class;
            public static readonly IntPtr ClassGenericPassword;
            public static readonly IntPtr AttrService;
            public static readonly IntPtr AttrAccount;
            public static readonly IntPtr AttrLabel;
            public static readonly IntPtr AttrCreator;
            public static readonly IntPtr ValueData;
            public static readonly IntPtr ReturnData;
            public static readonly IntPtr ReturnAttributes;
            public static readonly IntPtr MatchLimit;
            public static readonly IntPtr MatchLimitOne;
            public static readonly IntPtr MatchLimitAll;
            public static readonly IntPtr True;
            public static readonly IntPtr KeyCallBacks;
            public static readonly IntPtr ValueCallBacks;

            // The kSec* constants are exported data symbols, not functions, so they are read
            // through dlsym rather than declared as DllImports.
            static Sec()
            {
                var security = dlopen(SECURITY, RTLD_NOW);
                var coreFoundation = dlopen(CORE_FOUNDATION, RTLD_NOW);
                if (security == IntPtr.Zero || coreFoundation == IntPtr.Zero)
                    return;

                Class = Constant(security, "kSecClass");
                ClassGenericPassword = Constant(security, "kSecClassGenericPassword");
                AttrService = Constant(security, "kSecAttrService");
                AttrAccount = Constant(security, "kSecAttrAccount");
                AttrLabel = Constant(security, "kSecAttrLabel");
                AttrCreator = Constant(security, "kSecAttrCreator");
                ValueData = Constant(security, "kSecValueData");
                ReturnData = Constant(security, "kSecReturnData");
                ReturnAttributes = Constant(security, "kSecReturnAttributes");
                MatchLimit = Constant(security, "kSecMatchLimit");
                MatchLimitOne = Constant(security, "kSecMatchLimitOne");
                MatchLimitAll = Constant(security, "kSecMatchLimitAll");
                True = Constant(coreFoundation, "kCFBooleanTrue");
                KeyCallBacks = dlsym(coreFoundation, "kCFTypeDictionaryKeyCallBacks");
                ValueCallBacks = dlsym(coreFoundation, "kCFTypeDictionaryValueCallBacks");

                IsLoaded = Array.TrueForAll(new[]
                {
                    Class, ClassGenericPassword, AttrService, AttrAccount, AttrLabel, AttrCreator, ValueData,
                    ReturnData, ReturnAttributes, MatchLimit, MatchLimitOne, MatchLimitAll, True, KeyCallBacks, ValueCallBacks
                }, pointer => pointer != IntPtr.Zero);
            }

            private static IntPtr Constant(IntPtr library, string name)
            {
                var symbol = dlsym(library, name);
                return symbol == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(symbol);
            }
        }

        [DllImport(LIB_SYSTEM)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport(LIB_SYSTEM)]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport(SECURITY)]
        private static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

        [DllImport(SECURITY)]
        private static extern int SecItemAdd(IntPtr attributes, IntPtr result);

        [DllImport(SECURITY)]
        private static extern int SecItemUpdate(IntPtr query, IntPtr attributesToUpdate);

        [DllImport(SECURITY)]
        private static extern int SecItemDelete(IntPtr query);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, long capacity, IntPtr keyCallBacks, IntPtr valueCallBacks);

        [DllImport(CORE_FOUNDATION)]
        private static extern void CFDictionaryAddValue(IntPtr dictionary, IntPtr key, IntPtr value);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFDictionaryGetValue(IntPtr dictionary, IntPtr key);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFStringCreateWithBytes(IntPtr allocator, byte[] bytes, long length, uint encoding,
            [MarshalAs(UnmanagedType.I1)] bool isExternalRepresentation);

        [DllImport(CORE_FOUNDATION)]
        private static extern long CFStringGetLength(IntPtr cfString);

        [DllImport(CORE_FOUNDATION)]
        private static extern long CFStringGetMaximumSizeForEncoding(long length, uint encoding);

        [DllImport(CORE_FOUNDATION)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool CFStringGetCString(IntPtr cfString, byte[] buffer, long bufferSize, uint encoding);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFDataCreate(IntPtr allocator, byte[] bytes, long length);

        [DllImport(CORE_FOUNDATION)]
        private static extern long CFDataGetLength(IntPtr data);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFDataGetBytePtr(IntPtr data);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFNumberCreate(IntPtr allocator, int type, ref int value);

        [DllImport(CORE_FOUNDATION)]
        private static extern long CFArrayGetCount(IntPtr array);

        [DllImport(CORE_FOUNDATION)]
        private static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, long index);

        [DllImport(CORE_FOUNDATION)]
        private static extern ulong CFGetTypeID(IntPtr cf);

        [DllImport(CORE_FOUNDATION)]
        private static extern ulong CFArrayGetTypeID();

        [DllImport(CORE_FOUNDATION)]
        private static extern ulong CFDictionaryGetTypeID();

        [DllImport(CORE_FOUNDATION)]
        private static extern ulong CFStringGetTypeID();

        [DllImport(CORE_FOUNDATION)]
        private static extern ulong CFDataGetTypeID();

        [DllImport(CORE_FOUNDATION)]
        private static extern void CFRelease(IntPtr cf);
    }
}
