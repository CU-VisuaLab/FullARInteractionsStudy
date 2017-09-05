using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class CameraWiiBehavior : MonoBehaviour, IInputClickHandler
{
    public bool logging;
    public GameObject feedImage;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void OnInputClicked(InputEventData eventData)
    {
        feedImage.SetActive(true);
        if (logging) Debug.Log("Select: Clicked camera at " + Time.time);
    }
}
