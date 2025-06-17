// Author: František Holubec
// Created: 17.06.2025

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

namespace EDIVE.Utils.FontSymbols
{
    public abstract class ABaseFontSymbolEditor<T> : OdinEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var property in Tree.RootProperty.Children)
            {
                if (!typeof(FontSymbolTMPTextUI).IsAssignableFrom(property.Info.TypeOfOwner))
                    property.State.Visible = false;
            }
        }
    }
}
#endif
