using ObjectDetectionLogic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Drawing;

namespace ObjectDetectionApp.Logic
{
    public class DarknetService
    {
        private static DarknetService Instance;

        private AutoResetEvent newImageDataHandle;
        private Thread ClientCommunicationThread;
        public enum ConnectionStatus { Connected, Disconnected };
        private ConnectionStatus connectionState;
        public ConnectionStatus ConnectionState {
            get => connectionState;
            private set
            {
                connectionState = value;
                if (value == ConnectionStatus.Connected)
                    Connected?.Invoke(this, true);
                else
                    Disconnected?.Invoke(this, true);
            }
        }

        private bool connectingAttemptRunning = false;
        private bool StopCommunicationRequest = false;

        /// <summary>
        /// Raw JPEG Image Data
        /// </summary>
        private byte[] latestImageData;
        private byte[] LatestImageData {
            get => latestImageData;
            set
            {
                if(value != latestImageData)
                {
                    latestImageData = value;
                    newImageDataHandle.Set();
                }
            }
        }
        readonly private object imageDataLock = new object();

        /// <summary>
        /// Latest received detection result
        /// </summary>
        private DarknetDetectionResult latestDetectionResult;
        private DarknetDetectionResult LatestDetectionResult
        {
            get => latestDetectionResult;
            set
            {
                if(value != latestDetectionResult)
                {
                    latestDetectionResult = value;
                    DetectionResultAquired?.Invoke(this, value);
                }
            }
        }
        
        public ServiceClient ServiceClient { get; private set; }
        
        public ICameraPreview CameraPreview { get => CrossPlatformHelper<ICameraPreview>.Instance; }

        private DarknetSettings settings;
        public DarknetSettings Settings
        {
            get => settings;
            set
            {
                if(settings == null || !settings.Equals(value))
                {
                    settings = value;
                }
            }
        }

        private Rectangle frameSize;
        public Rectangle FrameSize
        {
            get => frameSize;
            private set
            {
                if (value.X != FrameSize.X || value.Y != FrameSize.Y || value.Width != FrameSize.Width || value.Height != FrameSize.Height)
                    frameSize = value;
            }
        }

        private ClassCollection classCollection;
        public ClassCollection ClassCollection
        {
            get => classCollection;
            private set
            {
                classCollection = value;
                ClassCollectionAquired?.Invoke(this, value);
            }
        }

        public List<string> FilteredClasses { get; set; }

        public event EventHandler<DarknetDetectionResult> DetectionResultAquired;
        public event EventHandler<ClassCollection> ClassCollectionAquired;
        public event EventHandler<bool> Connected;
        public event EventHandler<bool> Disconnected;

        private DarknetService()
        {
            ConnectionState = ConnectionStatus.Disconnected;
            newImageDataHandle = new AutoResetEvent(false);
        }

        public static DarknetService Create()
        {
            if(Instance == null)
            {
                Instance = new DarknetService();
            }
             return Instance;
        }

        public void Connect()
        {
            ServiceClient = ServiceClient.Create(new byte[4] { 141, 47, 69, 88 }, 27015);
            if (connectingAttemptRunning == false)
            {
                connectingAttemptRunning = true;
                ServiceClient.ConnectAsync(result =>
                {
                    // Start preview on connection success
                    ConnectionState = result == ConnectionResult.successfull ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
                    connectingAttemptRunning = false;
                });
            }
        }

        public void Disconnect()
        {
            ServiceClient.Disconnect();
            ConnectionState = ConnectionStatus.Disconnected;
        }

        private void OnFrameAquired(object sender, FrameAquiredEventArgs e)
        {

            FrameSize = new Rectangle(0, 0, e.width, e.height);
            lock(imageDataLock)
            {
                LatestImageData = e.ImageData;
            }
            newImageDataHandle.Set();
        }

        private void DoClientCommunication() {
            while(newImageDataHandle.WaitOne())
            {
                if(StopCommunicationRequest)
                {
                    StopCommunicationRequest = false;
                    break;
                }
                byte[] bytesToSend;
                lock (imageDataLock)
                {
                    bytesToSend = LatestImageData;
                }

                ServiceClient.SendRawData(BitConverter.GetBytes(Convert.ToUInt32(bytesToSend.Length)));
                ServiceClient.SendRawData(bytesToSend);

                LatestDetectionResult = ServiceClient.ReceiveResult();
                CameraPreview.State = FrameState.ReadyForNextFrame;
            }
        }

        public void StartService()
        {
            newImageDataHandle.Reset();
            CameraPreview.State = FrameState.ReadyForNextFrame;
            ClientCommunicationThread = new Thread(DoClientCommunication);
            ClientCommunicationThread.Start();
            SendSettings();
            // Start Analyse
            ServiceClient.SendRawData(new byte[1] { 0x05 });
            CameraPreview.PreviewResolution = Settings.Resolution;
            CameraPreview.StartPreview(() => CameraPreview.FrameAquired += OnFrameAquired);
        }

        public void StopService() {
            StopCommunicationRequest = true;
            newImageDataHandle.Set();
            ClientCommunicationThread?.Join(1000);
            CameraPreview.StopPreview();
            Disconnect();
        }

        public void SendSettings()
        {
            // Set Threshold
            ServiceClient.SendRawData(new byte[2] { 0x02, Convert.ToByte(Settings.Threshold) });
            // Set image format
            ServiceClient.SendRawData(new byte[2] { 0x04, Convert.ToByte(Settings.ImageFormat) });
            // Get classes
            ServiceClient.SendRawData(new byte[1] { 0x01 });
            ClassCollection = ServiceClient.ReceiveClassCollection();
        }
    }
}
