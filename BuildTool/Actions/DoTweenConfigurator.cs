using System.Collections;
using DG.Tweening;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public class DoTweenConfigurator : ABuildAction
    {
        public override IEnumerator OnPreBuildAfterDefines(BuildContext buildContext)
        {
#if DEBUG
            DOTween.logBehaviour = LogBehaviour.Verbose;
#else
            DOTween.logBehaviour = LogBehaviour.ErrorsOnly;
#endif
            yield break;
        }
    }
}
