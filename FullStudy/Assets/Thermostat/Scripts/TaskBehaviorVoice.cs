using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;
namespace Thermostat
{
    public class TaskBehaviorVoice : MonoBehaviour
    {
        public bool logging;

        public GameObject thermostat;
        public GameObject thermostatArm;
        public GameObject temperatureText;
        public Image viewport;
        public Image calendar;
        public Image promptViewport;
        public GameObject temperatureReading;

        private float armRotation;
        private float armHeight;

        private float dragSpeed = .003f;
        private Vector2 translateOffset;

        // Starting and stopping tasks
        public Image startStopButton;

        private DirectionEnum dragDirection;

        // Variables for rotating the zoomed image
        private DirectionEnum rotateDirection;
        private float rotationSpeed = 0.75f;

        private bool thermostatMoved = false;

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
                if (thermostat.transform.position.z < 3.0f)
                {
                    Debug.Log(">Thermostat_Near_Voice");
                }
                else if (thermostat.transform.position.z < 4.0f)
                {
                    Debug.Log(">Thermostat_Med_Voice");
                }
                else if (thermostat.transform.position.z < 5.0f)
                {
                    Debug.Log(">Thermostat_Far_Voice");
                }
            }

            dragDirection = DirectionEnum.None;
            rotateDirection = DirectionEnum.None;

            armRotation = -60f;
            adjustTemperature(armRotation);

            calendar.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false);
            promptViewport.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            if (!thermostatMoved) return;
            if (rotateDirection != DirectionEnum.None)
            {
                UpdateRotation();
                moveThermostatArmTo(armRotation);
                adjustTemperature(armRotation);
            }
            else if (dragDirection != DirectionEnum.None)
            {
                UpdateDragging();
            }
        }

        void moveThermostatArmTo(float angle)
        {
            armHeight = thermostatArm.transform.localScale.y;
            thermostatArm.transform.eulerAngles = new Vector3(0.0f, 0.0f, -armRotation);
            thermostatArm.transform.position = new Vector3(armHeight * Mathf.Sin(armRotation * Mathf.PI / 180f) / 2, armHeight * Mathf.Cos(armRotation * Mathf.PI / 180f) / 2, thermostat.transform.position.z - 0.025f);
        }

        void adjustTemperature(float angle)
        {
            int temperature = (int)Math.Round(angle / 10f) + 78;
            temperatureText.GetComponent<Text>().text = temperature.ToString();
        }
        public void StartRotation(String direction)
        {
            GazeManager gm = GazeManager.Instance;
            if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == thermostat.name)
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
                if (GazeManager.Instance.HitObject != null) Debug.Log("Rotate: Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Rotate: Missed Target: null object " + Time.time);
            }
        }

        public void StopRotation()
        {
            GazeManager gm = GazeManager.Instance;
            if (gm.HitObject != null && gm.HitObject.name == thermostat.name)
            {
                rotateDirection = DirectionEnum.None;
                if (logging)
                {
                    Debug.Log("Rotate: Finish at " + Time.time);
                    Debug.Log("Rotate: Object angle " + thermostatArm.transform.eulerAngles.z + " at " + Time.time);
                }
            }
            else if (logging)
            {
                if (GazeManager.Instance.HitObject != null) Debug.Log("Rotate: Finish Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Rotate: Finish Missed Target: null object " + Time.time);
            }
        }

        void UpdateRotation()
        {
            if (rotateDirection == DirectionEnum.Left)
            {
                armRotation -= rotationSpeed;
            }
            else if (rotateDirection == DirectionEnum.Right)
            {
                armRotation += rotationSpeed;
            }
            if (logging)
            {
                Debug.Log("Rotate: Object angle " + armRotation + " at " + Time.time);
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
                if (GazeManager.Instance.HitObject != null) Debug.Log("Translate: Finish Missed Target: " + GazeManager.Instance.HitObject.name + " at " + Time.time);
                else Debug.Log("Translate: Finish Missed Target: null object " + Time.time);
            }
        }
        private void UpdateDragging()
        {
            RectTransform viewportBounds = viewport.gameObject.GetComponent<RectTransform>();
            float newX = viewport.gameObject.transform.position.x;
            float newY = viewport.gameObject.transform.position.y;

            float imageWidth = (calendar.rectTransform.rect.width + 20f) * calendar.rectTransform.localScale.x;
            float viewportWidth = viewportBounds.rect.width * viewportBounds.localScale.x;

            if (newX + translateOffset.x < (imageWidth - viewportWidth) / 2 &&
                newX + translateOffset.x > (viewportWidth - imageWidth) / 2)
                newX += translateOffset.x;

            float imageHeight = (calendar.rectTransform.rect.height + 20f) * calendar.rectTransform.localScale.y;
            float viewportHeight = viewportBounds.rect.height * viewportBounds.localScale.y;
            if (newY + translateOffset.y - calendar.rectTransform.position.y < (imageHeight - viewportHeight) / 2 &&
                newY + translateOffset.y - calendar.rectTransform.position.y > (viewportHeight - imageHeight) / 2)
                newY += translateOffset.y;

            Vector3 newPos = new Vector3(newX, newY, viewport.gameObject.transform.position.z);

            viewport.gameObject.transform.position = newPos;
            if (logging)
            {
                Debug.Log("Translate: Object Position " + viewport.gameObject.transform.position.x + "," +
                              viewport.gameObject.transform.position.y + viewport.gameObject.transform.position.z + " at " + Time.time);
            }
        }

        public void SelectObject()
        {
            GazeManager gm = GazeManager.Instance;
            if (!thermostatMoved)
            {
                temperatureReading.transform.localPosition = new Vector3(0.0f, 0.2f, 0.0f);
                thermostat.transform.position = new Vector3(0.0835f, -0.0835f, thermostat.transform.position.z);
                moveThermostatArmTo(armRotation);
                thermostat.transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
                thermostatMoved = true;
            }
            else if (!calendar.gameObject.activeSelf)
            {
                if (gm.HitObject == null && logging) Debug.Log("Select: Missed Target: null object " + Time.time);
                else if (gm.HitObject.name != thermostat.name) Debug.Log("Select: Missed Target: " + gm.HitObject.name);
                else
                {
                    calendar.gameObject.SetActive(true);
                    viewport.gameObject.SetActive(true);
                    promptViewport.gameObject.SetActive(true);
                    if (logging) Debug.Log("Select: Clicked Thermostat at " + Time.time);
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
        public void loadScene(String sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}