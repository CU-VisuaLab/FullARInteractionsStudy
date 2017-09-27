using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;

namespace Cam
{
    public class TaskBehaviorVoice : MonoBehaviour
    {
        public bool logging;

        // Variables for dragging the viewport
        public Image viewport;
        public Image fullImage;
        public Image zoomedImage;
        public Texture2D fullImageTexture;
        private float dragSpeed = 0.003f;
        private Vector2 translateOffset;

        private DirectionEnum dragDirection;

        // Variables for rotating the zoomed image
        private DirectionEnum rotateDirection;
        private float rotationSpeed = 0.75f;

        // Variables for selection
        public GameObject camera;

        public Image startStopButton;

        public enum DirectionEnum
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        // Use this for initialization
        void Start()
        {
            if (logging)
            {
                if (fullImage.transform.position.z < 3.0f)
                {
                    Debug.Log(">Camera_Near_Voice");
                }
                else if (fullImage.transform.position.z < 4.0f)
                {
                    Debug.Log(">Camera_Med_Voice");
                }
                else if (fullImage.transform.position.z < 5.0f)
                {
                    Debug.Log(">Camera_Far_Voice");
                }
            }

            dragDirection = DirectionEnum.None;
            rotateDirection = DirectionEnum.None;

            fullImage.gameObject.SetActive(false);
            zoomedImage.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false);

            CropZoomedImage();
        }
        // Update is called once per frame
        void Update()
        {
            if (dragDirection != DirectionEnum.None)
            {
                UpdateDragging();
                CropZoomedImage();
            }
            else if (rotateDirection != DirectionEnum.None) UpdateRotation();
        }

        public void StartRotation(String direction)
        {
            GazeManager gm = GazeManager.Instance;
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == zoomedImage.name)
            {
                switch (direction)
                {
                    case "Left":
                        rotateDirection = DirectionEnum.Left;
                        if (logging)
                        {
                            Debug.Log("Rotate: Start Left at " + Time.time);
                        }
                        break;
                    case "Right":
                        rotateDirection = DirectionEnum.Right;
                        if (logging)
                        {
                            Debug.Log("Rotate: Start Right at " + Time.time);
                        }
                        break;
                }
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Rotate: Start Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Rotate: Start Missed Target: null object " + Time.time);
            }
        }
        public void StopRotation()
        {
            GazeManager gm = GazeManager.Instance;
            if (gm.HitObject != null && gm.HitObject.name == zoomedImage.name)
            {
                rotateDirection = DirectionEnum.None;
                if (logging)
                {
                    Debug.Log("Rotate: Finish at " + Time.time);
                    Debug.Log("Rotate: Object angle " + zoomedImage.gameObject.transform.eulerAngles.z + " at " + Time.time);
                }
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Rotate: Finish Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Rotate: Finish Missed Target: null object " + Time.time);
            }
        }
        public void StartDragging(String direction)
        {
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == viewport.name)
            {
                switch (direction)
                {
                    case "Left":
                        dragDirection = DirectionEnum.Left;
                        translateOffset = new Vector2(-dragSpeed, 0.0f);
                        break;
                    case "Right":
                        dragDirection = DirectionEnum.Right;
                        translateOffset = new Vector2(dragSpeed, 0.0f);
                        break;
                    case "Up":
                        dragDirection = DirectionEnum.Up;
                        translateOffset = new Vector2(0.0f, dragSpeed);
                        break;
                    case "Down":
                        dragDirection = DirectionEnum.Down;
                        translateOffset = new Vector2(0.0f, -dragSpeed);
                        break;
                }
                InputManager.Instance.PushModalInputHandler(viewport.gameObject);

                if (logging)
                {
                    Debug.Log("Translate: Start moving " + direction + " at " + Time.time);
                    Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                              viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
                }
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Translate: Start Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Translate: Start Missed Target: null object " + Time.time);
            }
        }
        public void StopDragging()
        {
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == viewport.name)
            {
                if (dragDirection == DirectionEnum.None)
                {
                    return;
                }

                if (logging)
                {
                    Debug.Log("Translate: Finish at " + Time.time);
                    Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                              viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
                }

                // Remove self as a modal input handler
                InputManager.Instance.PopModalInputHandler();

                dragDirection = DirectionEnum.None;
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Translate: Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Translate Missed Target: null object " + Time.time);
            }
        }
        private void UpdateDragging()
        {
            RectTransform viewportBounds = viewport.gameObject.GetComponent<RectTransform>();

            float imageWidth = fullImage.rectTransform.rect.width * fullImage.rectTransform.localScale.x;
            float viewportWidth = viewportBounds.rect.width * viewportBounds.localScale.x;

            float imageHeight = fullImage.rectTransform.rect.height * fullImage.rectTransform.localScale.y;
            float viewportHeight = viewportBounds.rect.height * viewportBounds.localScale.y;

            float newX = viewport.gameObject.transform.position.x;
            float newY = viewport.gameObject.transform.position.y;

            if ((dragDirection == DirectionEnum.Left && newX + translateOffset.x > (viewportWidth - imageWidth) / 2) ||
                (dragDirection == DirectionEnum.Right && newX + translateOffset.x < (imageWidth - viewportWidth) / 2) ||
                (dragDirection == DirectionEnum.Down && newY + translateOffset.y > (viewportHeight - imageHeight) / 2) ||
                (dragDirection == DirectionEnum.Up && newY + translateOffset.y < (imageHeight - viewportHeight) / 2))
            {
                newX += translateOffset.x; newY += translateOffset.y;
            }
            Vector3 newPos = new Vector3(newX, newY, viewport.gameObject.transform.position.z);

            viewport.gameObject.transform.position = newPos;
            if (logging)
            {
                Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                              viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
            }
        }

        void UpdateRotation()
        {
            if (rotateDirection == DirectionEnum.Left)
            {
                zoomedImage.gameObject.transform.eulerAngles = new Vector3(zoomedImage.gameObject.transform.eulerAngles.x,
                                                        zoomedImage.gameObject.transform.eulerAngles.y,
                                                        zoomedImage.gameObject.transform.eulerAngles.z + rotationSpeed);
            }
            else if (rotateDirection == DirectionEnum.Right)
            {
                zoomedImage.gameObject.transform.eulerAngles = new Vector3(zoomedImage.gameObject.transform.eulerAngles.x,
                                                        zoomedImage.gameObject.transform.eulerAngles.y,
                                                        zoomedImage.gameObject.transform.eulerAngles.z - rotationSpeed);
            }
            if (logging)
            {
                Debug.Log("Rotate: Object angle " + zoomedImage.gameObject.transform.eulerAngles.z + " at " + Time.time);
            }
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

        public void SelectObject()
        {
            GazeManager gm = GazeManager.Instance;
            if (!fullImage.gameObject.activeSelf)
            {
                if (gm.HitObject == null && logging) Debug.Log("Select: Missed Target: null object " + Time.time);
                else if (gm.HitObject.name != camera.name) Debug.Log("Select: Missed Target: " + gm.HitObject.name);
                else
                {
                    fullImage.gameObject.SetActive(true);
                    if (logging) Debug.Log("Select: Clicked camera at " + Time.time);
                }
            }
            else if (!zoomedImage.gameObject.activeSelf)
            {
                if (gm.HitObject == null && logging) Debug.Log("Select: Missed Target: null object " + Time.time);
                else if (gm.HitObject.name != fullImage.gameObject.name) Debug.Log("Select: Missed Target: " + gm.HitObject.name);
                else
                {
                    zoomedImage.gameObject.SetActive(true);
                    viewport.gameObject.SetActive(true);
                    if (logging) Debug.Log("Select: Clicked full image at " + Time.time);
                }
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
            SceneManager.LoadScene("CamWiiNear");
        }

        public void loadScene(String sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}