using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using ApxLabs.FastAndroidCamera;
using ObjectDetectionApp.Logic;
using Java.IO;
using System.IO;

using Camera = Android.Hardware.Camera;

namespace ObjectDetectionApp.Droid
{
    class CameraPreview : Java.Lang.Object, ICameraPreview, INonMarshalingPreviewCallback
    {
        public bool IsPreviewAvailable => true;

        private enum RunningState { stopped, running };
        private RunningState runningState = RunningState.stopped;

        public bool IsRunning { get => runningState == RunningState.running; }
        public FrameState State { get; set; }

        private SurfaceTexture previewTexture;
        public SurfaceTexture PreviewTexture
        {
            get => previewTexture;
            set
            {
                if(previewTexture != value)
                {
                    previewTexture = value;
                    RestartPreview();
                }
            }
        }

        private CameraFacing cameraFacing = CameraFacing.Back;
        public CameraFacing CameraFacing
        {
            get => cameraFacing;
            set
            {
                if(value != cameraFacing)
                {
                    cameraFacing = value;
                    RestartPreview();
                }
            }
        }

        private int displayOrientation;
        public int DisplayOrientation
        {
            get => displayOrientation;
            set
            {
                if(value != displayOrientation)
                {
                    displayOrientation = value;
                    RestartPreview();
                }
            }
        }

        private bool flashEnabled;
        public bool FlashEnabled
        {
            get => flashEnabled;
            set
            {
                if (value != flashEnabled)
                {
                    flashEnabled = value;
                    if (CameraFacing == CameraFacing.Back)
                        RestartPreview();
                }
            }
        }

        private Camera camera;
        private Camera.Parameters parameters;

        public Camera.Parameters Parameters
        {
            get => parameters;
            set
            {
                if(value != parameters)
                {
                    parameters = value;
                }
            }
        }

        public IEnumerable<Resolution> SupportedResolutions
        {
            get
            {
                if(runningState == RunningState.stopped)
                {
                    List<Resolution> outList = new List<Resolution>();
                    camera = Camera.Open((int)CameraFacing);
                    foreach(var res in camera.GetParameters().SupportedPreviewSizes)
                    {
                        outList.Add(new Resolution { Height = res.Height, Width = res.Width });
                    }
                    camera.Release();
                    return outList;
                }
                else
                {
                    throw new Exception("This action is only supported when preview is not running");
                }
            }
        }

        public Resolution PreviewResolution { get; set; }

        public event EventHandler<FrameAquiredEventArgs> FrameAquired;

        public void StartPreview(OnPreviewStartedCallback onPreviewStartedCallback)
        {
            camera = Camera.Open((int) CameraFacing);
            parameters = camera.GetParameters();
            parameters.PictureFormat = Android.Graphics.ImageFormatType.Jpeg;
            parameters.PreviewFormat = Android.Graphics.ImageFormatType.Nv21;
            //var size = parameters.SupportedPreviewSizes.OrderBy((r) => r.Height * r.Width).ToArray()[2];
            //parameters.SetPreviewSize(size.Width, size.Height);
            Camera.Size size = parameters.SupportedPreviewSizes.Where((res) => (res.Width == PreviewResolution.Width) && (res.Height == PreviewResolution.Height)).First();
            parameters.SetPreviewSize(size.Width, size.Height);
            if(CameraFacing == CameraFacing.Front)
                parameters.FlashMode = global::Android.Hardware.Camera.Parameters.FlashModeOff;
            else
            {
                if (FlashEnabled)
                    parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeTorch;
                else
                    parameters.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOn;
            }
            
            // snip - set resolution, frame rate, preview format, etc.
            camera.SetDisplayOrientation(DisplayOrientation);
            camera.SetParameters(parameters);
            camera.SetPreviewTexture(PreviewTexture);
            // assuming the SurfaceView has been set up elsewhere
            // camera.SetPreviewDisplay(_surfaceView.Holder);
            camera.StartPreview();

            int numBytes = (parameters.PreviewSize.Width * parameters.PreviewSize.Height * Android.Graphics.ImageFormat.GetBitsPerPixel(parameters.PreviewFormat)) / 8;
            //int numBytes = 100000;
            for (uint i = 0; i < 1; ++i)
            {
                using (FastJavaByteArray buffer = new FastJavaByteArray(numBytes))
                {
                    // allocate new Java byte arrays for Android to use for preview frames
                    camera.AddCallbackBuffer(new FastJavaByteArray(numBytes));
                }
                // The using block automatically calls Dispose() on the buffer, which is safe
                // because it does not automaticaly destroy the Java byte array. It only releases
                // our JNI reference to that array; the Android Camera (in Java land) still
                // has its own reference to the array.
            }

            // non-marshaling version of the preview callback
            camera.SetNonMarshalingPreviewCallback(this);

            runningState = RunningState.running;
            onPreviewStartedCallback.Invoke();
        }

        public void StopPreview()
        {
            camera.StopPreview();
            camera.Release();
            runningState = RunningState.stopped;
        }

        public void RestartPreview()
        {
            if(IsRunning)
            {
                StopPreview();
                StartPreview(() => { });
            }
        }

        public void OnPreviewFrame(IntPtr data, Camera camera)
        {
            // Wrap the JNI reference to the Java byte array
            using (FastJavaByteArray buffer = new FastJavaByteArray(data))
            {
                if (State == FrameState.ReadyForNextFrame && FrameAquired != null)
                {
                    State = FrameState.WaitForReady;

                    DateTime time = DateTime.Now;

                    // reuse the Java byte array; return it to the Camera API
                    // Camera.Parameters parameters = camera.GetParameters();
                    var size = parameters.PreviewSize;
                    YuvImage image = new YuvImage(buffer.ToArray(), ImageFormatType.Nv21, size.Width, size.Height, null);
                    Rect rectangle = new Rect() { Bottom = size.Height, Left = 0, Right = size.Width, Top = 0 };
                    var out2 = new System.IO.MemoryStream();
                    image.CompressToJpeg(rectangle, 100, out2);

                    System.Diagnostics.Debug.WriteLine("Time for converting and compressing: " + (DateTime.Now - time).TotalMilliseconds + " ms");

                    FrameAquired?.Invoke(this, new FrameAquiredEventArgs() { ImageData = out2.ToArray() });
                }

                camera.AddCallbackBuffer(buffer);

                // Don't do anything else with the buffer at this point - it now "belongs" to
                // Android, and the Camera could overwrite the data at any time.
                
            }
            // The end of the using() block calls Dispose() on the buffer, releasing our JNI
            // reference to the array
        }

        public new void Dispose()
        {
            if (IsRunning) StopPreview();
            base.Dispose();
        }
    }
}