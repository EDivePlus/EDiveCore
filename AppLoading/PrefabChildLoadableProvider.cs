using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.AppLoading
{
    [DisallowMultipleComponent]
    public class PrefabChildLoadableProvider : MonoBehaviour
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<GameObject> _LoadableObjects = new();

        public IEnumerable<T> GetLoadableComponents<T>()
        {
            return _LoadableObjects?.Where(c => c != null).SelectMany(c => c.GetComponents<T>()) ?? Enumerable.Empty<T>();
        }

        public void Add(GameObject go)
        {
            if (_LoadableObjects.Contains(go))
                return;
            _LoadableObjects.Add(go);
        }

        public bool Contains(GameObject go)
        {
            return _LoadableObjects.Contains(go);
        }
    }
}
