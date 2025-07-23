using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Time.TimeSpanUtils
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [InlineProperty]
    public class UTimeSpanRange
    {
        [JsonProperty("Start")]
        [SerializeField]
        [LabelWidth(22)]
        [IconLabelText(FontAwesomeEditorIconType.PlaySolid, HideText = true)]
        [OnValueChanged("OnStartChanged")]
        private UTimeSpan _Start;

        [JsonProperty("End")]
        [SerializeField]
        [LabelWidth(22)]
        [IconLabelText(FontAwesomeEditorIconType.StopSolid, HideText = true)]
        [OnValueChanged("OnEndChanged")]
        private UTimeSpan _End;

        public TimeSpan Start
        {
            get => _Start;
            set => _Start = value;
        }

        public TimeSpan End
        {
            get => _End;
            set => _End = value;
        }

        public TimeSpan Duration => End - Start;

        public bool IsInRange(TimeSpan time)
        {
            return time >= Start && time <= End;
        }

        [JsonConstructor]
        public UTimeSpanRange() { }

        public UTimeSpanRange(TimeSpan start)
        {
            _Start = start;
            _End = start;
        }

        public UTimeSpanRange(TimeSpan start, TimeSpan end)
        {
            _Start = start;
            _End = end;
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnStartChanged()
        {
            if (_Start.CompareTo(_End) > 0) _End.Value = _Start.Value;
        }

        [UsedImplicitly]
        private void OnEndChanged()
        {
           if (_End.CompareTo(_Start) < 0) _Start.Value = _End.Value;
        }
#endif
    }
}
