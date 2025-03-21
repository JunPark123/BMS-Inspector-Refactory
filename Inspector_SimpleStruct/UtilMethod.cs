using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inspector_SimpleArchitecture
{
    #region 유틸리티 확장 메서드

    /// <summary>
    /// 파라미터 값 가져오기 확장 메서드
    /// </summary>
    public static class DictionaryExtensions
    {
        public static T GetValue<T>(this Dictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (!dict.ContainsKey(key))
                return defaultValue;

            var value = dict[key];

            if (value is JsonElement jsonElement)
            {
                // JsonElement를 적절한 타입으로 변환
                if (typeof(T) == typeof(int))
                    return (T)(object)jsonElement.GetInt32();
                else if (typeof(T) == typeof(double))
                    return (T)(object)jsonElement.GetDouble();
                else if (typeof(T) == typeof(string))
                    return (T)(object)jsonElement.GetString();
                else if (typeof(T) == typeof(bool))
                    return (T)(object)jsonElement.GetBoolean();
                // 필요에 따라 다른 타입 추가
            }

            // 일반적인 변환 시도
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    #endregion
}
