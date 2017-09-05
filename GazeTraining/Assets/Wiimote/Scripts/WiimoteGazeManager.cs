// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Globalization;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// The gaze manager manages everything related to a gaze ray that can interact with other objects.
    /// </summary>
    public class WiimoteGazeManager : Singleton<WiimoteGazeManager>
    {
        public delegate void FocusedChangedDelegate(GameObject previousObject, GameObject newObject);

        /// <summary>
        /// Indicates whether the user is currently gazing at an object.
        /// </summary>
        public bool IsGazingAtObject { get; private set; }

        /// <summary>
        /// HitInfo property gives access to information at the object being gazed at, if any.
        /// </summary>
        public RaycastHit HitInfo { get { return hitInfo; } }
        private RaycastHit hitInfo;

        /// <summary>
        /// The game object that is currently being gazed at, if any.
        /// </summary>
        public GameObject HitObject { get; private set; }

        /// <summary>
        /// Position at which the gaze manager hit an object.
        /// If no object is currently being hit, this will use the last hit distance.
        /// </summary>
        public Vector3 HitPosition { get; private set; }

        /// <summary>
        /// Origin of the gaze.
        /// </summary>
        public Vector3 GazeOrigin { get; private set; }

        /// <summary>
        /// Normal of the gaze.
        /// </summary>
        public Vector3 GazeNormal { get; private set; }

        /// <summary>
        /// Maximum distance at which the gaze can collide with an object.
        /// </summary>
        public float MaxGazeCollisionDistance = 10.0f;

        /// <summary>
        /// The LayerMasks, in prioritized order, that are used to determine the HitObject when raycasting.
        ///
        /// Example Usage:
        ///
        /// // Allow the cursor to hit SR, but first prioritize any DefaultRaycastLayers (potentially behind SR)
        ///
        /// int sr = LayerMask.GetMask("SR");
        /// int nonSR = Physics.DefaultRaycastLayers & ~sr;
        /// GazeManager.Instance.RaycastLayerMasks = new LayerMask[] { nonSR, sr };
        /// </summary>
        [Tooltip("The LayerMasks, in prioritized order, that are used to determine the HitObject when raycasting.\n\nExample Usage:\n\n// Allow the cursor to hit SR, but first prioritize any DefaultRaycastLayers (potentially behind SR)\n\nint sr = LayerMask.GetMask(\"SR\");\nint nonSR = Physics.DefaultRaycastLayers & ~sr;\nGazeManager.Instance.RaycastLayerMasks = new LayerMask[] { nonSR, sr };")]
        public LayerMask[] RaycastLayerMasks = new LayerMask[] { Physics.DefaultRaycastLayers };

        /// <summary>
        /// Current stabilization method, used to smooth out the gaze ray data.
        /// If left null, no stabilization will be performed.
        /// </summary>
        [Tooltip("Stabilizer, if any, used to smooth out the gaze ray data.")]
        public BaseRayStabilizer Stabilizer = null;

        /// <summary>
        /// Transform that should be used as the source of the gaze position and orientation.
        /// Defaults to the main camera.
        /// </summary>
        [Tooltip("Transform that should be used to represent the gaze position and orientation. Defaults to Camera.Main")]
        public Transform GazeTransform;

        /// <summary>
        /// Dispatched when focus shifts to a new object, or focus on current object
        /// is lost.
        /// </summary>
        public event FocusedChangedDelegate FocusedObjectChanged;

        private float lastHitDistance = 2.0f;

        /// <summary>
        /// Unity UI pointer event.  This will be null if the EventSystem is not defined in the scene.
        /// </summary>
        public PointerEventData UnityUIPointerEvent { get; private set; }

        public bool usingWiimote = true;

        public float xOffset = 0.0f;
        public float yOffset = 0.0f;
        public float zOffset = 0.0f;

        private float ir_x = 0.0f;
        private float ir_y = 0.0f;

        private float oldRotation = 0.0f;
        public float deltaRotation = 0.0f;

        // Cached Socket object that will be used by each call for the lifetime of this class
        Socket _socket = null;

        // Signaling object used to notify when an asynchronous operation is completed
        static ManualResetEvent _clientDone = new ManualResetEvent(false);

        // Define a timeout in milliseconds for each asynchronous call. If a response is not received within this 
        // timeout period, the call is aborted.
        const int TIMEOUT_MILLISECONDS = 5000;

        // The maximum size of the data buffer to use with the asynchronous socket methods
        const int MAX_BUFFER_SIZE = 128;

        /// <summary>
        /// Cached results of racast results.
        /// </summary>
        private List<RaycastResult> raycastResultList = new List<RaycastResult>();

        // WiimoteGazeManager augmentation for logging
        public bool logging;
        public GameObject targetZPlaneObject;
        private float hitboxZPlane;

        protected override void Awake()
        {
            base.Awake();
            hitboxZPlane = targetZPlaneObject.transform.position.z;
            if (usingWiimote)
            {
<<<<<<< HEAD
                Connect("10.201.141.52", 4510);
=======
                Connect("10.202.103.227", 4510);
>>>>>>> d165087ac176b1d0e470bb84aa21f926c84787c4
                Send("I'm Alive");
            }


            // Add default RaycastLayers as first layerPriority
            if (RaycastLayerMasks == null || RaycastLayerMasks.Length == 0)
            {
                RaycastLayerMasks = new LayerMask[] { Physics.DefaultRaycastLayers };
            }

            if (GazeTransform == null)
            {
                if (Camera.main != null)
                {
                    GazeTransform = Camera.main.transform;
                }
                else
                {
                    Debug.LogError("Gaze Manager was not given a GazeTransform and no main camera exists to default to.");
                }
            }
        }

        private void Update()
        {
            if (GazeTransform == null)
            {
                return;
            }
            if (usingWiimote)
            {
                Receive();
            }
            UpdateGazeInfo();

            // Perform raycast to determine gazed object
            GameObject previousFocusObject = RaycastPhysics();

            // If we have a unity event system, perform graphics raycasts as well to support Unity UI interactions
            if (EventSystem.current != null)
            {
                // NOTE: We need to do this AFTER we set the HitPosition and HitObject since we need to use HitPosition to perform the correct 2D UI Raycast.
                RaycastUnityUI();
            }

            // Dispatch changed event if focus is different
            if (previousFocusObject != HitObject && FocusedObjectChanged != null)
            {
                FocusedObjectChanged(previousFocusObject, HitObject);
            }
            if (logging)
            {
                float a = (hitboxZPlane - GazeOrigin.z) / GazeNormal.z;
                if (HitObject != null)
                {
                    Vector3 WorldSpacePosition = GazeOrigin + (GazeNormal * a);
                    if (HitObject.GetComponent<Renderer>() != null)
                    {
                        float xOff = HitObject.GetComponent<Renderer>().bounds.center.x;
                        float yOff = HitObject.GetComponent<Renderer>().bounds.center.y;
                        float zOff = HitObject.GetComponent<Renderer>().bounds.center.z;
                        Debug.Log("Gaze: " + HitObject.name + HitObject.GetComponent<Renderer>().bounds.size + ": Offset from center (" +
                                  (HitPosition.x - xOff) + "," + (HitPosition.y - yOff) + "," + (HitPosition.z - zOff) +
                                  ") World space (" + WorldSpacePosition.x + "," + WorldSpacePosition.y + "," + WorldSpacePosition.z + ") at "
                                  + Time.time + " seconds");
                    }
                    else if (HitObject.GetComponent<RectTransform>() != null)
                    {
                        float xOff = HitObject.GetComponent<RectTransform>().rect.center.x;
                        float yOff = HitObject.GetComponent<RectTransform>().rect.center.y;
                        Debug.Log("Gaze: " + HitObject.name + HitObject.GetComponent<RectTransform>().rect.size * HitObject.GetComponent<RectTransform>().localScale.x +
                            ": Offset from center (" + (HitPosition.x - xOff) + ", " + (HitPosition.y - yOff) + ", " + hitboxZPlane +
                            ") World space (" + WorldSpacePosition.x + "," + WorldSpacePosition.y + "," + WorldSpacePosition.z + ") at " +
                            Time.time + " seconds");
                    }
                }
                else
                {
                    // Rather than HitPosition, figure out where the cursor intersects with the z-plane we calculated at the beginning
                    Vector3 NonHitPosition = GazeOrigin + (GazeNormal * a);
                    Debug.Log("Gaze: Whiff " + NonHitPosition.x + ", " + NonHitPosition.y + ", " + NonHitPosition.z + " at " + Time.time + " seconds");
                }
            }
        }

        /// <summary>
        /// Updates the current gaze information, so that the gaze origin and normal are accurate.
        /// </summary>
        private void UpdateGazeInfo()
        {

            Vector3 newGazeOrigin = GazeTransform.position;
            Vector3 newGazeNormal = GazeTransform.forward;

            if (!usingWiimote)
            {
                // Update gaze info from stabilizer
                if (Stabilizer != null)
                {
                    Stabilizer.UpdateStability(newGazeOrigin, GazeTransform.rotation);
                    newGazeOrigin = Stabilizer.StablePosition;
                    newGazeNormal = Stabilizer.StableRay.direction;
                }

                GazeOrigin = newGazeOrigin;
                GazeNormal = newGazeNormal;
            }
            else
            {
                Vector3 newWiiOrigin = GazeTransform.position;  // Would be great to get the wiimote's position
                Vector3 newWiiDirection = new Vector3(0.85f * (0.5f - ir_x), 0.85f * (0.5f - ir_y), 1.0f);

                GazeOrigin = newWiiOrigin;
                GazeNormal = newWiiDirection;
            }
        }

        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public void Receive()
        {
            string response = "Operation Timeout";
            // We are receiving over an established socket connection
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
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
                            // Parse the string for an x, y offset and rotation data
                            int firstOpenParenthesis = response.IndexOf('(');
                            int firstCommaAfterParenthesis = response.Substring(firstOpenParenthesis).IndexOf(',') + firstOpenParenthesis;
                            int firstClosedParenthesis = response.Substring(firstCommaAfterParenthesis).IndexOf(')') + firstCommaAfterParenthesis;
                            int firstSemicolon = response.Substring(firstClosedParenthesis).IndexOf(';') + firstClosedParenthesis;
                            int firstPound = response.Substring(firstSemicolon).IndexOf('#') + firstSemicolon;
                            string x_str = response.Substring(firstOpenParenthesis + 1, firstCommaAfterParenthesis - firstOpenParenthesis - 1);
                            string y_str = response.Substring(firstCommaAfterParenthesis + 1, firstClosedParenthesis - firstCommaAfterParenthesis - 1);
                            string rotation_str = response.Substring(firstSemicolon + 1, firstPound - firstSemicolon - 1);

                            float temp_x = float.Parse(x_str, CultureInfo.InvariantCulture);
                            float temp_y = float.Parse(y_str, CultureInfo.InvariantCulture);
                            if (temp_x < 0.999f) ir_x = float.Parse(x_str, CultureInfo.InvariantCulture);
                            if (temp_y < 1.332f) ir_y = float.Parse(y_str, CultureInfo.InvariantCulture);

                            if (!rotation_str.Contains("NaN"))
                            {
                                // Update existing change to rotation
                                float newRotation = float.Parse(rotation_str, CultureInfo.InvariantCulture);
                                deltaRotation = newRotation - oldRotation;
                                oldRotation = newRotation;
                            }
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Debug.Log("Lost IR Data");
                        }
                    }
                    else
                    {
                        response = "Error";// e.SocketError.ToString();
                    }
                    _clientDone.Set();
                });
                Send("ACK");  // Unblock the server mutex

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
        /// Perform a Unity physics Raycast to determine which scene objects with a collider is currently being gazed at, if any.
        /// </summary>
        private GameObject RaycastPhysics()
        {
            GameObject previousFocusObject = HitObject;

            // If there is only one priority, don't prioritize
            if (RaycastLayerMasks.Length == 1)
            {
                IsGazingAtObject = Physics.Raycast(GazeOrigin, GazeNormal, out hitInfo, MaxGazeCollisionDistance, RaycastLayerMasks[0]);
            }
            else
            {
                // Raycast across all layers and prioritize
                RaycastHit? hit = PrioritizeHits(Physics.RaycastAll(new Ray(GazeOrigin, GazeNormal), MaxGazeCollisionDistance, -1));

                IsGazingAtObject = hit.HasValue;
                if (IsGazingAtObject)
                {
                    hitInfo = hit.Value;
                }
            }

            if (IsGazingAtObject)
            {
                HitObject = HitInfo.collider.gameObject;
                HitPosition = HitInfo.point;
                lastHitDistance = HitInfo.distance;
            }
            else
            {
                HitObject = null;
                HitPosition = GazeOrigin + (GazeNormal * lastHitDistance);
            }
            return previousFocusObject;
        }

        /// <summary>
        /// Perform a Unity UI Raycast, compare with the latest 3D raycast, and overwrite the hit object info if the UI gets focus
        /// </summary>
        private void RaycastUnityUI()
        {
            if (UnityUIPointerEvent == null)
            {
                UnityUIPointerEvent = new PointerEventData(EventSystem.current);
            }

            // 2D cursor position
            Vector2 cursorScreenPos = Camera.main.WorldToScreenPoint(HitPosition);
            UnityUIPointerEvent.delta = cursorScreenPos - UnityUIPointerEvent.position;
            UnityUIPointerEvent.position = cursorScreenPos;

            // Graphics raycast
            raycastResultList.Clear();
            EventSystem.current.RaycastAll(UnityUIPointerEvent, raycastResultList);
            RaycastResult uiRaycastResult = FindFirstRaycastInLayermasks(raycastResultList, RaycastLayerMasks);
            UnityUIPointerEvent.pointerCurrentRaycast = uiRaycastResult;

            // If we have a raycast result, check if we need to overwrite the 3D raycast info
            if (uiRaycastResult.gameObject != null)
            {
                // Add the near clip distance since this is where the raycast is from
                float uiRaycastDistance = uiRaycastResult.distance + Camera.main.nearClipPlane;

                bool superseded3DObject = false;
                if (IsGazingAtObject)
                {
                    // Check layer prioritization
                    if (RaycastLayerMasks.Length > 1)
                    {
                        // Get the index in the prioritized layer masks
                        int uiLayerIndex = FindLayerListIndex(uiRaycastResult.gameObject.layer, RaycastLayerMasks);
                        int threeDLayerIndex = FindLayerListIndex(hitInfo.collider.gameObject.layer, RaycastLayerMasks);

                        if (threeDLayerIndex > uiLayerIndex)
                        {
                            superseded3DObject = true;
                        }
                        else if (threeDLayerIndex == uiLayerIndex)
                        {
                            if (hitInfo.distance > uiRaycastDistance)
                            {
                                superseded3DObject = true;
                            }
                        }
                    }
                    else
                    {
                        if (hitInfo.distance > uiRaycastDistance)
                        {
                            superseded3DObject = true;
                        }
                    }
                }

                // Check if we need to overwrite the 3D raycast info
                if (!IsGazingAtObject || superseded3DObject)
                {
                    IsGazingAtObject = true;
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(uiRaycastResult.screenPosition.x, uiRaycastResult.screenPosition.y, uiRaycastDistance));
                    hitInfo = new RaycastHit()
                    {
                        distance = uiRaycastDistance,
                        normal = -Camera.main.transform.forward,
                        point = worldPos
                    };

                    HitObject = uiRaycastResult.gameObject;
                    HitPosition = HitInfo.point;
                    lastHitDistance = HitInfo.distance;
                }
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
                    response = "Success";
                    //response = e.SocketError.ToString();

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

        /// <summary>
        /// Closes the Socket connection and releases all associated resources
        /// </summary>
        public void Close()
        {
            if (_socket != null)
            {
                //_socket.Close();
            }
        }
        #region Helpers

        /// <summary>
        /// Find the first (closest) raycast in the list of RaycastResults that is also included in the LayerMask list.  
        /// </summary>
        /// <param name="candidates">List of RaycastResults from a Unity UI raycast</param>
        /// <param name="layerMaskList">List of layers to support</param>
        /// <returns>RaycastResult if hit, or an empty RaycastResult if nothing was hit</returns>
        private RaycastResult FindFirstRaycastInLayermasks(List<RaycastResult> candidates, LayerMask[] layerMaskList)
        {
            int combinedLayerMask = 0;
            for (int i = 0; i < layerMaskList.Length; i++)
            {
                combinedLayerMask = combinedLayerMask | layerMaskList[i].value;
            }

            for (var i = 0; i < candidates.Count; ++i)
            {
                if (candidates[i].gameObject == null || !IsLayerInLayerMask(candidates[i].gameObject.layer, combinedLayerMask))
                {
                    continue;
                }

                return candidates[i];
            }

            return new RaycastResult();
        }

        /// <summary>
        /// Look through the layerMaskList and find the index in that list for which the supplied layer is part of
        /// </summary>
        /// <param name="layer">Layer to search for</param>
        /// <param name="layerMaskList">List of LayerMasks to search</param>
        /// <returns>LayerMaskList index, or -1 for not found</returns>
        private int FindLayerListIndex(int layer, LayerMask[] layerMaskList)
        {
            for (int i = 0; i < layerMaskList.Length; i++)
            {
                if (IsLayerInLayerMask(layer, layerMaskList[i].value))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsLayerInLayerMask(int layer, int layerMask)
        {
            return ((1 << layer) & layerMask) != 0;
        }

        private RaycastHit? PrioritizeHits(RaycastHit[] hits)
        {
            if (hits.Length == 0)
            {
                return null;
            }

            // Return the minimum distance hit within the first layer that has hits.
            // In other words, sort all hit objects first by layerMask, then by distance.
            for (int layerMaskIdx = 0; layerMaskIdx < RaycastLayerMasks.Length; layerMaskIdx++)
            {
                RaycastHit? minHit = null;

                for (int hitIdx = 0; hitIdx < hits.Length; hitIdx++)
                {
                    RaycastHit hit = hits[hitIdx];
                    if (IsLayerInLayerMask(hit.transform.gameObject.layer, RaycastLayerMasks[layerMaskIdx]) &&
                        (minHit == null || hit.distance < minHit.Value.distance))
                    {
                        minHit = hit;
                    }
                }

                if (minHit != null)
                {
                    return minHit;
                }
            }

            return null;
        }

        #endregion Helpers
    }
}