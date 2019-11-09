using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using Newtonsoft.Json;
using ObjectDetectionApp.Logic;
using System.IO;

namespace ObjectDetectionLogic
{
    public enum ConnectionResult { successfull, failure }

    public delegate void OnConnectedCallback(ConnectionResult result);

    public class OnDisconnectedEventArgs { public IPEndPoint Endpoint; }

    public class ServiceClient
    {
        Socket clientSocket;
        IPEndPoint endPoint;

        private IPEndPoint EndPoint {
            get => endPoint;
            set {
                // Changes of endpoint are only allowed, when there are no connections running
                if(!clientSocket.Connected || clientSocket == null || endPoint == null)
                {
                    endPoint = value;
                }
            }
        }

        private NetworkStream Stream { get; set; }

        private StreamReader StreamReader { get; set; }

        public override string ToString() { return ("IP Address: " + EndPoint.Address.ToString() + ", Port: " + EndPoint.Port.ToString()); }

        private ServiceClient(byte[] address, int port)
           : this(new IPEndPoint(new IPAddress(address), port)) { }

        private ServiceClient(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
            clientSocket = new Socket(EndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Creates a new endpoint or returns an already existing one
        /// </summary>
        /// <param name="address">IPv4 address as string, example: "141.47.69.88" </param>
        /// <param name="port">Port of the target application</param>
        /// <returns>Returns ServiceClient instance</returns>
        public static ServiceClient Create(string address, int port)
        {
            // Parse byte[] address from string
            string[] numbers = address.Split('.');
            if (numbers.Count() != 4) return null;
            byte[] byteArray = new byte[4];
            int i = 0;
            foreach (string a in numbers)
            {
                if (!a.All(c => char.IsDigit(c)) || a.Count() > 3) return null;
                if (!byte.TryParse(a, out byteArray[i++])) return null;
            }
            return Create(byteArray, port);
        }

        /// <summary>
        /// Creates a new endpoint or returns an already existing one
        /// </summary>
        /// <param name="address">IPv4 address as byte array, example: {141, 47, 69, 88}</param>
        /// <param name="port">Port of the target application</param>
        /// <returns>Returns ServiceClient instance</returns>
        public static ServiceClient Create(byte[] address, int port)
        {
            IPEndPoint endPoint = new IPEndPoint(new IPAddress(address), port);
            return new ServiceClient(endPoint);
        }

        public async void ConnectAsync(OnConnectedCallback onConnectedCallback)
        {
            try
            {
                await clientSocket.ConnectAsync(EndPoint).ContinueWith(t => {
                    if(clientSocket.Connected)
                    {
                        Stream = new NetworkStream(clientSocket);
                        StreamReader = new StreamReader(Stream, Encoding.ASCII, false, 3000);
                        onConnectedCallback.Invoke(ConnectionResult.successfull);
                    }
                    else
                    {
                        onConnectedCallback.Invoke(ConnectionResult.failure);
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while connecting: {0}", e.ToString());
                onConnectedCallback.Invoke(ConnectionResult.failure);
            }
        }

        public void Disconnect()
        {
            clientSocket.Disconnect(false);
            clientSocket.Dispose();
            Stream.Dispose();
            StreamReader.Dispose();
        }

        public void SendRawData(byte[] data)
        {
            clientSocket.Send(data);
        }

        public string ReceiveJSON()
        {
            string response = "";
            // Brace ratio is the numerical difference of "{" and "}"
            int braceDiff = 0;

            while (!response.Contains("{") || braceDiff != 0)
            {
                response += StreamReader.ReadLine();
                braceDiff = response.Replace("{", "").Length - response.Replace("}", "").Length;
            }

            return response;
        }

        public DarknetDetectionResult ReceiveResult()
        {
            return JsonConvert.DeserializeObject<DarknetDetectionResult>(ReceiveJSON());
        }

        public ClassCollection ReceiveClassCollection()
        {
            return JsonConvert.DeserializeObject<ClassCollection>(ReceiveJSON());
        }
    }
}
