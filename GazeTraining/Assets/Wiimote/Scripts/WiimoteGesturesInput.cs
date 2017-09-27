// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.VR.WSA.Input;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Globalization;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Input source for gestures information from the WSA APIs, which gives access to various system-supported gestures.
    /// This is a wrapper on top of GestureRecognizer.
    /// </summary>
    public class WiimoteGesturesInput : WiimoteBaseInputSource
    {
        [Tooltip("Set to true to use the use rails (guides) for the navigation gesture, as opposed to full 3D navigation.")]
        public bool UseRailsNavigation = false;

        private GestureRecognizer gestureRecognizer;
        private GestureRecognizer navigationGestureRecognizer;

        private bool wiimoteFound = false;
        public bool usingWiimote = true;
        private bool readyToReceive = false;
        private bool click_a = false;
        private bool press_b = false;
        private bool release_b = false;

        private bool connected = false;
        private int waitCount = 10;

        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent _clientDone = new ManualResetEvent(false);

        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5;

        // The maximum size of the data buffer to use with the asynchronous socket methods
        const int MAX_BUFFER_SIZE = 64;

        // Cached Socket object that will be used by each call for the lifetime of this class
        Socket _socket = null;
        public override SupportedInputEvents SupportedEvents
        {
            get
            {
                return SupportedInputEvents.SourceClicked |
                        SupportedInputEvents.Hold |
                        SupportedInputEvents.Manipulation |
                        SupportedInputEvents.Navigation;
            }
        }

        private void Awake()
        {
            Connect("192.168.0.143", 4511);
            while (!_socket.Connected) ;
            Send("I'm Alive");

            gestureRecognizer = new GestureRecognizer();
            gestureRecognizer.TappedEvent += OnTappedEvent;

            gestureRecognizer.HoldStartedEvent += OnHoldStartedEvent;
            gestureRecognizer.HoldCompletedEvent += OnHoldCompletedEvent;
            gestureRecognizer.HoldCanceledEvent += OnHoldCanceledEvent;

            gestureRecognizer.ManipulationStartedEvent += OnManipulationStartedEvent;
            gestureRecognizer.ManipulationUpdatedEvent += OnManipulationUpdatedEvent;
            gestureRecognizer.ManipulationCompletedEvent += OnManipulationCompletedEvent;
            gestureRecognizer.ManipulationCanceledEvent += OnManipulationCanceledEvent;

            gestureRecognizer.SetRecognizableGestures(GestureSettings.Tap |
                                                      GestureSettings.ManipulationTranslate |
                                                      GestureSettings.Hold);
            gestureRecognizer.StartCapturingGestures();

            // We need a separate gesture recognizer for navigation, since it isn't compatible with manipulation
            navigationGestureRecognizer = new GestureRecognizer();

            navigationGestureRecognizer.NavigationStartedEvent += OnNavigationStartedEvent;
            navigationGestureRecognizer.NavigationUpdatedEvent += OnNavigationUpdatedEvent;
            navigationGestureRecognizer.NavigationCompletedEvent += OnNavigationCompletedEvent;
            navigationGestureRecognizer.NavigationCanceledEvent += OnNavigationCanceledEvent;

            if (UseRailsNavigation)
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationRailsX |
                                                                    GestureSettings.NavigationRailsY |
                                                                    GestureSettings.NavigationRailsZ);
            }
            else
            {
                navigationGestureRecognizer.SetRecognizableGestures(GestureSettings.NavigationX |
                                                                    GestureSettings.NavigationY |
                                                                    GestureSettings.NavigationZ);
            }
            navigationGestureRecognizer.StartCapturingGestures();

        }

        private void Update()
        {
            if (usingWiimote)// && !click_a)
            {
                Receive();
            }
            if (click_a)
            {
                // Manual implementation of a timeout--make sure a click isn't processed twice
                //if (waitCount < 10) { Debug.Log("No click: " + waitCount); click_a = false;  waitCount++; }
                //else
                //{
                click_a = false;
                waitCount = 0;
                SourceClickEventArgs args = new SourceClickEventArgs(this, 0, 1); // manual tap count of 1

                RaiseSourceClickedEvent(args);
                //}
            }
            else if (press_b)
            {
                InputSourceEventArgs args = new InputSourceEventArgs(this, 0);
                RaiseSourceDownEvent(args);
            }
            else if (release_b)
            {
                release_b = false;
                InputSourceEventArgs args = new InputSourceEventArgs(this, 0);
                RaiseSourceUpEvent(args);
            }
        }

        protected override void OnDestroy()
        {
            if (gestureRecognizer != null)
            {
                gestureRecognizer.StopCapturingGestures();
                gestureRecognizer.TappedEvent -= OnTappedEvent;

                gestureRecognizer.HoldStartedEvent -= OnHoldStartedEvent;
                gestureRecognizer.HoldCompletedEvent -= OnHoldCompletedEvent;
                gestureRecognizer.HoldCanceledEvent -= OnHoldCanceledEvent;

                gestureRecognizer.ManipulationStartedEvent -= OnManipulationStartedEvent;
                gestureRecognizer.ManipulationUpdatedEvent -= OnManipulationUpdatedEvent;
                gestureRecognizer.ManipulationCompletedEvent -= OnManipulationCompletedEvent;
                gestureRecognizer.ManipulationCanceledEvent -= OnManipulationCanceledEvent;

                gestureRecognizer.Dispose();
            }

            base.OnDestroy();
        }

        private void OnTappedEvent(InteractionSourceKind source, int tapCount, Ray headRay)
        {
            SourceClickEventArgs args = new SourceClickEventArgs(this, 0, tapCount);
            //args.InputSource.isRegistered = true;
            RaiseSourceClickedEvent(args);
        }
        private void OnHoldStartedEvent(InteractionSourceKind source, Ray headray)
        {
            HoldEventArgs args = new HoldEventArgs(this, 0);
            RaiseHoldStartedEvent(args);
        }

        private void OnHoldCanceledEvent(InteractionSourceKind source, Ray headray)
        {
            HoldEventArgs args = new HoldEventArgs(this, 0);
            RaiseHoldCanceledEvent(args);
        }

        private void OnHoldCompletedEvent(InteractionSourceKind source, Ray headray)
        {
            HoldEventArgs args = new HoldEventArgs(this, 0);
            RaiseHoldCompletedEvent(args);
        }

        private void OnManipulationStartedEvent(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headray)
        {
            ManipulationEventArgs args = new ManipulationEventArgs(this, 0, cumulativeDelta);
            RaiseManipulationStartedEvent(args);
        }

        private void OnManipulationUpdatedEvent(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headray)
        {
            ManipulationEventArgs args = new ManipulationEventArgs(this, 0, cumulativeDelta);
            RaiseManipulationUpdatedEvent(args);
        }

        private void OnManipulationCompletedEvent(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headray)
        {
            ManipulationEventArgs args = new ManipulationEventArgs(this, 0, cumulativeDelta);
            RaiseManipulationCompletedEvent(args);
        }

        private void OnManipulationCanceledEvent(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headray)
        {
            ManipulationEventArgs args = new ManipulationEventArgs(this, 0, cumulativeDelta);
            RaiseManipulationCanceledEvent(args);
        }

        private void OnNavigationStartedEvent(InteractionSourceKind source, Vector3 normalizedOffset, Ray headray)
        {
            NavigationEventArgs args = new NavigationEventArgs(this, 0, normalizedOffset);
            RaiseNavigationStartedEvent(args);
        }

        private void OnNavigationUpdatedEvent(InteractionSourceKind source, Vector3 normalizedOffset, Ray headray)
        {
            NavigationEventArgs args = new NavigationEventArgs(this, 0, normalizedOffset);
            RaiseNavigationUpdatedEvent(args);
        }

        private void OnNavigationCompletedEvent(InteractionSourceKind source, Vector3 normalizedOffset, Ray headray)
        {
            NavigationEventArgs args = new NavigationEventArgs(this, 0, normalizedOffset);
            RaiseNavigationCompletedEvent(args);
        }

        private void OnNavigationCanceledEvent(InteractionSourceKind source, Vector3 normalizedOffset, Ray headray)
        {
            NavigationEventArgs args = new NavigationEventArgs(this, 0, normalizedOffset);
            RaiseNavigationCanceledEvent(args);
        }

        public override bool TryGetPosition(uint sourceId, out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }

        public override bool TryGetOrientation(uint sourceId, out Quaternion orientation)
        {
            orientation = Quaternion.identity;
            return false;
        }

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            return SupportedInputInfo.None;
        }
        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public void Receive()
        {
            readyToReceive = false;
            string response = "Operation Timeout";
            // We are receiving over an established socket connection
            if (_socket != null)
            {
                // Create SocketAsynscEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;

                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);

                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make 
                // this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        // Retrieve the data from the buffer
                        response = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        response = response.Replace("\0", "");
                        try
                        {
                            if (response.Contains("A") && !click_a) // "A" button clicked
                            {
                                click_a = true;
                            }
                            else if (response.Contains("B") && !response.Contains("NB") && !press_b) // Begin "B" button hold
                            {
                                release_b = false;
                                press_b = true;
                            }
                            else if (response.Contains("NB") && press_b)
                            {
                                press_b = false;
                                release_b = true;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Debug.Log("NO BUTTON DATA");
                        }
                    }
                    else
                    {
                        response = e.SocketError.ToString();
                    }
                    _clientDone.Set();
                });

                Send("ACK");  // Unblock the server mutex

                readyToReceive = true;

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();

                // Make an asynchronous Receive request over the socket
                _socket.ReceiveAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
        }
        /// <summary>
        /// Attempt a TCP socket connection to the given host over the given port
        /// </summary>
        /// <param name="hostName">The name of the host</param>
        /// <param name="portNumber">The port number to connect</param>
        /// <returns>A string representing the result of this connection attempt</returns>
        public string Connect(string hostName, int portNumber)
        {
            string result = string.Empty;

            // Create DnsEndPoint. The hostName and port are passed in to this method.
            IPAddress ipAddress = IPAddress.Parse(hostName);
            IPEndPoint hostEntry = new IPEndPoint(ipAddress, portNumber);
            // Create a stream-based, TCP socket using the InterNetwork Address Family. 
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Create a SocketAsyncEventArgs object to be used in the connection request
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;

            // Inline event handler for the Completed event.
            // Note: This event handler was implemented inline in order to make this method self-contained.
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
            {
                // Retrieve the result of this request
                result = e.SocketError.ToString();

                // Signal that the request is complete, unblocking the UI thread
                _clientDone.Set();

                readyToReceive = true;
                connected = true;
            });

            // Sets the state of the event to nonsignaled, causing threads to block
            _clientDone.Reset();

            // Make an asynchronous Connect request over the socket
            _socket.ConnectAsync(socketEventArg);

            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
            // If no response comes back within this time then proceed
            _clientDone.WaitOne(TIMEOUT_MILLISECONDS);

            return result;
        }
        /// <summary>
        /// Send the given data to the server using the established connection
        /// </summary>
        /// <param name="data">The data to send to the server</param>
        /// <returns>The result of the Send request</returns>
        public string Send(string data)
        {
            string response = "Operation Timeout";

            // We are re-using the _socket object initialized in the Connect method
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order 
                // to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();

                    // Unblock the UI thread
                    _clientDone.Set();
                });

                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();

                // Make an asynchronous Send request over the socket
                _socket.SendAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed

                _clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                response = "Socket is not initialized";
            }

            return response;
        }
    }
}
