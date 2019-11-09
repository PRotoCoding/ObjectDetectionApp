using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
    /// <summary>
    /// Main menu toggle button
    /// </summary>
    public class ToggleButton : ImageButton
    {
        public event EventHandler<bool> ToggleChanged;

        private const double scaleMin = 0.5;
        private const double scaleMax = 0.6;
        private const double fadeMax = 1f;
        private const double fadeMin = 0.5f;
        private const int animationTime = 100;

        private bool isToggled;
        public bool IsToggled
        {
            get => isToggled;
            set
            {
                if (value != isToggled)
                {
                    isToggled = value;
                    if (value)
                    {
                        this.ScaleTo(scaleMax, animationTime).ContinueWith((val) => {
                            this.ScaleTo(scaleMin, animationTime);
                            this.FadeTo(fadeMax, animationTime);
                        });
                    }
                    else
                    {
                        this.ScaleTo(scaleMax, animationTime).ContinueWith((val) => {
                            this.ScaleTo(scaleMin, animationTime);
                            this.FadeTo(fadeMin, animationTime);
                        });
                    }
                    ToggleChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// The ToggleButton requires the underlying image to be 80x80 sized to fit perfectly into the bottom toolbar
        /// </summary>
        /// <param name="imageFilePath"></param>
        public ToggleButton(string imageFilePath, bool startToggleState = true)
        {
            Source = imageFilePath;
            BackgroundColor = Color.Transparent;
            Scale = scaleMin;
            IsToggled = startToggleState;
            Opacity = IsToggled ? fadeMax : fadeMin;
            Pressed += (o, e) => { IsToggled = !IsToggled; };
        }
    }
}
