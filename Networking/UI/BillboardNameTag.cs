// Author: Radim Holub
// Created: 03.11.2025

using TMPro;
using UnityEngine;

public class BillboardNameTag : MonoBehaviour
{

    [Header("Binding")]
    [SerializeField]
    private Transform _Head;
    [SerializeField]
    private Vector3 _Offset = new(0f, 0.25f, 0f);

    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI _Label;

    [Header("Behaviour")]
    [SerializeField]
    private bool _HideForOwner = true;
    private bool _isOwner;

    public void BindHead(Transform head) => _Head = head;

    public void SetIsOwner(bool isOwner)
    {
        _isOwner = isOwner;
        if (_HideForOwner) gameObject.SetActive(!isOwner);
    }

    public void SetText(string t)
    {
        if (_Label) _Label.text = t;
    }

    private void LateUpdate()
    {
        if (_Head == null) return;
        var cam = Camera.main;
        if (!cam) return;

        transform.localPosition = _Offset;

        var toCam = transform.position - cam.transform.position;
        if (toCam.sqrMagnitude > 1e-6f)
            transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
    }
}
