using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AVFoundation;
using CoreImage;
using CoreMedia;
using CoreVideo;
using Foundation;
using ObjectDetectionApp.Logic;
using UIKit;

namespace ObjectDetectionApp.iOS
{
    class CameraPreview : ICameraPreview, IAVCaptureVideoDataOutputSampleBufferDelegate
    {
        public FrameState State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IEnumerable<Resolution> SupportedResolutions => new List<Resolution>() { new Resolution() { Width = 500, Height = 500 } };

        public Resolution PreviewResolution { get; set; }

        public bool IsPreviewAvailable => true;

        public bool IsRunning { get; set; }

        public event EventHandler<FrameAquiredEventArgs> FrameAquired;

        public UIView LiveCameraView
        {
            get => liveCameraView;
            set
            {
                liveCameraView = value;
            }
        }

        public AVCaptureDevicePosition CaptureDevicePosition
        {
            get => captureDeviceInput.Device.Position;
            set
            {
                if(IsRunning)
                {
                    var device = GetCameraForOrientation(value);
                    ConfigureCameraForDevice(device);

                    captureSession.BeginConfiguration();
                    captureSession.RemoveInput(captureDeviceInput);
                    captureDeviceInput = AVCaptureDeviceInput.FromDevice(device);
                    captureSession.AddInput(captureDeviceInput);
                    captureSession.CommitConfiguration();
                }
            }
        }

        public bool HasFlash { get => captureDeviceInput.Device.HasFlash; }

        public AVCaptureFlashMode FlashMode {
            get => captureDeviceInput.Device.FlashMode;
            set
            {
                if(HasFlash && IsRunning)
                {
                    var error = new NSError();
                    captureDeviceInput.Device.LockForConfiguration(out error);
                    captureDeviceInput.Device.FlashMode = value == AVCaptureFlashMode.Off ? AVCaptureFlashMode.Off : AVCaptureFlashMode.On;
                    captureDeviceInput.Device.UnlockForConfiguration();
                }
            }
        }

        public IntPtr Handle => throw new NotImplementedException();

        AVCaptureSession captureSession;
        AVCaptureDeviceInput captureDeviceInput;
        AVCaptureVideoDataOutput captureVideoDataOutput;
        UIView liveCameraView;

        public void StartPreview(OnPreviewStartedCallback onPreviewStartedCallback)
        {
            SetupLiveCameraStream();
            IsRunning = true;
        }

        public void StopPreview()
        {
            if (captureDeviceInput != null && captureSession != null)
            {
                captureSession.RemoveInput(captureDeviceInput);
            }

            if (captureDeviceInput != null)
            {
                captureDeviceInput.Dispose();
                captureDeviceInput = null;
            }

            if (captureSession != null)
            {
                captureSession.StopRunning();
                captureSession.Dispose();
                captureSession = null;
            }

            IsRunning = false;
        }

        void SetupLiveCameraStream()
        {
            captureSession = new AVCaptureSession();

            if(liveCameraView != null)
            {
                var viewLayer = liveCameraView.Layer;
                var videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
                {
                    Frame = liveCameraView.Bounds
                };
                liveCameraView.Layer.AddSublayer(videoPreviewLayer);
            }

            var captureDevice = AVCaptureDevice.GetDefaultDevice(AVMediaType.Video);
            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            
            var dictionary = new NSMutableDictionary();
            dictionary[AVVideo.CodecKey] = new NSNumber((int)AVVideoCodec.JPEG);
            captureVideoDataOutput = new AVCaptureVideoDataOutput()
            {
                CompressedVideoSetting = new AVVideoSettingsCompressed(new NSDictionary()),
                
                
            };
            var outputRecorder = new OutputRecorder();
            captureVideoDataOutput.SetSampleBufferDelegateQueue(outputRecorder, new CoreFoundation.DispatchQueue("MyQueue"));
            captureSession.AddOutput(captureVideoDataOutput);
            captureSession.AddInput(captureDeviceInput);
            captureSession.StartRunning();
        }
       
        void ConfigureCameraForDevice(AVCaptureDevice device)
        {
            var error = new NSError();
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                device.UnlockForConfiguration();
            }
            else if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
            {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                device.UnlockForConfiguration();
            }
            else if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
            {
                device.LockForConfiguration(out error);
                device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
                device.UnlockForConfiguration();
            }
        }

        AVCaptureDevice GetCameraForOrientation(AVCaptureDevicePosition orientation)
        {
            var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);

            foreach (var device in devices)
            {
                if (device.Position == orientation)
                {
                    return device;
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (IsRunning) StopPreview();
        }
    }

    public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate
    {
        public OutputRecorder()
        {
        }

        public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
        {
            try
            {
                ImageFromSampleBuffer(sampleBuffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void ImageFromSampleBuffer(CMSampleBuffer sampleBuffer)
        {
            var size = sampleBuffer.TotalSampleSize;
            // Get the CoreVideo image
            using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
            {
                // Lock the base address
                pixelBuffer.Lock(CVOptionFlags.None);
                // Get the number of bytes per row for the pixel buffer
                pixelBuffer.Unlock(CVOptionFlags.None);
            }
        }
    }
}