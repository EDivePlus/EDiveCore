using EDIVE.Core.Services;
using UnityEngine;


namespace EDIVE.StagePlay {
    public class StagePlayManager : MonoBehaviour, IService
    {
        /*
        [FormerlySerializedAs("stagePlayConfig")]
        [SerializeField]
        public StagePlayConfig _StagePlayConfig;

        [SerializeField]
        private StagePlayDefinition stagePlayDefinition;

        [SerializeField]
        public StagePlaySceneDefinition currentScene;

        [SerializeField]
        private AScriptSegment currentSegment;

        [SerializeField]
        private ScriptController _Controller;

        [SerializeField]
        private IActivation _ToggleFloatingScriptTabletAction;

        private void Awake()
        {
            Debug.Log("Loading Stage Play Manager");
            currentSegment = currentScene.ScriptSegments.First(seg => !seg.Delivered);
            _Controller.OnDeliverLine += HandleLineDelivered;
            if (_ToggleFloatingScriptTabletAction)
                _ToggleFloatingScriptTabletAction.action.performed += FloatingScriptTabletAction;
            AppCore.Services.Register(this);
        }

        private void FloatingScriptTabletAction(InputAction.CallbackContext ctx)
        {
            gameObject.SetActive(!gameObject.activeSelf);
            _Controller.gameObject.SetActive(gameObject.activeSelf);
        }

        private void OnDestroy()
        {
            AppCore.Services.Unregister(this);
            _ToggleFloatingScriptTabletAction.action.performed -= FloatingScriptTabletAction;
        }

        private void HandleLineDelivered(AScriptSegment segment)
        {
            // update the Delivered value of the segment in the current scene
            currentScene.ScriptSegments.Find(seg => seg == segment).Delivered = true;
            // set next as current
            currentSegment = currentScene.ScriptSegments.First(seg => !seg.Delivered);
            _Controller.RefreshScroller();
        }
        */
    }
}
