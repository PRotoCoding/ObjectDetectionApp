using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ObjectDetectionApp.Logic
{
    public enum FrameState { ReadyForNextFrame, WaitForReady }
    public enum PreviewState { Running, Stopped }

    public delegate void OnPreviewStartedCallback();
    public class FrameAquiredEventArgs
    {
        public byte[] ImageData;
        public MemoryStream jpegStream;
        public MemoryStream bitmapStream;
        public byte[] BitmapData;
        public int width;
        public int height;
        public int compressionQuality;
    }

    public interface ICameraPreview : ICrossPlatform
    {
        FrameState State { get; set; }

        event EventHandler<FrameAquiredEventArgs> FrameAquired;

        IEnumerable<Resolution> SupportedResolutions { get; }

        Resolution PreviewResolution { get; set; }

        bool IsPreviewAvailable { get; }

        void StartPreview(OnPreviewStartedCallback onPreviewStartedCallback);

        void StopPreview();
    }
}
