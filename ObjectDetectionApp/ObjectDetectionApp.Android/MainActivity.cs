using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Plugin.CurrentActivity;
using ObjectDetectionApp.Logic;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using Android.Util;
using Android.Support.Design.Widget;
using Lottie.Forms.Droid;

namespace ObjectDetectionApp.Droid
{
    [Activity(Label = "FastObjectDetection", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainActivity Instance { get; internal set; }

        Bundle SavedInstanceState;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            SavedInstanceState = savedInstanceState;

            base.OnCreate(savedInstanceState);
            Instance = this;
            //AnimationViewRenderer.Init();
            //FFImageLoading.Forms.Platform.CachedImageRenderer.Init(null);
            
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == (int)Permission.Granted)
            {
                // We have permission, go ahead and use the camera.
                StartApp();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.Camera }, 0);
            }

            
            
        }

        public void StartApp()
        {
            CrossPlatformHelper<IImageResizer>.AddImplementation(new ImageResizer());
            CrossPlatformHelper<ICameraPreview>.AddImplementation(new CameraPreview());
            CrossCurrentActivity.Current.Init(this, SavedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, SavedInstanceState);
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == (int)Permission.Granted)
            {
                // We have permission, go ahead and use the camera.
                StartApp();
            }
        }
    }
}