// Author: František Holubec
// Created: 15.09.2026

using System;
using System.Diagnostics;

namespace EDIVE.CredentialStore
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.All)]
    public class CredentialFieldAttribute : Attribute
    {
        public string Service;
        public string Account;
        public string ValidationMethod;

        public CredentialFieldAttribute(string service, string account, string validationMethod = null)
        {
            Service = service;
            Account = account;
            ValidationMethod = validationMethod;
        }
    }
}
