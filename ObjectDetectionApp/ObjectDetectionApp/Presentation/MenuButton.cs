using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
    public class MenuButton : ImageButton
    {
        public event EventHandler<EventArgs> ClickedAnimationFinished;

        private const double enabledScale = 0.75;
        private const double disabledScale = 0.3;
        private const int animationTime = 100;

        public new bool IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                if (base.IsEnabled != value)
                {
                    base.IsEnabled = value;
                    if (value)
                        this.FadeTo(enabledScale, animationTime);
                    else
                        this.FadeTo(disabledScale, animationTime);
                }
            }
        }

        public MenuButton(string imageFilePath)
        {
            Source = imageFilePath;
            BackgroundColor = Color.Transparent;
            WidthRequest = 400;
            HeightRequest = 120;
            VerticalOptions = LayoutOptions.Start;
            HorizontalOptions = LayoutOptions.Center;
            Opacity = IsEnabled ? enabledScale : disabledScale;
            Aspect = Aspect.AspectFit;

            Pressed += async (o, e) =>
            {
                // "Discard" feature (C# 7.0) to get rid of compiler warning
                // Underscore is an unassigned variable that indicates that the return value of the Task is not needed
                _ = (o as MenuButton).ScaleTo(1.1, 50);
                await (o as MenuButton).FadeTo(1, 50);
                _ = (o as MenuButton).ScaleTo(1, 50);
                await (o as MenuButton).FadeTo(enabledScale, 50);
                ClickedAnimationFinished?.Invoke(this, e);
            };
        }
    }
}
