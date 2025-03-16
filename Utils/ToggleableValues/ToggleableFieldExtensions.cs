using System.Collections.Generic;
using System.Linq;

namespace EDIVE.Utils.ToggleableValues
{
    public static class ToggleableFieldExtensions
    {
        public static IEnumerable<T> ToValueList<T>(this IEnumerable<ToggleableField<T>> list, bool state = true)
        {
            return list.Where(value => value.State == state).Select(value => value.Value);
        }
        
        public static IEnumerable<ToggleableField<T>> ToToggleableList<T>(this IEnumerable<T> list)
        {
            return list.Select(value => new ToggleableField<T>(value));
        }
        
        public static T EvaluateField<T>(this ToggleableField<T> field, bool state = true) where T: class
        {
            return field.State == state ? field.Value : null;
        }
    }
}
