using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ObjectDetectionApp.Presentation;

//[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace ObjectDetectionApp
{
    public partial class App : Application
    {
        Presentation.Menu mainMenu;
        public App()
        {
            //InitializeComponent();
            mainMenu = new ObjectDetectionApp.Presentation.Menu();
            MainPage = new NavigationPage(mainMenu);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            mainMenu.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            mainMenu.OnResume();
        }
    }
}
