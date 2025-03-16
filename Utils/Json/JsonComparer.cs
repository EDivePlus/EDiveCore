using System;
using Newtonsoft.Json.Linq;

namespace EDIVE.Utils.Json
{
    // Thanks to ChatGPT for this magnificent shit
    public static class JsonComparer
    {
        public static bool DeepEquals(JToken token1, JToken token2)
        {
            if (token1.Type != token2.Type)
            {
                return false;
            }

            switch (token1.Type)
            {
                case JTokenType.Float:
                    return Approximately(token1.Value<double>(), token2.Value<double>());
                case JTokenType.Integer:
                    return token1.Value<long>() == token2.Value<long>();

                case JTokenType.Array:
                    return DeepEqualsArray(token1, token2);

                case JTokenType.Object:
                    return DeepEqualsObject(token1, token2);

                case JTokenType.None:
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Comment:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                default:
                    return JToken.DeepEquals(token1, token2);
            }
        }

        private static bool DeepEqualsArray(JToken token1, JToken token2)
        {
            var array1 = token1 as JArray;
            var array2 = token2 as JArray;

            if (array1.Count != array2.Count)
            {
                return false;
            }

            for (var i = 0; i < array1.Count; i++)
            {
                if (!DeepEquals(array1[i], array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DeepEqualsObject(JToken token1, JToken token2)
        {
            var obj1 = token1 as JObject;
            var obj2 = token2 as JObject;

            if (obj1.Count != obj2.Count)
            {
                return false;
            }

            foreach (var property in obj1)
            {
                var obj2Token = obj2[property.Key];

                if (obj2Token == null || !DeepEquals(property.Value, obj2Token))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Approximately(double a, double b)
        {
            return Math.Abs(a - b) < 0.0001;
        } 
    }
}
