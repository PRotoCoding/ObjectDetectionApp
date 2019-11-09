using ObjectDetectionApp.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
    /// <summary>
    /// Interface that allows the SettingsPage to access the property of each Property User Control
    /// </summary>
    public interface IDarknetSettingUI
    {
        PropertyInfo PropertyInfo { get; set; }
    }

    /// <summary>
    /// Info label that prints the DarknetSettings property name for each section
    /// </summary>
    public class SettingsInfoLabel : Label
    {
        public SettingsInfoLabel(string text)
            : base()
        {
            Text = text;
            Margin = new Thickness(10);
        }
    }

    /// <summary>
    /// Simple line to make a cut between two sections
    /// </summary>
    public class SectionLine : BoxView
    {
        public SectionLine()
            : base()
        {
            BackgroundColor = Color.Silver;
            HeightRequest = 1;
            HorizontalOptions = LayoutOptions.Fill;
        }
    }

    /// <summary>
    /// Setting picker for DarknetSettings that are enumerated
    /// </summary>
    public class SettingPicker : Picker, IDarknetSettingUI
    {
        public Array Values { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public SettingPicker(PropertyInfo property)
        {
            if (!property.PropertyType.IsEnum)
                throw new SystemException("Can't setup EnumPicker for non-enum type.");
            PropertyInfo = property;
            Values = Enum.GetValues(PropertyInfo.PropertyType);
            ItemsSource = Enum.GetNames(PropertyInfo.PropertyType);
        }
    }

    /// <summary>
    /// Slider for DarknetSettings that use numeric values
    /// </summary>
    public class SettingSlider : Slider, IDarknetSettingUI
    {
        public PropertyInfo PropertyInfo { get; set; }
        public SettingMinMax SettingAttribute;
        public Label Label { get; set; }

        public SettingSlider(PropertyInfo property, Label label)
        {
            PropertyInfo = property;
            SettingAttribute = PropertyInfo.GetCustomAttribute(typeof(SettingMinMax)) as SettingMinMax;
            MaximumTrackColor = Color.Blue;
            MinimumTrackColor = Color.Blue;
            ThumbColor = Color.Black;
            Minimum = SettingAttribute.Min;
            Maximum = SettingAttribute.Max;
            Label = label;
        }
    }

    /// <summary>
    /// Text field for DarknetSettings that require the user to type in some values
    /// </summary>
    public class SettingEntry : Entry, IDarknetSettingUI
    {
        public PropertyInfo PropertyInfo { get; set; }
        public SettingString SettingAttribute;

        public SettingEntry(PropertyInfo property)
        {
            PropertyInfo = property;
            SettingAttribute = PropertyInfo.GetCustomAttribute(typeof(SettingString)) as SettingString;
            HorizontalTextAlignment = TextAlignment.Start;
        }
    }


    public class SettingsPage : ContentPage
	{
        readonly DarknetService darknetService;
        DarknetSettings newSettings;

#if DEBUG
        readonly AccessDegree UserDegree = AccessDegree.Developer;
#else
        readonly AccessDegree UserDegree = AccessDegree.Consumer;
#endif

        public SettingsPage(DarknetService darknetService)
        {
            this.darknetService = darknetService;
            // First copy the current darknet settings
            // The new settings will be modified by the user in this page, and only written after the "Apply Changes" Button is pressed
            newSettings = new DarknetSettings(darknetService.Settings);
            NavigationPage.SetHasNavigationBar(this, false);

            var stackLayout = new StackLayout() { Children = { new Label() { Text = "Darknet Settings", FontSize = 30, HorizontalTextAlignment = TextAlignment.Center }, new SectionLine(), } };

            // Setup user interface for each property
            foreach (var property in newSettings)
            {
                SettingAttribute attr = property.GetCustomAttribute(typeof(SettingAttribute)) as SettingAttribute;
                bool Enabled = attr.Degree <= UserDegree;
                
                stackLayout.Children.Add(new SettingsInfoLabel(attr.Name));

                // Pattern matching, C# 7.0
                // Choose User Control depending on the property type and attribute
                switch (attr as object)
                {
                    case SettingMinMax darkSlider:
                        var label = new Label() { HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, Text = property.GetValue(newSettings).ToString() + darkSlider.Unit };
                        // The following cast will result in InvalidCastException:  int -> object -> double
                        // So the object needs to be casted to int before:          int -> object -> int -> double
                        var slider = new SettingSlider(property, label) { IsEnabled = Enabled };
                        slider.Value = (int) property.GetValue(newSettings);
                        slider.ValueChanged += (o, e) => {
                            var sl = o as SettingSlider;
                            // Same as above
                            sl.PropertyInfo.SetValue(newSettings, (int)(e.NewValue));
                            sl.Label.Text = sl.PropertyInfo.GetValue(newSettings).ToString() + sl.SettingAttribute.Unit;
                        };
                        stackLayout.Children.Add(label);
                        stackLayout.Children.Add(slider);
                        break;

                    case SettingEnum darkEnum:
                        SettingPicker picker = new SettingPicker(property)
                        {
                            Title = attr.Name,
                            IsEnabled = Enabled
                        };

                        picker.SelectedIndex = Array.IndexOf(picker.Values, property.GetValue(newSettings));
                        picker.SelectedIndexChanged += (o, e) =>
                        {
                            var pick = o as SettingPicker;
                            pick.PropertyInfo.SetValue(newSettings, pick.Values.GetValue(pick.SelectedIndex));
                        };
                        stackLayout.Children.Add(picker);
                        break;

                    case SettingString darkEntry:
                        var entry = new SettingEntry(property) { Text = property.GetValue(newSettings).ToString(), IsEnabled = Enabled };
                        entry.TextChanged += (o, e) =>
                        {
                            var entr = o as SettingEntry;
                            if (e.NewTextValue.Length > entr.SettingAttribute.MaxChars)
                            {
                                entr.TextColor = Color.Red;
                                return;
                            }
                            foreach (char c in e.NewTextValue)
                            {
                                bool containsAtLeastOneChar = false;
                                foreach (char c2 in entr.SettingAttribute.AllowedChars)
                                {
                                    if (c2 == c) containsAtLeastOneChar = true;
                                }
                                if (containsAtLeastOneChar == false)
                                {
                                    entr.TextColor = Color.Red;
                                    return;
                                }
                            }
                            entr.TextColor = Color.Black;
                            switch (Type.GetTypeCode(entr.PropertyInfo.PropertyType))
                            {
                                case TypeCode.Int32:
                                    entr.PropertyInfo.SetValue(newSettings, int.Parse(e.NewTextValue));
                                    break;

                                case TypeCode.String:
                                    entr.PropertyInfo.SetValue(newSettings, e.NewTextValue);
                                    break;
                            }
                        };
                        stackLayout.Children.Add(entry);
                        break;

                    default:
                        switch(property.Name)
                        {

                            case "Resolution":
                                int index = 0;
                                var resList = darknetService.CameraPreview.SupportedResolutions.ToList();
                                foreach (var obj in resList)
                                {
                                    if (obj.Height == newSettings.Resolution.Height && obj.Width == newSettings.Resolution.Width)
                                    {
                                        index = resList.IndexOf(obj);
                                        break;
                                    }
                                }

                                Picker resolutionsPicker = new Picker() { Title = "Resolutions", ItemsSource = resList, SelectedIndex = index, IsEnabled = Enabled };
                                resolutionsPicker.SelectedIndexChanged += (o, e) =>
                                {
                                    newSettings.Resolution = darknetService.CameraPreview.SupportedResolutions.ToList()[resolutionsPicker.SelectedIndex];
                                };
                                stackLayout.Children.Add(resolutionsPicker);
                                break;
                        }
                        break;
                }
                stackLayout.Children.Add(new SectionLine());
            }

            Button applySettings = new Button() { Text = "Apply Settings" };
            applySettings.Pressed += (o, e) =>
            {
                darknetService.Settings = newSettings;
                Navigation.PopAsync();
            };
            stackLayout.Children.Add(applySettings);
            Content = new ScrollView()
            {
                Content = stackLayout
            };



            /// OUTDATED CODE

            //Label ThresholdLabel = new Label() { HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center, Text = newSettings.Threshold.ToString() + " %" };
            //Slider thresholdSlider = new Slider(0, 100, newSettings.Threshold) { MaximumTrackColor = Color.Blue, MinimumTrackColor = Color.Blue };
            //thresholdSlider.ValueChanged += (o, e) =>
            //{
            //    ThresholdLabel.Text = e.NewValue.ToString() + " %";
            //    newSettings.Threshold = (int)e.NewValue;
            //};

            //Picker formatPicker = new Picker() { Title = "Image Format", ItemsSource = new List<string>() { "compressed", "uncompressed" }, SelectedIndex = newSettings.ImageFormat == ImageFormat.compressed ? 0 : 1, IsEnabled = false };
            //formatPicker.SelectedIndexChanged += (o, e) =>
            //{
            //    newSettings.ImageFormat = formatPicker.SelectedIndex == 0 ? ImageFormat.compressed : ImageFormat.uncompressed;
            //};

            //int index = 0;
            //var resList = darknetService.CameraPreview.SupportedResolutions.ToList();
            //foreach (var obj in resList)
            //{
            //    if (obj.Height == newSettings.Resolution.Height && obj.Width == newSettings.Resolution.Width)
            //    {
            //        index = resList.IndexOf(obj);
            //        break;
            //    }
            //}
            //Picker resolutionsPicker = new Picker() { Title = "Resolutions", ItemsSource = resList, SelectedIndex = index };
            //resolutionsPicker.SelectedIndexChanged += (o, e) =>
            //{
            //    newSettings.Resolution = darknetService.CameraPreview.SupportedResolutions.ToList()[resolutionsPicker.SelectedIndex];
            //};

            //Entry ipEntry = new Entry() { Text = newSettings.IpAddress.ToString(), HorizontalTextAlignment = TextAlignment.Start };
            //ipEntry.TextChanged += (o, e) =>
            //{
            //    newSettings.IpAddress = e.NewTextValue;
            //};

            //Entry portEntry = new Entry() { Text = newSettings.Port.ToString(), HorizontalTextAlignment = TextAlignment.Start };
            //portEntry.TextChanged += (o, e) =>
            //{
            //    int port = 0;
            //    if (int.TryParse(e.NewTextValue, out port))
            //    {
            //        newSettings.Port = port;
            //        portEntry.TextColor = Color.Black;
            //    }
            //    else
            //    {
            //        portEntry.TextColor = Color.Red;
            //    }

            //};

            //Button applySettings = new Button() { Text = "Apply Settings" };
            //applySettings.Pressed += (o, e) =>
            //{
            //    darknetService.Settings = newSettings;
            //    Navigation.PopAsync();
            //};
            //stackLayout.Children.Add(applySettings);
            //Content = new ScrollView()
            //{
            //    Content = new StackLayout
            //    {
            //        Children = {
            //        new Label() {Text = "Darknet Settings", FontSize = 30, HorizontalTextAlignment = TextAlignment.Center }, new SectionLine(),
            //        new SettingsInfoLabel("Prediction Threshold"), ThresholdLabel, thresholdSlider, new SectionLine(),
            //        new SettingsInfoLabel("Image Format"), formatPicker, new SectionLine(),
            //        new SettingsInfoLabel("Image Resolution"), resolutionsPicker, new SectionLine(),
            //        new SettingsInfoLabel("Host IP Address"), ipEntry, new SectionLine(),
            //        new SettingsInfoLabel("Host Port"), portEntry, new SectionLine(),
            //        applySettings
            //    }
            //    }
            //};
        }
	}
}