using ObjectDetectionApp.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
    
    public class MainMenu_NOT_USED : ContentPage
    {
        DarknetService darknetService;

        public MainMenu_NOT_USED()
        {
            const double fontSize = 30;
            Font font = Font.Default;
            Label Welcome = new Label() { Text = "HS Pforzheim\nObject Detection", FontSize = fontSize, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            Button StartObjectDetection = new Button() { Text = "Start", FontSize = fontSize, BackgroundColor = Color.AliceBlue, Font = font, IsEnabled = false };
            Button Settings = new Button() { Text = "Change Settings", FontSize = fontSize, Font = font };
            Button Train = new Button() { Text = "Train Network", IsEnabled = false, FontSize = fontSize, Font = font };
            Button ShowClassList = new Button() { Text = "Customize Class Filter", FontSize = fontSize, Font = font, IsEnabled = false };
            Button Credits = new Button() { Text = "Credits", FontSize = fontSize, Font = font };
            Label Connected = new Label() { Text = "Disconnected", FontSize = fontSize, TextColor = Color.Red, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
            Button TryConnect = new Button() { Text = "Try to Connect", FontSize = fontSize, Font = font };
            Content = new StackLayout
            {
                Children = {
                    Welcome, StartObjectDetection, Settings, Train, ShowClassList, Credits, Connected, TryConnect
                }
            };

            darknetService = DarknetService.Create();
            darknetService.Settings = new DarknetSettings() { Resolution = darknetService.CameraPreview.SupportedResolutions.OrderByDescending((res) => res.Width * res.Height).First() };

            TryConnect.Pressed += (o, e) =>
            {
                Connected.TextColor = Color.Orange;
                if(darknetService.ConnectionState == DarknetService.ConnectionStatus.Connected)
                {
                    Connected.Text = "Disconnecting...";
                    darknetService.Disconnect();
                }
                else
                {
                    Connected.Text = "Connecting...";
                    darknetService.Connect();
                }
                
            };

            darknetService.Connected += (o, e) =>
            {
                darknetService.SendSettings();
                Device.BeginInvokeOnMainThread(() =>
                {
                    TryConnect.Text = "Disconnect";
                    StartObjectDetection.IsEnabled = true;
                    Connected.TextColor = Color.Green;
                    Connected.Text = "Connected";
                });
            };

            darknetService.Disconnected += (o, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    TryConnect.Text = "Try To Connect";
                    StartObjectDetection.IsEnabled = false;
                    Connected.TextColor = Color.Red;
                    Connected.Text = "Disconnected";
                });
            };

            darknetService.ClassCollectionAquired += (o, e) =>
            {
                if (e != null)
                {
                    Device.BeginInvokeOnMainThread(() => ShowClassList.IsEnabled = true);
                    if(darknetService.FilteredClasses == null)
                    {
                        darknetService.FilteredClasses = new List<string>();
                        foreach(var obj in e.classes)
                        {
                            darknetService.FilteredClasses.Add(obj);
                        }
                    }
                }
            };

            Settings.Pressed += (o, e) =>
            {
                Navigation.PushAsync(new SettingsPage(darknetService));
            };

            StartObjectDetection.Pressed += (o, e) =>
            {
                Navigation.PushAsync(new PreviewPage(darknetService));
            };

            ShowClassList.Pressed += (o, e) =>
            {
                Navigation.PushAsync(new ClassFilterPage(darknetService));
            };
        }
    }
}