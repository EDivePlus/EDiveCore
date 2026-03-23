using System.Collections;
using DG.Tweening;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public class DoTweenConfigurator : ABuildAction, IPreprocessBuildCallback
    {
        public override string Tooltip => "Configures DOTween logging based on build type.";

        public IEnumerator OnPreprocess(BuildContext context)
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
