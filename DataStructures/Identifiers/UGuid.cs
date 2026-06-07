// Author: František Holubec
// Created: 07.06.2026

using System;
using EDIVE.DataStructures.Identifiers;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEngine;

[assembly: RegisterValidationRule(typeof(UGuidValidator))]
namespace EDIVE.DataStructures.Identifiers
{
    [Serializable]
    public struct UGuid : IEquatable<UGuid>
    { 
        [SerializeField]
        private uint _Part1;
        
        [SerializeField]
        private uint _Part2;
        
        [SerializeField]
        private uint _Part3;

        [SerializeField]
        private uint _Part4;
        
        public string HexString => ToString();
        public static UGuid Empty => new(0, 0, 0, 0);
        public readonly bool IsEmpty => this == Empty;

        public UGuid(uint part1, uint part2, uint part3, uint part4)
        {
            _Part1 = part1;
            _Part2 = part2;
            _Part3 = part3;
            _Part4 = part4;
        }

        public UGuid(Guid guid)
        {
            var bytes = guid.ToByteArray();
            _Part1 = BitConverter.ToUInt32(bytes, 0);
            _Part2 = BitConverter.ToUInt32(bytes, 4);
            _Part3 = BitConverter.ToUInt32(bytes, 8);
            _Part4 = BitConverter.ToUInt32(bytes, 12);
        }

        public readonly override string ToString()
        {
            return $"{_Part1:X8}-{_Part2:X8}-{_Part3:X8}-{_Part4:X8}";
        }

        public static UGuid Parse(string hexString)
        {
            return TryParse(hexString, out var result) ? result : Empty;
        }

        public static bool TryParse(string hexString, out UGuid result)
        {
            result = Empty;
            if (string.IsNullOrEmpty(hexString))
                return false;

            hexString = hexString.Replace("-", "").ToLower();
            if (hexString.Length != 32)
                return false;

            try
            {
                result = new UGuid(
                    Convert.ToUInt32(hexString[..8], 16),
                    Convert.ToUInt32(hexString[8..16], 16),
                    Convert.ToUInt32(hexString[16..24], 16),
                    Convert.ToUInt32(hexString[24..32], 16)
                );
                return true;
            }
            catch (FormatException)
            {
                result = Empty;
                return false;
            }
        }

        public readonly Guid ToGuid()
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(_Part1).CopyTo(bytes, 0);
            BitConverter.GetBytes(_Part2).CopyTo(bytes, 4);
            BitConverter.GetBytes(_Part3).CopyTo(bytes, 8);
            BitConverter.GetBytes(_Part4).CopyTo(bytes, 12);
            return new Guid(bytes);
        }

        public static UGuid New() => Guid.NewGuid();

        public static implicit operator UGuid(Guid guid) => new(guid);
        public static implicit operator Guid(UGuid unityId) => unityId.ToGuid();

        public static bool operator ==(UGuid left, UGuid right) => left.Equals(right);
        public static bool operator !=(UGuid left, UGuid right) => !left.Equals(right);
        
        public bool Equals(UGuid other)
        {
            return _Part1 == other._Part1 && _Part2 == other._Part2 && _Part3 == other._Part3 && _Part4 == other._Part4;
        }

        public override bool Equals(object obj)
        {
            return obj is UGuid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_Part1, _Part2, _Part3, _Part4);
        }
    }
    
#if UNITY_EDITOR
    [Serializable]
    public class UGuidValidator : ValueValidator<UGuid>
    {
        [EnumToggleButtons]
        [SerializeField]
        private ValidatorSeverity _Severity = ValidatorSeverity.Warning;

        protected override void Validate(ValidationResult result)
        {
            if (Value.IsEmpty)
            {
                result
                    .Add(_Severity, "UnityID is empty. Are you sure this is correct?")
                    .WithFix(() => Value = UGuid.New());
            }
        }
    }
#endif
}
