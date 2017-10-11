using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.SceneManagement;

namespace Thermostat
{
    public class TaskBehaviorWii : MonoBehaviour, IInputHandler, ISourceStateHandler, IInputClickHandler
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

        private bool _isHolding;
        private float prevWiiPosX;
        private float prevWiiPosY;

        // Starting and stopping tasks
        public Image startStopButton;

        private bool rotating = false;

        private bool thermostatMoved = false;

        // Use this for initialization
        void Start()
        {
            if (logging)
            {
                if (thermostat.transform.position.z < 3.0f)
                {
                    Debug.Log(">Thermostat_Near_Wii");
                }
                else if (thermostat.transform.position.z < 4.0f)
                {
                    Debug.Log(">Thermostat_Med_Wii");
                }
                else if (thermostat.transform.position.z < 5.0f)
                {
                    Debug.Log(">Thermostat_Far_Wii");
                }
            }

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
            if (rotating)
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
                else if (WiimoteGazeManager.Instance.HitObject.name == thermostat.name)
                {
                    if (logging) Debug.Log("Rotate: Hand Down at " + Time.time);
                    WiimoteInputManager.Instance.PushModalInputHandler(thermostat.gameObject);
                    rotating = true;
                }
                else if (logging) Debug.Log("Hand Down Missed Target: " + WiimoteGazeManager.Instance.HitObject.name + " at " + Time.time);
            }
            else if (logging)
            {
                Debug.Log("Hand Down Missed Target: null object " + Time.time);
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

        public void OnSourceDetected(SourceStateEventData eventData)
        {
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
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
                Debug.Log("Translate: cursor: " + newWiiPosition.x + "," + newWiiPosition.y + "," + newWiiPosition.z + " at " + Time.time);
            }
            viewport.gameObject.transform.position = newPos;

            prevWiiPosX = newWiiPosX;
            prevWiiPosY = newWiiPosY;
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
        void UpdateRotation()
        {
            if (Mathf.Abs(WiimoteGazeManager.Instance.deltaRotation) < 50)
            {
                if (logging) { Debug.Log("Rotate: New Rotation " + (thermostatArm.gameObject.transform.eulerAngles.z + WiimoteGazeManager.Instance.deltaRotation)); }
                armRotation += WiimoteGazeManager.Instance.deltaRotation;
            }
        }
        public void OnInputClicked(InputEventData eventData)
        {
            if (WiimoteGazeManager.Instance.HitObject != null)
            {
				if (WiimoteGazeManager.Instance.HitObject.name == thermostat.name) {
					Debug.Log ("Select: Selected Thermostat at " + Time.time);
					if (!thermostatMoved) {
						temperatureReading.transform.localPosition = new Vector3 (0.0f, 0.2f, 0.0f);
						thermostat.transform.position = new Vector3 (0.0835f, -0.0835f, thermostat.transform.position.z);
						moveThermostatArmTo (armRotation);
						thermostat.transform.eulerAngles = new Vector3 (0.0f, 180.0f, 0.0f);
						thermostatMoved = true;
					} else {
						calendar.gameObject.SetActive (true);
						viewport.gameObject.SetActive (true);
						promptViewport.gameObject.SetActive (true);
					}
				} 
				else 
				{
					Debug.Log ("Select: Missed Target " + WiimoteGazeManager.Instance.HitObject.name + " at " + Time.time);
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