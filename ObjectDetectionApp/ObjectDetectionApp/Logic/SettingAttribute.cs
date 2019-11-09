using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectDetectionApp.Logic
{
    public enum AccessDegree { Developer = 1, Consumer = 0, Noone = 2 };

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class SettingAttribute : Attribute
    {
        public AccessDegree Degree { get; set; }

        public string Name { get; set; }

        public SettingAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SettingMinMax : SettingAttribute
    {
        public int Min { get; set; }
        public int Max { get; set; }
        // Symbol for unit of value
        public string Unit { get; set; }
        public SettingMinMax() : base() { }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class SettingString : SettingAttribute
    {
        public int MaxChars { get; set; }
        public char[] AllowedChars { get; set; }
        public SettingString() : base() { }
    }

    public class SettingEnum : SettingAttribute
    {
        public SettingEnum() : base() { }
    }
}
