using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Lottie.Forms;
using System.Reflection;
using System.IO;
using Plugin.SimpleAudioPlayer;
using System.Timers;
using System.Collections.Concurrent;
using ObjectDetectionApp.Logic;

namespace ObjectDetectionApp.Presentation
{
    public class CustomAnimation : AnimationView
    {
        public CustomAnimation(string animationPath)
        {
            Animation = animationPath;
            Loop = true;
            AutoPlay = true;
            HeightRequest = 80;
            WidthRequest = 80;
        }
    }

    /// <summary>
    /// Main menu
    /// Problem: Image Button resizes when ImageSource changes https://github.com/xamarin/Xamarin.Forms/issues/4510
    /// </summary>
    public partial class Menu : ContentPage
    {
        DarknetService darknetService;
        ParticleManager particleManager;

        // UI Elements
        ToggleButton starButton;
        ToggleButton volumeButton;
        Slider volumeSlider;
        Grid toolbarGrid;
        Image cyberDogImage;

        MenuButton startDetectionButton;
        MenuButton customizeFilterButton;
        MenuButton trainNetworkButton;
        MenuButton optionsButton;
        MenuButton disconnectButton;
        MenuButton connectButton;

        ISimpleAudioPlayer player;

        public Menu()
        {
            InitializeComponent();

            darknetService = DarknetService.Create();
            darknetService.Settings = new DarknetSettings() { Resolution = darknetService.CameraPreview.SupportedResolutions.OrderBy((res) => res.Width * res.Height).First() };

            particleManager = new ParticleManager(absLayout) { Enabled = false };

            BackgroundImage = "background.jpg";

            startDetectionButton = new MenuButton("StartDetectionWhite.png") { IsEnabled = false };
            customizeFilterButton = new MenuButton("CustomizeFilterWhite.png") { IsEnabled = false };
            trainNetworkButton = new MenuButton("TrainNetworkWhite.png") { IsEnabled = false };
            optionsButton = new MenuButton("OptionsWhite.png");
            disconnectButton = new MenuButton("DisconnectWhite.png");
            connectButton = new MenuButton("ConnectWhite.png");
            starButton = new ToggleButton("Stern.png", false);
            volumeButton = new ToggleButton("LautstaerkeButton.png");
            volumeSlider = new Slider(0, 1, 1) { ThumbColor = Color.AliceBlue };

            cyberDogImage = new Image() { Source = "cyber_dog.png", Scale = 0 };
            AbsoluteLayout.SetLayoutBounds(cyberDogImage, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(cyberDogImage, AbsoluteLayoutFlags.All);
            absLayout.Children.Add(cyberDogImage);

            toolbarGrid = new Grid();
            toolbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            toolbarGrid.Padding = new Thickness(0, 0, 0, 0);
            toolbarGrid.Margin = new Thickness(0, 0, 0, 0);
            toolbarGrid.Children.Add(starButton, 0, 0);
            toolbarGrid.Children.Add(volumeButton, 1, 0);
            toolbarGrid.Children.Add(volumeSlider, 2, 0);

            grid.Children.Add(startDetectionButton, 0, 0);
            grid.Children.Add(optionsButton, 0, 1);
            grid.Children.Add(customizeFilterButton, 0, 2);
            grid.Children.Add(trainNetworkButton, 0, 3);
            grid.Children.Add(connectButton, 0, 4);
            grid.Children.Add(toolbarGrid, 0, 5);

            player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.Current;
            player.Load("mainMenu.mp3");
            player.Loop = true;
            player.Play();

            // Handler
            starButton.ToggleChanged += (o, e) =>
            {
                if (e)
                    particleManager.Enabled = true;
                else
                    particleManager.Enabled = false;
            };

            volumeButton.ToggleChanged += (o, e) =>
            {
                if (e)
                    player.Play();
                else
                    player.Pause();
            };

            
            volumeSlider.ValueChanged += (o, e) =>
            {
                player.Volume = volumeSlider.Value;
            };

            connectButton.ClickedAnimationFinished += (o, e) =>
            {
                connectButton.IsEnabled = false;
                if(darknetService.ConnectionState == DarknetService.ConnectionStatus.Disconnected)
                {
                    darknetService.Connect();
                }
            };

            disconnectButton.ClickedAnimationFinished += (o, e) =>
            {
                disconnectButton.IsEnabled = false;
                if(darknetService.ConnectionState == DarknetService.ConnectionStatus.Connected)
                {
                    darknetService.Disconnect();
                }
            };

            optionsButton.ClickedAnimationFinished += (o, e) => Navigation.PushAsync(new SettingsPage(darknetService));
            startDetectionButton.ClickedAnimationFinished += (o, e) => Navigation.PushAsync(new PreviewPage(darknetService));
            customizeFilterButton.ClickedAnimationFinished += (o, e) => Navigation.PushAsync(new ClassFilterPage(darknetService));

            darknetService.ClassCollectionAquired += (o, e) =>
            {
                if (e != null)
                {
                    Device.BeginInvokeOnMainThread(() => customizeFilterButton.IsEnabled = true);
                    if (darknetService.FilteredClasses == null)
                    {
                        darknetService.FilteredClasses = new List<string>();
                        foreach (var obj in e.classes)
                        {
                            darknetService.FilteredClasses.Add(obj);
                        }
                    }
                }
            };

            darknetService.Connected += (o, e) =>
            {
                darknetService.SendSettings();
                Device.BeginInvokeOnMainThread(() =>
                {
                    if(grid.Children.Contains(connectButton))
                    {
                        grid.Children.Remove(connectButton);
                        grid.Children.Add(disconnectButton, 0, 4);
                    }
                    disconnectButton.IsEnabled = true;
                    startDetectionButton.IsEnabled = true;
                });
                cyberDogImage.ScaleTo(1, 250);
            };

            darknetService.Disconnected += (o, e) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if(grid.Children.Contains(disconnectButton))
                    {
                        grid.Children.Remove(disconnectButton);
                        grid.Children.Add(connectButton, 0, 4);
                    }
                    connectButton.IsEnabled = true;
                    startDetectionButton.IsEnabled = false;
                });
                cyberDogImage.ScaleTo(0, 250);
            };
        }

        public async void OnSleep()
        {
            if(darknetService.ConnectionState == DarknetService.ConnectionStatus.Connected)
                darknetService.Disconnect();
            while(Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            particleManager.Enabled = false;
            player.Pause();
        }

        public void OnResume()
        {
            if (volumeButton.IsToggled)
                player.Play();
            if (starButton.IsToggled)
                particleManager.Enabled = true;
        }
    }


}
