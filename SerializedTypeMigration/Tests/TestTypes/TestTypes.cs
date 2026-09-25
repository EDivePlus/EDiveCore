using UnityEngine.Scripting.APIUpdating;

// public fields so tests can use object initializers
// ReSharper disable InconsistentNaming

namespace EDIVE.SerializedTypeMigration.Tests
{
    [System.Serializable]
    public abstract class TestPayload { }

    // never moved
    [System.Serializable]
    public class StablePayload : TestPayload
    {
        public string Note;
    }

    [System.Serializable]
    [FormerlySerializedType(0, "RenamedPayloadLegacy")]
    public class RenamedPayload : TestPayload
    {
        public int Value;
        public string Label;
    }

    // out of source order on purpose, only Version decides
    [System.Serializable]
    [FormerlySerializedType(2, "ChainV1", "EDIVE.SerializedTypeMigration.Tests.Current")]
    [FormerlySerializedType(0, "ChainV0")]
    [FormerlySerializedType(1, ns: "EDIVE.SerializedTypeMigration.Tests.Legacy")]
    public class ChainedPayload : TestPayload
    {
        public int Value;
    }

    [System.Serializable]
    public class NestingOwner
    {
        [System.Serializable]
        [FormerlySerializedType(0, ns: "EDIVE.SerializedTypeMigration.Tests.Legacy")]
        public class NestedPayload : TestPayload
        {
            public string Deep;
        }
    }

    [System.Serializable]
    [FormerlySerializedType(0, ns: "EDIVE.SerializedTypeMigration.Tests.Legacy")]
    public class BoxedPayload<T> : TestPayload
    {
        public T Item;
    }

    [System.Serializable]
    [FormerlySerializedType(0, "ArgumentLegacy")]
    public class ArgumentPayload
    {
        public int Number;
    }

    // enums can be generic arguments
    [FormerlySerializedType(0, "LegacyMode")]
    public enum TestMode
    {
        A,
        B
    }

    // had no namespace
    [System.Serializable]
    [FormerlySerializedType(0, "GlobalLegacy", "")]
    public class FormerlyGlobalPayload : TestPayload { }

    [System.Serializable]
    [MovedFrom(false, "EDIVE.SerializedTypeMigration.Tests.Legacy", null, "MovedFromLegacy")]
    public class MovedFromPayload : TestPayload { }

    // both claim SharedLegacy, [FormerlySerializedType] wins
    [System.Serializable]
    [FormerlySerializedType(0, "SharedLegacy")]
    public class SharedWinner : TestPayload { }

    [System.Serializable]
    [MovedFrom(false, null, null, "SharedLegacy")]
    public class SharedLoser : TestPayload { }

    // never serialized. Unity really applies MovedFromHijacker's attribute to it, other tests would break
    [System.Serializable]
    public class HijackTarget : TestPayload { }

    [System.Serializable]
    [MovedFrom(false, null, null, "HijackTarget")]
    public class MovedFromHijacker : TestPayload { }

    // invalid on purpose, test the validation
    [System.Serializable]
    [FormerlySerializedType(0, "FirstGuess")]
    [FormerlySerializedType(0, "SecondGuess")]
    public class InvalidDuplicateVersion : TestPayload { }

    [System.Serializable]
    [FormerlySerializedType(0, "InvalidNoOp")]
    public class InvalidNoOp : TestPayload { }

    [System.Serializable]
    [FormerlySerializedType(0, "")]
    public class InvalidEmptyClass : TestPayload { }

    // same old name as RenamedPayload
    [System.Serializable]
    [FormerlySerializedType(0, "RenamedPayloadLegacy")]
    public class InvalidConflict : TestPayload { }

    // old name is a live type
    [System.Serializable]
    [FormerlySerializedType(0, "StablePayload")]
    public class InvalidHijacker : TestPayload { }

    // UnityEngine.Object, attribute does nothing
    [FormerlySerializedType(0, "OldAssetName")]
    public class FormerlyOnAsset : UnityEngine.ScriptableObject { }
}
