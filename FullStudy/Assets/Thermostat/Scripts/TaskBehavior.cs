using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;
using UnityEngine.SceneManagement;
namespace Thermostat
{
    public class TaskBehavior : MonoBehaviour, IInputHandler, IFocusable, ISourceStateHandler, IInputClickHandler
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

        // Variables for translation
        private bool _isHolding;
        private float prevHandPosX;
        private float prevHandPosY;
        private float dragSpeed = 1.5f;

        private IInputSource currentInputSource = null;
        private uint currentInputSourceId;

        // Variables for rotating the thermostat
        private Vector3 handAPos;
        private Vector3 handBPos;
        private IInputSource handAInputSource;
        private IInputSource handBInputSource;
        private uint handASourceId;
        private uint handBSourceId;
        private bool handAHolding = false;
        private bool handBHolding = false;

        private bool thermostatMoved = false;

        // Starting and stopping tasks
        public Image startStopButton;

        // Use this for initialization
        void Start()
        {
            if (logging) Debug.Log(">Thermostat_Near_Hand");

            armRotation = -60f;
            adjustTemperature(armRotation);

            calendar.gameObject.SetActive(false);
            viewport.gameObject.SetActive(false);
            promptViewport.gameObject.SetActive(false);

            _isHolding = false;

        }

        // Update is called once per frame
        void Update()
        {
            if (!thermostatMoved) return;
            if (handAHolding && handBHolding)
            {
                UpdateRotation();
                moveThermostatArmTo(armRotation);
                adjustTemperature(armRotation);
            }
            else if (_isHolding)
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
            if (float.IsNaN(deltaRotation)) return;
            armRotation -= deltaRotation;

            if (logging)
            {
                Debug.Log("Rotate: Object angle " + thermostatArm.transform.eulerAngles.z + " at " + Time.time);
                Debug.Log("Rotate: Hand A: " + newHandA.x + "," + newHandA.y + "," + newHandA.z + " at " + Time.time);
                Debug.Log("Rotate: Hand B: " + newHandB.x + "," + newHandB.y + "," + newHandB.z + " at " + Time.time);
            }
            handAPos = newHandA;
            handBPos = newHandB;
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

            float imageWidth = (calendar.rectTransform.rect.width + 20f) * calendar.rectTransform.localScale.x;
            float viewportWidth = viewportBounds.rect.width * viewportBounds.localScale.x;
            if (newX - changeInX < (imageWidth - viewportWidth) / 2 &&
                newX - changeInX > (viewportWidth - imageWidth) / 2)
                newX -= changeInX;

            float imageHeight = (calendar.rectTransform.rect.height + 20f) * calendar.rectTransform.localScale.y;
            float viewportHeight = viewportBounds.rect.height * viewportBounds.localScale.y;
            if (newY - changeInY - calendar.rectTransform.position.y < (imageHeight - viewportHeight) / 2 &&
                newY - changeInY - calendar.rectTransform.position.y > (viewportHeight - imageHeight) / 2)
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

        public void OnInputUp(InputEventData eventData)
        {
            if (eventData.SourceId == currentInputSourceId)
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
            else if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == thermostat.name)
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

        public void OnFocusEnter()
        {
        }

        public void OnFocusExit()
        {
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

        public void OnInputClicked(InputEventData eventData)
        {
            if (GazeManager.Instance.HitObject != null)
            {
                if (GazeManager.Instance.HitObject.name == thermostat.name)
                {
                    if (!thermostatMoved)
                    {
                        temperatureReading.transform.localPosition = new Vector3(0.0f, 0.2f, 0.0f);
                        thermostat.transform.position = new Vector3(0.0835f, -0.0835f, thermostat.transform.position.z);
                        moveThermostatArmTo(armRotation);
                        thermostat.transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
                        thermostatMoved = true;
                    }
                    else
                    {
                        calendar.gameObject.SetActive(true);
                        viewport.gameObject.SetActive(true);
                        promptViewport.gameObject.SetActive(true);
                    }
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