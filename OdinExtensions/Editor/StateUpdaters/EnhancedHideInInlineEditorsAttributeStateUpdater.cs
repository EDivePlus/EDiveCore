using EDIVE.OdinExtensions.Editor.Drawers;
using EDIVE.OdinExtensions.Editor.StateUpdaters;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
[assembly: RegisterStateUpdater(typeof(HideInInlineEditorsAttributeStateUpdater))]
namespace EDIVE.OdinExtensions.Editor.StateUpdaters
{
    public sealed class HideInInlineEditorsAttributeStateUpdater : AttributeStateUpdater<HideInInlineEditorsAttribute>
    {
        public override void OnStateUpdate()
        {
            Property.State.Visible = EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth <= 0;
        }
    }
}