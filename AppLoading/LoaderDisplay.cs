using System.Collections.Generic;
using System.Linq;
using EDIVE.AppLoading.LoadItems;
using EDIVE.Core;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.AppLoading
{
    public class LoaderDisplay : MonoBehaviour
    {
        [SerializeField]
        private AppLoaderController _LoaderController;

        [SerializeField]
        private Slider _ProgressSlider;
        
        [SerializeField]
        protected TextMeshProUGUI _LoadPercentageText;

        [PropertySpace]
        [SerializeField]
        private GameObject _LoadReportRoot;

        [SerializeField]
        private TMP_Text _LoadReportTextArea;

        [SerializeField]
        private Toggle _LoadReportToggle;

        private List<ALoadItemDefinition> _loadItems;

        private bool EnableLoadReportByDefault =>
#if UNITY_EDITOR
            true;
#else
            false;
#endif

        private void Awake()
        {
            if (_LoaderController == null)
                _LoaderController = AppCore.Services.Get<AppLoaderController>();
            
            _ProgressSlider.minValue = 0;
            _ProgressSlider.maxValue = 1;
            _ProgressSlider.wholeNumbers = false;

            if (_LoadReportToggle)
            {
                _LoadReportToggle.onValueChanged.AddListener(OnToggleValueChanged);
                _LoadReportToggle.isOn = EnableLoadReportByDefault;
                OnToggleValueChanged(_LoadReportToggle.isOn);
            }
            else
            {
                OnToggleValueChanged(EnableLoadReportByDefault);
            }

            _loadItems = _LoaderController.Setup.GetValidLoadItemsSorted().ToList();
            _LoaderController.LoadFinalizedSignal.AddListener(OnLoadFinalized);
        }
        
        private void OnDestroy()
        {
            if (_LoadReportToggle)
                _LoadReportToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
        
        private void OnLoadFinalized()
        {
            Destroy(gameObject);
        }

        private void OnToggleValueChanged(bool state)
        {
            if (_LoadReportRoot)
                _LoadReportRoot.SetActive(state);
        }

        private void Update()
        {
            var progress = Mathf.Clamp01(_LoaderController.GetLoadingProgress());

            _ProgressSlider.value = progress;
            if (_LoadPercentageText)
                _LoadPercentageText.text = $"{Mathf.RoundToInt(progress * 100)}%";

            if (_LoadReportTextArea != null && (_LoadReportToggle == null || _LoadReportToggle.isOn))
            {
                var details = _loadItems.Select(l => l.GetLoadingDetail());
                _LoadReportTextArea.text = string.Join("\n", details);
            }
        }
    }
}
