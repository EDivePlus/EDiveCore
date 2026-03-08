using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Tweening
{
    public interface ITweenReferencesHolder
    {
        public IDictionary<TweenObjectReference, Object> GetReferencesDictionary();
    }
}
