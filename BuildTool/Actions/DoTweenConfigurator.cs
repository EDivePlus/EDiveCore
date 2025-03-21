using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public class DoTweenConfigurator : ABuildAction
    {
        public override UniTask OnPreBuildAfterDefines(BuildContext buildContext)
        {
#if DEBUG
            DOTween.logBehaviour = LogBehaviour.Verbose;
#else
            DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
#endif
            return UniTask.CompletedTask;
        }
    }
}
