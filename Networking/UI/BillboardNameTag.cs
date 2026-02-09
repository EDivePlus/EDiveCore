// Author: Radim Holub
// Created: 03.11.2025

using TMPro;
using UnityEngine;

namespace EDIVE.Networking.UI
{
    public class BillboardNameTag : MonoBehaviour
    {
        [SerializeField]
        private Transform _AttachRoot;
        
        [SerializeField]
        private Vector3 _AttachOffset = new(0f, 0.5f, 0f);
        
        [SerializeField]
        private TMP_Text _Label;
        
        [SerializeField]
        private bool _HideForOwner = true;
    
        private bool _isOwner;
    
        public void SetIsOwner(bool isOwner)
        {
            _isOwner = isOwner;
            if (_HideForOwner) 
                gameObject.SetActive(!_isOwner);
        }

        public void SetText(string t)
        {
            if (_Label) _Label.text = t;
        }

        private void LateUpdate()
        {
            // Update position in global space
            transform.position = _AttachRoot.position + _AttachOffset;
            
            var cam = Camera.main;
            if (!cam) return;

            var toCam = transform.position - cam.transform.position;
            if (toCam.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
        }
    }
}
