using System;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

public class TaskBehaviorVoice : MonoBehaviour {

    public Material green;
    public Material blue;
    public Material red;

    public GameObject cube;
    public GameObject sphere;
    private float dragSpeed = 0.005f;
    private Vector2 translateOffset;

    private DirectionEnum dragDirection;
    
    private DirectionEnum rotateDirection;
    private float rotationSpeed = 2f;

    // Variables for selection
    public GameObject camera;

    private int colorCounter;

    public enum DirectionEnum
    {
        Up,
        Down,
        Left,
        Right,
        None
    }

    // Use this for initialization
    void Start () {
        dragDirection = DirectionEnum.None;
        rotateDirection = DirectionEnum.None;

        colorCounter = 0;
        cube.GetComponent<Renderer>().material = blue;
    }
    // Update is called once per frame
    void Update()
    {
        if (dragDirection != DirectionEnum.None)
        {
            UpdateDragging();
        }
        else if (rotateDirection != DirectionEnum.None) UpdateRotation();
    }
    
    public void StartRotation(String direction) {
        GazeManager gm = GazeManager.Instance;
        if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == cube.name)
            {
            switch (direction)
            {
                case "Left":
                    rotateDirection = DirectionEnum.Left;
                    break;
                case "Right":
                    rotateDirection = DirectionEnum.Right;
                    break;
            }
        }
    }
    public void StopRotation()
    {
        GazeManager gm = GazeManager.Instance;
        if (gm.HitObject != null && gm.HitObject.name == cube.name)
        {
            rotateDirection = DirectionEnum.None;   
        }
    }
    public void StartDragging(String direction)
    {
        if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == sphere.name)
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
        }
    }
    public void StopDragging()
    {
        if (GazeManager.Instance.HitObject != null && GazeManager.Instance.HitObject.name == sphere.name)
        {
            if (dragDirection == DirectionEnum.None)
            {
                return;
            }
            
            dragDirection = DirectionEnum.None;
        }
    }
    private void UpdateDragging()
    {
        float newX = sphere.transform.position.x;
        float newY = sphere.transform.position.y;
        
        newX += translateOffset.x;
        newY += translateOffset.y;
        Vector3 newPos = new Vector3(newX, newY, sphere.transform.position.z);

        sphere.transform.position = newPos;
    }

    void UpdateRotation()
    {
        if (rotateDirection == DirectionEnum.Left)
        {
            cube.transform.eulerAngles = new Vector3(cube.transform.eulerAngles.x,
                                                    cube.transform.eulerAngles.y,
                                                    cube.transform.eulerAngles.z + rotationSpeed);
        }
        else if (rotateDirection == DirectionEnum.Right)
        {
            cube.transform.eulerAngles = new Vector3(cube.transform.eulerAngles.x,
                                                    cube.transform.eulerAngles.y,
                                                    cube.transform.eulerAngles.z - rotationSpeed);
        }
    }
    
    public void SelectObject()
    {
        GazeManager gm = GazeManager.Instance;
        if (gm.HitObject == null || gm.HitObject.name != cube.name) return;
        else
        {
                colorCounter++;
                if (colorCounter % 3 == 2) cube.GetComponent<Renderer>().material = green;
                else if (colorCounter % 3 == 1) cube.GetComponent<Renderer>().material = red;
                else if (colorCounter % 3 == 0) cube.GetComponent<Renderer>().material = blue;
        }
    }
}
