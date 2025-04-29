using UnityEngine;

[CreateAssetMenu(fileName = "TransformObject", menuName = "EDIVE/Transform/Transform Object")]
public class TransformObject : ScriptableObject
{
    protected GameObject _transformData;
    
    public virtual void SetValue(GameObject value)
    {
        _transformData = value;
    }
    
    public GameObject GetValue()
    {
        return _transformData;
    }
}
