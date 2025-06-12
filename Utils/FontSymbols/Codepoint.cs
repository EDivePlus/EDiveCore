// Author: František Holubec
// Created: 11.06.2025

using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.FontSymbols
{
    [Serializable]
    public struct Codepoint : IEquatable<Codepoint>
    {
        [SerializeField]
        public string _Name;

        [SerializeField]
        private string _Code;

        public string Name => _Name;
        public string HexCode => _Code;

        [ShowInInspector]
        public uint Unicode => Convert.ToUInt32(HexCode, 16);
        public char Char => Convert.ToChar(Unicode);

        public Codepoint(string name, string code)
        {
            _Name = name;
            _Code = code;
        }

        public bool Equals(Codepoint other)
        {
            return _Code == other._Code;
        }

        public override bool Equals(object obj)
        {
            return obj is Codepoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_Code != null ? _Code.GetHashCode() : 0);
        }
    }
}
