using System;
using System.IO;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using CustomRenderer;
using CustomRenderer.Droid;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using ObjectDetectionApp;
using ObjectDetectionApp.Droid;
using Camera = Android.Hardware.Camera;
using System.Collections.Generic;
using Color = Android.Graphics.Color;
using ObjectDetectionApp.Logic;
using ObjectDetectionApp.Presentation;

[assembly: ExportRenderer(typeof(PreviewPage), typeof(PreviewPageCustom))]
namespace CustomRenderer.Droid
{
    public class PreviewPageCustom : PageRenderer, TextureView.ISurfaceTextureListener
    {
        
        global::Android.Widget.Button toggleFlashButton;
        global::Android.Widget.Button switchCameraButton;
        global::Android.Views.View view;

        Activity activity;
        TextureView textureView;
        SurfaceTexture surfaceTexture;
        SurfaceView transparentView;
        ISurfaceHolder holderTransparent;

        PreviewPage previewPage;
        CameraPreview cameraPreview;
        public PreviewPageCustom(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null || Element == null)
            {
                return;
            }

            previewPage = e.NewElement as PreviewPage;
            

            try
            {
                SetupUserInterface();
                SetupEventHandlers();
                AddView(view);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(@"			ERROR: ", ex.Message);
            }
        }

        void OnDetectionResultAquired(object sender, DarknetDetectionResult result)
        {
            var canvas = holderTransparent.LockCanvas();
            canvas.DrawColor(Android.Graphics.Color.Transparent, PorterDuff.Mode.Clear);
            //border's properties
            var paint = new Paint();
            paint.SetStyle(Paint.Style.Stroke);

            paint.StrokeWidth = 12;

            foreach (var obj in result.objects)
            {
                if(previewPage.UnderlyingService.FilteredClasses.Contains(obj.name))
                {
                    previewPage.ClassColors.TryGetValue(obj.name, out Xamarin.Forms.Color color);
                    paint.Color = color.ToAndroid();

                    Rect rect = new Rect((int)((obj.relative_Coordinates.center_x - obj.relative_Coordinates.width / 2) * transparentView.Width),
                        (int)((obj.relative_Coordinates.center_y - obj.relative_Coordinates.height / 2) * transparentView.Height),
                        (int)((obj.relative_Coordinates.center_x + obj.relative_Coordinates.width / 2) * transparentView.Width),
                        (int)((obj.relative_Coordinates.center_y + obj.relative_Coordinates.height / 2) * transparentView.Height));

                    canvas.DrawRect(rect, paint);
                    canvas.DrawText(obj.name,
                        (float)(obj.relative_Coordinates.center_x - obj.relative_Coordinates.width / 2) * transparentView.Width + 10,
                        (float)(obj.relative_Coordinates.center_y - obj.relative_Coordinates.height / 2) * transparentView.Height + 50,
                        new Paint() { Color = color.ToAndroid(), TextSize = 60 });
                }
            }
            holderTransparent.UnlockCanvasAndPost(canvas);
        }

        void SetupUserInterface()
        {
            activity = this.Context as Activity;
            view = activity.LayoutInflater.Inflate(ObjectDetectionApp.Droid.Resource.Layout.CameraLayout, this, false);
            transparentView = view.FindViewById<SurfaceView>(ObjectDetectionApp.Droid.Resource.Id.transparentView);
            transparentView.SetZOrderOnTop(true);
            holderTransparent = transparentView.Holder;
            holderTransparent.SetFormat(Format.Transparent);

            textureView = view.FindViewById<TextureView>(ObjectDetectionApp.Droid.Resource.Id.textureView);
            textureView.SurfaceTextureListener = this;
        }

        void SetupEventHandlers()
        {
            //takePhotoButton = view.FindViewById<global::Android.Widget.Button>(ObjectDetectionApp.Droid.Resource.Id.takePhotoButton);
            //takePhotoButton.Click += TakePhotoButtonTapped;

            switchCameraButton = view.FindViewById<global::Android.Widget.Button>(ObjectDetectionApp.Droid.Resource.Id.switchCameraButton);
            switchCameraButton.Click += SwitchCameraButtonTapped;

            toggleFlashButton = view.FindViewById<global::Android.Widget.Button>(ObjectDetectionApp.Droid.Resource.Id.toggleFlashButton);
            toggleFlashButton.Click += ToggleFlashButtonTapped;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            view.Measure(msw, msh);
            view.Layout(0, 0, r - l, b - t);
            transparentView.Measure(msw, msh);
            transparentView.Layout(0, 0, r - l, b - t);
            textureView.Measure(msw, msh);
            textureView.Layout(0, 0, r - l, b - t);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            cameraPreview = previewPage.UnderlyingService.CameraPreview as CameraPreview;
            cameraPreview.CameraFacing = CameraFacing.Back;
            cameraPreview.FlashEnabled = false;
            cameraPreview.PreviewTexture = surface;
            cameraPreview.DisplayOrientation = 0;
            previewPage.UnderlyingService.DetectionResultAquired += OnDetectionResultAquired;
            textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            surfaceTexture = surface;
            previewPage.UnderlyingService.StartService();
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            previewPage.UnderlyingService.DetectionResultAquired -= OnDetectionResultAquired;
            cameraPreview.PreviewTexture = null;
            return true;
        }

        void ToggleFlashButtonTapped(object sender, EventArgs e)
        {
            if(cameraPreview.FlashEnabled)
            {
                cameraPreview.FlashEnabled = false;
                toggleFlashButton.SetBackgroundResource(ObjectDetectionApp.Droid.Resource.Drawable.NoFlashButton);
            }
            else
            {
                cameraPreview.FlashEnabled = true;
                toggleFlashButton.SetBackgroundResource(ObjectDetectionApp.Droid.Resource.Drawable.FlashButton);
            }
        }

        void SwitchCameraButtonTapped(object sender, EventArgs e)
        {
            cameraPreview.CameraFacing = cameraPreview.CameraFacing == CameraFacing.Back ? CameraFacing.Front : CameraFacing.Back;
        }

        //async void TakePhotoButtonTapped(object sender, EventArgs e)
        //{
        //    camera.StopPreview();

        //    var image = textureView.Bitmap;

        //    try
        //    {
        //        var absolutePath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim).AbsolutePath;
        //        var folderPath = absolutePath + "/Camera";
        //        var filePath = System.IO.Path.Combine(folderPath, string.Format("photo_{0}.jpg", Guid.NewGuid()));

        //        var fileStream = new FileStream(filePath, FileMode.Create);
        //        await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, fileStream);
        //        fileStream.Close();
        //        image.Recycle();

        //        var intent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
        //        var file = new Java.IO.File(filePath);
        //        var uri = Android.Net.Uri.FromFile(file);
        //        intent.SetData(uri);
        //        MainActivity.Instance.SendBroadcast(intent);
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine(@"				", ex.Message);
        //    }

        //    camera.StartPreview();
        //}

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {

        }
    }
}