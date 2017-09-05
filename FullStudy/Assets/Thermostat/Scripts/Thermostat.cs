using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;

namespace Lighthouse
{
    public class Thermostat : MonoBehaviour, IFocusable, IInputClickHandler
    {
        public GameObject objectToShow;
        private bool show;
        public List<GameObject> objectsToHide;

        // Use this for initialization
        void Start()
        {
            show = false;
            objectToShow.SetActive(show);
            foreach (var obj in objectsToHide)
            {
                obj.SetActive(false);
            }
        }

        void Update()
        {
          
        }
        public void OnFocusEnter() {}

        public void OnFocusExit() {}

        public void OnInputClicked(InputEventData eventData)
        {
            show = !show;
            objectToShow.SetActive(show);
            // Hide all objects if hiding the radial menu
            if (!show)
            {
                foreach (var obj in objectsToHide)
                {
                    obj.SetActive(false);
                }
            }
        }
    }
}
