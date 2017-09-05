using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;
namespace Cam {
    public class TaskBehaviorWii : MonoBehaviour, IInputHandler, ISourceStateHandler
    {
        public bool logging;

        // Variables for dragging the viewport
        public Image viewport;
        public Image fullImage;
        public Image zoomedImage;
        public Texture2D fullImageTexture;

        private bool _isHolding;
        private float prevWiiPosX;
        private float prevWiiPosY;

        // Variables for rotating the zoomed image
        private bool rotating = false;

        // Starting and stopping tasks
        private int colorCounter;
        public Image startStopButton;

        // Use this for initialization
        void Start() {
            if (logging)
            {
                if (fullImage.transform.position.z < 3.0f)
                {
                    Debug.Log(">Camera_Near_Wii");
                }
                else if (fullImage.transform.position.z < 4.0f)
                {
                    Debug.Log(">Camera_Med_Wii");
                }
                else if (fullImage.transform.position.z < 5.0f)
                {
                    Debug.Log(">Camera_Far_Wii");
                }
            }

            fullImage.gameObject.SetActive(false);
            zoomedImage.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false);

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
            else if (rotating) UpdateRotation();
        }

        public void OnInputDown(InputEventData eventData)
        {
            if (WiimoteGazeManager.Instance.HitObject != null)
            {
                if (WiimoteGazeManager.Instance.HitObject.name == viewport.name)
                {
                    if (_isHolding)
                    {
                        // We're already handling drag input, so we can't start a new rotate operation
                        return;
                    }

                    if (logging) Debug.Log("Translate: Hand Down at " + Time.time);
                    StartDragging();
                }
                else if (WiimoteGazeManager.Instance.HitObject.name == zoomedImage.name)
                {
                    if (logging) Debug.Log("Rotate: Hand Down at " + Time.time);
                    WiimoteInputManager.Instance.PushModalInputHandler(zoomedImage.gameObject);
                    rotating = true;
                }
            }
            else if (logging)
            {
                if (WiimoteGazeManager.Instance.HitObject != null) Debug.Log("Hand Down Missed Target: " + WiimoteGazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Hand Down Missed Target: null object " + Time.time);
            }
        }

        public void OnInputUp(InputEventData eventData)
        {
            if (_isHolding)
            {
                if (logging) Debug.Log("Translate: Hand Up at " + Time.time);
                StopDragging();
            }
            else if (rotating)
            {
                if (logging) Debug.Log("Rotate: Hand Up at " + Time.time);
                WiimoteInputManager.Instance.PopModalInputHandler();
                rotating = false;
            }
            else if (logging) Debug.Log("Unrecognized Hand Up at " + Time.time);
        }

        private void StartDragging()
        {
            WiimoteInputManager.Instance.PushModalInputHandler(viewport.gameObject);
            _isHolding = true;

            Vector3 gazeHitPosition = WiimoteGazeManager.Instance.HitInfo.point;
            prevWiiPosX = gazeHitPosition.x;
            prevWiiPosY = gazeHitPosition.y;
            if (logging)
            {
                Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                          viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
                Debug.Log("Translate: cursor: " + gazeHitPosition.x + "," + gazeHitPosition.y + "," + gazeHitPosition.z + " at " + Time.time);
            }
        }
        private void UpdateDragging()
        {
            Vector3 newWiiPosition = WiimoteGazeManager.Instance.HitPosition;

            float newWiiPosX = newWiiPosition.x;
            float newWiiPosY = newWiiPosition.y;

            float changeInX = prevWiiPosX - newWiiPosX;
            float changeInY = prevWiiPosY - newWiiPosY;

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
                Debug.Log("Translate: cursor: " + newWiiPosition.x + "," + newWiiPosition.y + "," + newWiiPosition.z + " at " + Time.time);
            }
            viewport.gameObject.transform.position = newPos;

            prevWiiPosX = newWiiPosX;
            prevWiiPosY = newWiiPosY;
        }

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
            WiimoteInputManager.Instance.PopModalInputHandler();

            _isHolding = false;
        }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
        }
        void UpdateRotation()
        {
            if (Mathf.Abs(WiimoteGazeManager.Instance.deltaRotation) < 50)
            {
                if (logging) { Debug.Log("Rotate: New Rotation " + (zoomedImage.gameObject.transform.eulerAngles.z - WiimoteGazeManager.Instance.deltaRotation)); }
                zoomedImage.gameObject.transform.eulerAngles = new Vector3(zoomedImage.gameObject.transform.eulerAngles.x,
                                                               zoomedImage.gameObject.transform.eulerAngles.y,
                                                               zoomedImage.gameObject.transform.eulerAngles.z - WiimoteGazeManager.Instance.deltaRotation);
            }
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
        public void LoadNext()
        {
            SceneManager.LoadScene("CamWiiMed");
        }

        public void loadScene(String sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}