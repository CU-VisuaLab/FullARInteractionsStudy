using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

public class TaskBehaviorWii : MonoBehaviour, IInputHandler, ISourceStateHandler, IInputClickHandler
{
    public Material green;
    public Material blue;
    public Material red;
    private int colorCounter = 0;

    public GameObject cube;
    public GameObject sphere;

    private bool _isHolding;
    private float prevWiiPosX;
    private float prevWiiPosY;

    // Variables for rotating the zoomed image
    private bool rotating = false;

    // Variables for selection
    public GameObject camera;

    // Use this for initialization
    void Start () {
        _isHolding = false;
        colorCounter = 0;
        cube.GetComponent<Renderer>().material = blue;
    }
    // Update is called once per frame
    void Update()
    {
        if (_isHolding)
        {
            UpdateDragging();
        }
        else if (rotating) UpdateRotation();
    }
    
    public void OnInputDown(InputEventData eventData)
    {
        if (WiimoteGazeManager.Instance.HitObject != null)
        {
            if (WiimoteGazeManager.Instance.HitObject.name == sphere.name)
            {
                if (_isHolding)
                {
                    // We're already handling drag input, so we can't start a new rotate operation
                    return;
                }
                StartDragging();
            }
            else if (WiimoteGazeManager.Instance.HitObject.name == cube.name)
            {
                WiimoteInputManager.Instance.PushModalInputHandler(cube.gameObject);
                rotating = true;
            }
        }
    }

    public void OnInputUp(InputEventData eventData)
    {
        if (_isHolding)
        {
            StopDragging();
        }
        else if (rotating)
        {
            WiimoteInputManager.Instance.PopModalInputHandler();
            rotating = false;
        }
    }

    private void StartDragging()
    {
        WiimoteInputManager.Instance.PushModalInputHandler(sphere.gameObject);
        _isHolding = true;

        Vector3 gazeHitPosition = WiimoteGazeManager.Instance.HitInfo.point;
        prevWiiPosX = gazeHitPosition.x;
        prevWiiPosY = gazeHitPosition.y;
    }
    private void UpdateDragging()
    {
        Vector3 newWiiPosition = WiimoteGazeManager.Instance.HitPosition;

        float newWiiPosX = newWiiPosition.x;
        float newWiiPosY = newWiiPosition.y;

        float changeInX = prevWiiPosX - newWiiPosX;
        float changeInY = prevWiiPosY - newWiiPosY;

        RectTransform viewportBounds = sphere.gameObject.GetComponent<RectTransform>();
        float newX = sphere.gameObject.transform.position.x;
        float newY = sphere.gameObject.transform.position.y;
        
        newX -= changeInX;
        newY -= changeInY;

        Vector3 newPos = new Vector3(newX, newY, sphere.transform.position.z);
        sphere.transform.position = newPos;

        prevWiiPosX = newWiiPosX;
        prevWiiPosY = newWiiPosY;
    }

    private void StopDragging()
    {
        if (!_isHolding)
        {
            return;
        }
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
        cube.transform.eulerAngles = new Vector3 (cube.gameObject.transform.eulerAngles.x,
                                                       cube.transform.eulerAngles.y,
                                                       cube.transform.eulerAngles.z - WiimoteGazeManager.Instance.deltaRotation);

    }

    public void OnInputClicked(InputEventData eventData)
    {
        if (WiimoteGazeManager.Instance.HitObject != null && WiimoteGazeManager.Instance.HitObject.name == cube.name)
        {
            colorCounter++;
            if (colorCounter % 3 == 2) cube.GetComponent<Renderer>().material = green;
            else if (colorCounter % 3 == 1) cube.GetComponent<Renderer>().material = red;
            else if (colorCounter % 3 == 0) cube.GetComponent<Renderer>().material = blue;
        }
    }
}
