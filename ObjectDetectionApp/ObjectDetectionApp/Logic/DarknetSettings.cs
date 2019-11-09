using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace ObjectDetectionApp.Logic
{
    public enum ImageFormat { compressed = 1, uncompressed = 2 }

    public class Resolution : IEquatable<Resolution>
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public override string ToString() => Width.ToString() + " x " + Height.ToString();
        public bool Equals(Resolution other) => (other.Height == Height && other.Width == Width);
        //public override bool Equals(object other) => ((other as Resolution).Height == Height && (other as Resolution).Width == Width);
    }

    public class DarknetSettings : IEnumerable<PropertyInfo>, IEquatable<DarknetSettings>
    {
        [SettingMinMax(Degree = AccessDegree.Consumer, Name = "Prediction Threshold", Min = 0, Max = 100, Unit = "%")]
        public int Threshold { get; set; }

        //[SettingMinMax(Degree = AccessDegree.Consumer, Name = "How much do you weigh?", Min = 0, Max = 100, Unit = "g")]
        //public int Weight{ get; set; }

        [SettingEnum(Degree = AccessDegree.Noone, Name = "Image Format")]
        public ImageFormat ImageFormat { get; set; }

        [SettingString(Degree = AccessDegree.Developer, Name = "Host Port", MaxChars = 5, AllowedChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'})]
        public int Port { get; set; }

        [SettingString(Degree = AccessDegree.Developer, Name = "Host IP", MaxChars = 15, AllowedChars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.' })]
        public string IpAddress { get; set; }

        [SettingAttribute(Degree = AccessDegree.Consumer, Name = "Resolution")]
        public Resolution Resolution { get; set; }
        
        public IEnumerator<PropertyInfo> GetEnumerator()
        {
            PropertyInfo[] properties = typeof(DarknetSettings).GetProperties();
            foreach(var property in properties)
            {
                if(property.GetCustomAttribute(typeof(SettingAttribute)) != null)
                {
                    yield return property;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return GetEnumerator();
        }

        public DarknetSettings(DarknetSettings refSettings)
        {
            foreach (var property in this)
                property.SetValue(this, property.GetValue(refSettings));
        }

        public DarknetSettings()
        {
            Threshold = 25;
            ImageFormat = ImageFormat.compressed;
            Port = 27015;
            IpAddress = "141.47.69.88";
        }

        public bool Equals(DarknetSettings other)
        {
            foreach (var property in this)
            {
                if (!Equals(property.GetValue(this), property.GetValue(other)))
                    return false;
            }
            return true;
        }
    }
}
