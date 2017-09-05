using HoloToolkit.Unity.InputModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GazeTrainingTaskBehavior : MonoBehaviour, IInputClickHandler, IFocusable, IInputHandler, ISourceStateHandler
{
    public Material green;
    public Material blue;
    public Material red;
    private int colorCounter = 0;
    
    private bool handAHolding;
    private bool handBHolding;
    private Vector3 handAPos;
    private Vector3 handBPos;
    private IInputSource handAInputSource;
    private IInputSource handBInputSource;
    private uint handASourceId;
    private uint handBSourceId;

    private bool _isHolding;
    private float prevHandPosX;
    private float prevHandPosY;
    private float dragSpeed = 2.5f;

    private IInputSource currentInputSource = null;
    private uint currentInputSourceId;

    public GameObject cube;
    public GameObject sphere;

    public void OnFocusEnter()
    {
    }

    public void OnFocusExit()
    {
    }

    public void OnInputClicked(InputEventData eventData)
    {
        if (GazeManager.Instance.HitObject.name == cube.name)
        {
            colorCounter++;
            if (colorCounter % 3 == 2) cube.GetComponent<Renderer>().material = green;
            else if (colorCounter % 3 == 1) cube.GetComponent<Renderer>().material = red;
            else if (colorCounter % 3 == 0) cube.GetComponent<Renderer>().material = blue;
        }
    }

    public void OnInputDown(InputEventData eventData)
    {
<<<<<<< HEAD
        if (GazeManager.Instance.HitObject == null) return;
=======
        if (GazeManager.Instance.HitObject.name == null) return; // BREAK
>>>>>>> d165087ac176b1d0e470bb84aa21f926c84787c4
        if (GazeManager.Instance.HitObject.name == sphere.name)
        {
            if (_isHolding)
            {
                // We're already handling drag input, so we can't start a new rotate operation
                return;
            }
            
            currentInputSource = eventData.InputSource;
            currentInputSourceId = eventData.SourceId;
            StartDragging();
        }
        else if (GazeManager.Instance.HitObject.name == cube.name)
        {
            if (!handAHolding)
            {
                eventData.InputSource.TryGetPosition(eventData.SourceId, out handAPos);
                handAInputSource = eventData.InputSource;
                handASourceId = eventData.SourceId;
                handAHolding = true;
            }
            else if (!handBHolding)
            {
                eventData.InputSource.TryGetPosition(eventData.SourceId, out handBPos);
                handBInputSource = eventData.InputSource;
                handBSourceId = eventData.SourceId;
                handBHolding = true;
            }
        }

    }

    public void OnInputUp(InputEventData eventData)
    {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
        {
            StopDragging();
        }
        if (eventData.SourceId == handASourceId)
        {
            handAHolding = false;
        }
        else if (eventData.SourceId == handBSourceId)
        {
            handBHolding = false;
        }
    }

    public void OnSourceDetected(SourceStateEventData eventData)
    {
    }

    public void OnSourceLost(SourceStateEventData eventData)
    {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
        {
            StopDragging();
        }
        else if (eventData.SourceId == handASourceId)
        {
            handAHolding = false;
        }
        else if (eventData.SourceId == handBSourceId)
        {
            handBHolding = false;
        }
    }

    // Use this for initialization
    void Start()
    {
        colorCounter = 0;
        cube.GetComponent<Renderer>().material = blue;
        handAHolding = false;
        handBHolding = false;
        _isHolding = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isHolding) UpdateDragging();
        if (handAHolding && handBHolding) UpdateRotation();
    }

    private void StartDragging()
    {
        if (_isHolding) return;

        InputManager.Instance.PushModalInputHandler(sphere.gameObject);
        _isHolding = true;

        Vector3 gazeHitPosition = GazeManager.Instance.HitInfo.point;
        Vector3 handPosition;
        currentInputSource.TryGetPosition(currentInputSourceId, out handPosition);
        prevHandPosX = handPosition.x;
        prevHandPosY = handPosition.y;
    }

    void UpdateDragging()
    {
        Vector3 newHandPosition;
        currentInputSource.TryGetPosition(currentInputSourceId, out newHandPosition);

        float newHandPosX = newHandPosition.x;
        float newHandPosY = newHandPosition.y;

        float changeInX = dragSpeed * (prevHandPosX - newHandPosX);
        float changeInY = dragSpeed * (prevHandPosY - newHandPosY);

        float newX = sphere.gameObject.transform.position.x;
        float newY = sphere.gameObject.transform.position.y;

        newX -= changeInX;

        newY -= changeInY;

        Vector3 newPos = new Vector3(newX, newY, sphere.gameObject.transform.position.z);
       
        sphere.gameObject.transform.position = newPos;

        prevHandPosX = newHandPosX;
        prevHandPosY = newHandPosY;
    }

    private void StopDragging()
    {
        if (!_isHolding)
        {
            return;
        }
        
        _isHolding = false;
        currentInputSource = null;
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
        cube.gameObject.transform.eulerAngles = new Vector3(cube.gameObject.transform.eulerAngles.x,
                                                       cube.gameObject.transform.eulerAngles.y,
                                                       cube.gameObject.transform.eulerAngles.z + deltaRotation);

        handAPos = newHandA;
        handBPos = newHandB;
    }
}
