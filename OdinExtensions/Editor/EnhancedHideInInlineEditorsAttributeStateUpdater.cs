using EDIVE.OdinExtensions.Editor;
using EDIVE.OdinExtensions.Editor.Drawers;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

[assembly: RegisterStateUpdater(typeof(HideInInlineEditorsAttributeStateUpdater))]
namespace EDIVE.OdinExtensions.Editor
{
    public sealed class HideInInlineEditorsAttributeStateUpdater : AttributeStateUpdater<HideInInlineEditorsAttribute>
    {
        public override void OnStateUpdate()
        {
            Property.State.Visible = EnhancedInlineEditorAttributeDrawer.CurrentInlineEditorDrawDepth <= 0;
        }
    }
}