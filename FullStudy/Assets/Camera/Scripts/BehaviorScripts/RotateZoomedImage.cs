using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using UnityEngine.UI;

public class RotateZoomedImage : MonoBehaviour, IInputHandler, IFocusable, ISourceStateHandler
{
    private bool _isHolding;

    private float dragSpeed = 1.5f;

    private IInputSource currentLeftHandSource = null;
    private IInputSource currentRightHandSource = null;
    private int leftHandSourceId = -1;
    private int rightHandSourceId = -1;

    private Vector3 handAPos;
    private Vector3 handBPos;
    private IInputSource handAInputSource;
    private IInputSource handBInputSource;
    private uint handASourceId;
    private uint handBSourceId;
    private bool handAHolding = false;
    private bool handBHolding = false;

    // Use this for initialization
    void Start () {
        _isHolding = false;
    }
	
	// Update is called once per frame
	void Update () {
        if (handAHolding && handBHolding) UpdateRotation();
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
        Debug.Log(deltaRotation);
        gameObject.transform.eulerAngles = new Vector3(gameObject.transform.eulerAngles.x, 
                                                       gameObject.transform.eulerAngles.y,
                                                       gameObject.transform.eulerAngles.z + deltaRotation);
        handAPos = newHandA;
        handBPos = newHandB;
    }

    public void OnFocusEnter()
    {
    }
    public void OnFocusExit()
    {
    }

    public void OnInputDown(InputEventData eventData)
    { 
        if (handAHolding && handBHolding)
        {
            Debug.Log("Unrecognized Hand Down");
            return;
        }
        InputManager.Instance.PushModalInputHandler(gameObject);
        if (!handAHolding)
        {
            Debug.Log("Hand A down");
            eventData.InputSource.TryGetPosition(eventData.SourceId, out handAPos);
            handAInputSource = eventData.InputSource;
            handASourceId = eventData.SourceId;
            handAHolding = true;
        }
        else if (!handBHolding)
        {
            Debug.Log("Hand B down");
            eventData.InputSource.TryGetPosition(eventData.SourceId, out handBPos);
            handBInputSource = eventData.InputSource;
            handBSourceId = eventData.SourceId;
            handBHolding = true;
        }
    }

    public void OnInputUp(InputEventData eventData)
    {
        if (eventData.SourceId == handASourceId) handAHolding = false;
        else if (eventData.SourceId == handBSourceId) handBHolding = false;
        else Debug.Log("Unrecognized hand up....");
    }

    public void OnSourceDetected(SourceStateEventData eventData)
    {
    }
    public void OnSourceLost(SourceStateEventData eventData)
    {
        if (eventData.SourceId == handASourceId)
        {
            handAHolding = false;
            Debug.Log("Lost Hand A");
        }
        else if (eventData.SourceId == handBSourceId)
        {
            handBHolding = false;
            Debug.Log("Lost Hand B");
        }
        else Debug.Log("Unrecognized lost hand");
    }
}
