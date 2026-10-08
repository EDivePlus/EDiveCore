// Author: František Holubec
// Created: 06.10.2026

namespace EDIVE.Utils.DrivenValues
{
    public interface IValueDriver
    {
#if UNITY_EDITOR
        void PopulateDrivenValues(DrivenValuesCollection values);
#endif
    }
}
