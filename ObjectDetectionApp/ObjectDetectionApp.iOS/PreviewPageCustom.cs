using AVFoundation;
using CoreGraphics;
using CustomRenderer;
using CustomRenderer.iOS;
using Foundation;
using ObjectDetectionApp;
using ObjectDetectionApp.iOS;
using ObjectDetectionApp.Presentation;
using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(PreviewPage), typeof(PreviewPageCustom))]
namespace CustomRenderer.iOS
{
    public class PreviewPageCustom : PageRenderer
    {
        AVCaptureStillImageOutput stillImageOutput;
        UIView liveCameraStream;
        UIButton takePhotoButton;
        UIButton toggleCameraButton;
        UIButton toggleFlashButton;

        PreviewPage previewPage;
        CameraPreview cameraPreview;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            
            if (e.OldElement != null || Element == null)
            {
                return;
            }

            previewPage = e.NewElement as PreviewPage;
            cameraPreview = previewPage.UnderlyingService.CameraPreview as CameraPreview;
            cameraPreview.LiveCameraView = liveCameraStream;
            previewPage.UnderlyingService.DetectionResultAquired += (o, result) => {
                
            };
            previewPage.UnderlyingService.StartService();
            try
            {
                SetupUserInterface();
                SetupEventHandlers();
                cameraPreview.StartPreview(() => { });
                AuthorizeCameraUse();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"\t\t\tERROR: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            cameraPreview.StopPreview();
            base.Dispose(disposing);
        }

        void SetupUserInterface()
        {
            var centerButtonX = View.Bounds.GetMidX() - 35f;
            var topLeftX = View.Bounds.X + 25;
            var topRightX = View.Bounds.Right - 65;
            var bottomButtonY = View.Bounds.Bottom - 150;
            var topButtonY = View.Bounds.Top + 15;
            var buttonWidth = 70;
            var buttonHeight = 70;

            liveCameraStream = new UIView()
            {
                Frame = new CGRect(0f, 0f, View.Bounds.Width, View.Bounds.Height)
            };

            takePhotoButton = new UIButton()
            {
                Frame = new CGRect(centerButtonX, bottomButtonY, buttonWidth, buttonHeight)
            };
            takePhotoButton.SetBackgroundImage(UIImage.FromFile("TakePhotoButton.png"), UIControlState.Normal);

            toggleCameraButton = new UIButton()
            {
                Frame = new CGRect(topRightX, topButtonY + 5, 35, 26)
            };
            toggleCameraButton.SetBackgroundImage(UIImage.FromFile("ToggleCameraButton.png"), UIControlState.Normal);

            toggleFlashButton = new UIButton()
            {
                Frame = new CGRect(topLeftX, topButtonY, 37, 37)
            };
            toggleFlashButton.SetBackgroundImage(UIImage.FromFile("NoFlashButton.png"), UIControlState.Normal);

            View.Add(liveCameraStream);
            View.Add(takePhotoButton);
            View.Add(toggleCameraButton);
            View.Add(toggleFlashButton);
        }

        void SetupEventHandlers()
        {
            takePhotoButton.TouchUpInside += (object sender, EventArgs e) => {
                CapturePhoto();
            };

            toggleCameraButton.TouchUpInside += (object sender, EventArgs e) => {
                cameraPreview.CaptureDevicePosition = cameraPreview.CaptureDevicePosition == AVCaptureDevicePosition.Front ? AVCaptureDevicePosition.Back : AVCaptureDevicePosition.Front;
            };

            toggleFlashButton.TouchUpInside += (object sender, EventArgs e) => {
                ToggleFlash();
            };
        }

        async void CapturePhoto()
        {
            var videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
            var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);
            var jpegImage = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);

            var photo = new UIImage(jpegImage);
            photo.SaveToPhotosAlbum((image, error) =>
            {
                if (!string.IsNullOrEmpty(error?.LocalizedDescription))
                {
                    Console.Error.WriteLine($"\t\t\tError: {error.LocalizedDescription}");
                }
            });
        }

        void ToggleFlash()
        {
            if (cameraPreview.HasFlash)
            {
                if (cameraPreview.FlashMode == AVCaptureFlashMode.On)
                {
                    cameraPreview.FlashMode = AVCaptureFlashMode.Off;
                    toggleFlashButton.SetBackgroundImage(UIImage.FromFile("NoFlashButton.png"), UIControlState.Normal);
                }
                else
                {
                    cameraPreview.FlashMode = AVCaptureFlashMode.On;
                    toggleFlashButton.SetBackgroundImage(UIImage.FromFile("FlashButton.png"), UIControlState.Normal);
                }
            }
        }
        
        async void AuthorizeCameraUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);
            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
        }
    }
}

