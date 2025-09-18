// Author: František Holubec
// Created: 18.09.2025

using TMPro;
using UnityEngine;

namespace EDIVE.Utils
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] 
        private TextMeshProUGUI _FPSText;
        
        [SerializeField] 
        private float _UpdateInterval = 0.5f;

        private float _time;
        private int _frames;

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            _frames++;
            
            if (_time >= _UpdateInterval)
            {
                float fps = (int)(_frames / _time);
                _time = 0;
                _frames = 0;

                _FPSText.text = $"{Mathf.RoundToInt(fps)}";
            }
        }
    }
}
