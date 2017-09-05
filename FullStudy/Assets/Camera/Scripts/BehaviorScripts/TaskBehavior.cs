using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;

namespace Cam
{
    public class TaskBehavior : MonoBehaviour, IInputHandler, IFocusable, ISourceStateHandler
    {
        public bool logging;

        // Variables for dragging the viewport
        public Image viewport;
        public Image fullImage;
        public Image zoomedImage;
        public Texture2D fullImageTexture;

        private bool _isHolding;
        private float prevHandPosX;
        private float prevHandPosY;
        private float dragSpeed = 1.5f;

        private IInputSource currentInputSource = null;
        private uint currentInputSourceId;

        // Variables for rotating the zoomed image
        private Vector3 handAPos;
        private Vector3 handBPos;
        private IInputSource handAInputSource;
        private IInputSource handBInputSource;
        private uint handASourceId;
        private uint handBSourceId;
        private bool handAHolding = false;
        private bool handBHolding = false;

        public Image startStopButton;

        // Use this for initialization
        void Start()
        {
            if (logging)
            {
                if (fullImage.transform.position.z < 3.0f)
                {
                    Debug.Log(">Camera_Near_Gaze");
                }
                else if (fullImage.transform.position.z < 4.0f)
                {
                    Debug.Log(">Camera_Med_Gaze");
                }
                else if (fullImage.transform.position.z < 5.0f)
                {
                    Debug.Log(">Camera_Far_Gaze");
                }
            }
            _isHolding = false;
            CropZoomedImage();
        }
        // Update is called once per frame
        void Update()
        {
            if (_isHolding)
            {
                UpdateDragging();
                CropZoomedImage();
            }
            else if (handAHolding && handBHolding) UpdateRotation();
        }

        public void OnFocusEnter()
        {
        }

        public void OnFocusExit()
        {
        }

        public void OnInputDown(InputEventData eventData)
        {
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == viewport.name)
            {
                if (_isHolding)
                {
                    // We're already handling drag input, so we can't start a new rotate operation
                    return;
                }

                if (!eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.Position))
                {
                    // The input source must provide positional data for this script to be usable
                    return;
                }

                if (logging) Debug.Log("Translate: Hand Down at " + Time.time);
                currentInputSource = eventData.InputSource;
                currentInputSourceId = eventData.SourceId;
                StartDragging();
            }
            else if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == zoomedImage.name)
            {
                if (handAHolding && handBHolding)
                {
                    if (logging) Debug.Log("Rotate: Hand Down Unrecognized at " + Time.time);
                    return;
                }
                InputManager.Instance.PushModalInputHandler(gameObject);
                if (!handAHolding)
                {
                    if (logging) Debug.Log("Rotate: Hand A down at " + Time.time);
                    eventData.InputSource.TryGetPosition(eventData.SourceId, out handAPos);
                    handAInputSource = eventData.InputSource;
                    handASourceId = eventData.SourceId;
                    handAHolding = true;
                }
                else if (!handBHolding)
                {
                    if (logging) Debug.Log("Rotate: Hand B down at " + Time.time);
                    eventData.InputSource.TryGetPosition(eventData.SourceId, out handBPos);
                    handBInputSource = eventData.InputSource;
                    handBSourceId = eventData.SourceId;
                    handBHolding = true;
                }
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Hand Down Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Hand Down Missed Target: null object " + Time.time);
            }
        }

        public void OnInputUp(InputEventData eventData)
        {
            if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            {
                if (logging) Debug.Log("Translate: Hand Up at " + Time.time);
                StopDragging();
            }
            else if (eventData.SourceId == handASourceId)
            {
                if (logging) Debug.Log("Rotate: Hand Up (A) at " + Time.time);
                handAHolding = false;
            }
            else if (eventData.SourceId == handBSourceId)
            {
                if (logging) Debug.Log("Rotate: Hand Up (B) at " + Time.time);
                handBHolding = false;
            }
            else if (logging) Debug.Log("Unrecognized Hand Up at " + Time.time);
        }

        private void StartDragging()
        {
            if (_isHolding) return;

            InputManager.Instance.PushModalInputHandler(viewport.gameObject);
            _isHolding = true;

            Vector3 gazeHitPosition = GazeManager.Instance.HitInfo.point;
            Vector3 handPosition;
            currentInputSource.TryGetPosition(currentInputSourceId, out handPosition);
            prevHandPosX = handPosition.x;
            prevHandPosY = handPosition.y;
            if (logging)
            {
                Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                          viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
                Debug.Log("Translate: Hand: " + handPosition.x + "," + handPosition.y + "," + handPosition.z + " at " + Time.time);
            }
        }
        private void UpdateDragging()
        {
            Vector3 newHandPosition;
            currentInputSource.TryGetPosition(currentInputSourceId, out newHandPosition);

            float newHandPosX = newHandPosition.x;
            float newHandPosY = newHandPosition.y;

            float changeInX = dragSpeed * (prevHandPosX - newHandPosX);
            float changeInY = dragSpeed * (prevHandPosY - newHandPosY);

            RectTransform viewportBounds = viewport.gameObject.GetComponent<RectTransform>();
            float newX = viewport.gameObject.transform.position.x;
            float newY = viewport.gameObject.transform.position.y;

            float imageWidth = fullImage.rectTransform.rect.width * fullImage.rectTransform.localScale.x;
            float viewportWidth = viewportBounds.rect.width * viewportBounds.localScale.x;
            if (newX - changeInX < (imageWidth - viewportWidth) / 2 &&
                newX - changeInX > (viewportWidth - imageWidth) / 2)
                newX -= changeInX;

            float imageHeight = fullImage.rectTransform.rect.height * fullImage.rectTransform.localScale.y;
            float viewportHeight = viewportBounds.rect.height * viewportBounds.localScale.y;
            if (newY - changeInY < (imageHeight - viewportHeight) / 2 &&
                newY - changeInY > (viewportHeight - imageHeight) / 2)
                newY -= changeInY;

            Vector3 newPos = new Vector3(newX, newY, viewport.gameObject.transform.position.z);
            if (logging)
            {
                Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                          viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
                Debug.Log("Translate: Hand: " + newHandPosition.x + "," + newHandPosition.y + "," + newHandPosition.z + " at " + Time.time);
            }
            viewport.gameObject.transform.position = newPos;

            prevHandPosX = newHandPosX;
            prevHandPosY = newHandPosY;
        }

        /*
         * Update such that the zoomed image matches the portion of the image inside the viewport
         */
        private void CropZoomedImage()
        {
            RectTransform viewportBounds = viewport.gameObject.GetComponent<RectTransform>();

            float imageWidth = fullImage.rectTransform.rect.width * fullImage.rectTransform.localScale.x;
            float imageHeight = fullImage.rectTransform.rect.height * fullImage.rectTransform.localScale.y;
            float viewportWidth = viewportBounds.rect.width * viewportBounds.localScale.x;
            float viewportHeight = viewportBounds.rect.height * viewportBounds.localScale.y;
            float xOffset = viewport.gameObject.transform.position.x / (imageWidth - viewportWidth) + 0.5f;
            float yOffset = viewport.gameObject.transform.position.y / (imageHeight - viewportHeight) + 0.5f;

            int x = (int)((fullImageTexture.width - viewportWidth / (viewportBounds.localScale.x)) * xOffset);
            int y = (int)((fullImageTexture.height - viewportHeight / (viewportBounds.localScale.y)) * yOffset);

            Color[] c = fullImageTexture.GetPixels(x, y, 250, 140);
            Texture2D zoomCroppedTexture = new Texture2D(250, 140);
            zoomCroppedTexture.SetPixels(0, 0, 250, 140, c);
            zoomCroppedTexture.Apply();

            Sprite sprite = Sprite.Create(zoomCroppedTexture, new Rect(0, 0, 250, 140), Vector2.zero);
            zoomedImage.sprite = sprite;
        }

        private void StopDragging()
        {
            if (!_isHolding)
            {
                return;
            }

            // Remove self as a modal input handler
            InputManager.Instance.PopModalInputHandler();

            _isHolding = false;
            currentInputSource = null;
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            {
                if (logging) Debug.Log("Translate: Hand Lost at " + Time.time);
                StopDragging();
            }
            else if (eventData.SourceId == handASourceId)
            {
                handAHolding = false;
                Debug.Log("Rotate: Lost Hand A at " + Time.time);
            }
            else if (eventData.SourceId == handBSourceId)
            {
                handBHolding = false;
                Debug.Log("Rotate: Lost Hand B at " + Time.time);
            }
            else if (logging) Debug.Log("Lost Hand Unrecognized at " + Time.time);
        }
        void UpdateRotation()
        {
            Vector3 newHandA, newHandB;
            handAInputSource.TryGetPosition(handASourceId, out newHandA);
            handBInputSource.TryGetPosition(handBSourceId, out newHandB);
            float newHandLineSlope = (newHandA.y - newHandB.y) / (newHandA.x - newHandB.x);
            float oldHandLineSlope = (handAPos.y - handBPos.y) / (handAPos.x - handBPos.x);
            float newRotation = (float)Math.Atan(newHandLineSlope);
            float oldRotation = (float)Math.Atan(oldHandLineSlope);
            float deltaRotation = 180 * (newRotation - oldRotation) / (float)Math.PI;
            if (Math.Abs(deltaRotation) < 50)
            {
                zoomedImage.gameObject.transform.eulerAngles = new Vector3(zoomedImage.gameObject.transform.eulerAngles.x,
                                                               zoomedImage.gameObject.transform.eulerAngles.y,
                                                               zoomedImage.gameObject.transform.eulerAngles.z + deltaRotation);
                if (logging)
                {
                    Debug.Log("Rotate: Object angle " + zoomedImage.gameObject.transform.eulerAngles.z + " at " + Time.time);
                    Debug.Log("Rotate: Hand A: " + newHandA.x + "," + newHandA.y + "," + newHandA.z + " at " + Time.time);
                    Debug.Log("Rotate: Hand B: " + newHandB.x + "," + newHandB.y + "," + newHandB.z + " at " + Time.time);
                }
            }
            handAPos = newHandA;
            handBPos = newHandB;
        }
        public void StartTask()
        {
            if (logging) Debug.Log("*Begin Task at " + Time.time);
            startStopButton.gameObject.GetComponent<Image>().color = new Color(0, 255, 0, 255);
        }
        public void StopTask()
        {
            Debug.Log("*Finish Task at " + Time.time);
            startStopButton.gameObject.GetComponent<Image>().color = new Color(255, 0, 0, 255);
        }
        public void loadScene(String sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}