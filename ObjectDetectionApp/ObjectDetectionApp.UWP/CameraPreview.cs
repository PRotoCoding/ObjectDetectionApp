using ObjectDetectionApp.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media;
using Windows.Media.MediaProperties;
using CameraStream;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

namespace ObjectDetectionApp.UWP
{
    class CameraPreview : ICameraPreview
    {
        public bool IsPreviewAvailable => throw new NotImplementedException();

        public FrameState State { get; set; }

        public event EventHandler<FrameAquiredEventArgs> FrameAquired;

        private VideoCapture videoCapture;
        private Resolution _resolution;

        public async void StartPreview(OnPreviewStartedCallback onPreviewStartedCallback)
        {
            await VideoCapture.CreateAync(v => videoCapture = v ) ;

            if (videoCapture == null)
            {
                Debug.Write("Did not find a video capture object. You may not be using the HoloLens.");
                return;
            }

            _resolution = GetLowestResolution();
            float frameRate = GetHighestFrameRate(_resolution);
            videoCapture.FrameSampleAcquired += OnFrameSampleAcquired;

            //You don't need to set all of these params.
            //I'm just adding them to show you that they exist.
            CameraParameters cameraParams = new CameraParameters();
            cameraParams.cameraResolutionHeight = _resolution.height;
            cameraParams.cameraResolutionWidth = _resolution.width;
            cameraParams.frameRate = (int) Math.Round(frameRate);
            cameraParams.pixelFormat = CapturePixelFormat.BGRA32;
            cameraParams.rotateImage180Degrees = false; //If your image is upside down, remove this line.
            cameraParams.enableHolograms = false;

            videoCapture.StartVideoModeAsync(cameraParams, (result) =>
            {
                if (!result.success) throw new Exception("Starting Video Mode failed");
                onPreviewStartedCallback.Invoke();
            });
        }

        private async void OnFrameSampleAcquired(VideoCaptureSample videoCaptureSample)
        {
            if(FrameAquired != null && State == FrameState.ReadyForNextFrame)
            {
                State = FrameState.WaitForReady;
                var buffer = new Windows.Storage.Streams.Buffer((uint)videoCaptureSample.dataLength);
                videoCaptureSample.bitmap.CopyToBuffer(buffer);

                DataReader dataReader = DataReader.FromBuffer(buffer);
                byte[] bytes = new byte[buffer.Length];
                dataReader.ReadBytes(bytes);

                var inputStream = new InMemoryRandomAccessStream();

                BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, inputStream);

                bitmapEncoder.SetSoftwareBitmap(videoCaptureSample.bitmap);
                await bitmapEncoder.FlushAsync();

                var encodedData = new Windows.Storage.Streams.Buffer((uint)inputStream.Size);
                await inputStream.ReadAsync(encodedData, (uint) inputStream.Size, InputStreamOptions.None);
                Debug.WriteLine("Size of JPEG Image: " + encodedData.Length);

                DataReader dataReader2 = DataReader.FromBuffer(encodedData);
                byte[] encodedBytes = new byte[encodedData.Length];
                dataReader2.ReadBytes(encodedBytes);

                dataReader.Dispose();
                dataReader2.Dispose();

                FrameAquired?.Invoke(this, new FrameAquiredEventArgs() { BitmapData = bytes, width = _resolution.width, height = _resolution.height, ImageData = encodedBytes });
            }
            videoCaptureSample.Dispose();
        }

        public void StopPreview()
        {
            throw new NotImplementedException();
        }

        public Resolution GetHighestResolution()
        {
            if (videoCapture == null)
            {
                throw new Exception("Please call this method after a VideoCapture instance has been created.");
            }
            return videoCapture.GetSupportedResolutions().OrderByDescending((r) => r.width * r.height).FirstOrDefault();
        }

        public Resolution GetLowestResolution()
        {
            if (videoCapture == null)
            {
                throw new Exception("Please call this method after a VideoCapture instance has been created.");
            }
            return videoCapture.GetSupportedResolutions().OrderBy((r) => r.width * r.height).FirstOrDefault();
        }

        public float GetHighestFrameRate(Resolution forResolution)
        {
            if (videoCapture == null)
            {
                throw new Exception("Please call this method after a VideoCapture instance has been created.");
            }
            return videoCapture.GetSupportedFrameRatesForResolution(forResolution).OrderByDescending(r => r).FirstOrDefault();
        }

        public float GetLowestFrameRate(Resolution forResolution)
        {
            if (videoCapture == null)
            {
                throw new Exception("Please call this method after a VideoCapture instance has been created.");
            }
            return videoCapture.GetSupportedFrameRatesForResolution(forResolution).OrderBy(r => r).FirstOrDefault();
        }
    }
}
