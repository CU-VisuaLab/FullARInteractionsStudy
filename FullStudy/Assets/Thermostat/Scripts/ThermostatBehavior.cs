using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

public class ThermostatBehavior : MonoBehaviour, IInputClickHandler
{
    public GameObject viewport;
    public GameObject calendar;

	// Use this for initialization
	void Start () {
        viewport.SetActive(false);
        calendar.SetActive(false);
	}

    public void OnInputClicked(InputEventData eventData)
    {
        viewport.SetActive(true);
        calendar.SetActive(true);
    }
}
