using System;
using System.Linq;
using BepInEx.Configuration;

namespace NorvinskMaps.Config
{
    public class AcceptableEnumList<T> : AcceptableValueBase where T : Enum
    {
        private readonly T[] _values;

        public AcceptableEnumList(params T[] values) : base(typeof(T))
        {
            _values = values ?? Array.Empty<T>();
        }

        public override object Clamp(object value)
        {
            if (value is T t && _values.Contains(t))
            {
                return value;
            }
            return _values.Length > 0 ? (object)_values[0] : value;
        }

        public override bool IsValid(object value)
        {
            return value is T t && _values.Contains(t);
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values: " + string.Join(", ", _values.Select(v => v.ToString()));
        }
    }
}