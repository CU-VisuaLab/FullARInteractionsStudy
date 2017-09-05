using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class FeedImageBehaviorWii : MonoBehaviour, IInputClickHandler
{
    public bool logging;
    public GameObject viewport;
    public GameObject zoomedImage;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnInputClicked(InputEventData eventData)
    {
        if (logging) Debug.Log("Select: Clicked feed image at " + Time.time);
        viewport.SetActive(true);
        zoomedImage.SetActive(true);
    }
}
