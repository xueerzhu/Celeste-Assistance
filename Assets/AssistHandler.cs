using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditorInternal;

public class AssistHandler : MonoBehaviour
{
    public GameObject objective;
    public Camera mainCam;
    public GameObject visual;
    public SearchandReplay search;
    public GameObject radius;

    private Image image;
    private bool assistOn;
    private Color c = new Color();
    
    private bool objectivePlaced;
    private bool placingObjective;

    private void Start()
    {
        image = gameObject.GetComponent<Image>();
        c = mainCam.backgroundColor;
    }
    
    /// <summary>
    /// On assist button click
    /// </summary>
    public void ToggleAssist()
    {
        assistOn = !assistOn;  // mode
        image.color = assistOn ? Color.green : Color.white;  // button color
        
        // UI change
        mainCam.backgroundColor = assistOn ? Color.black : c;
        visual.SetActive(assistOn ? false : true);
        
    }

    private void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = -1;
        
        if (assistOn && !objectivePlaced)
        {
            Debug.Log("assist on ");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray,Mathf.Infinity);
            
            if (hit.collider != null && hit.collider.transform == objective.transform)
            {

                placingObjective = true;
                
            }

            if (placingObjective)
            {
                //Debug.Log("mouse pos is " + mousePos);
                radius.SetActive(true);
                objective.transform.position = mousePos;
                if (Input.GetButtonDown("Fire1"))
                {
                    Debug.Log("clicked");
                    placingObjective = false;
                    objectivePlaced = true;
                }
            }
        }

        if (assistOn && objectivePlaced)
        {
            radius.SetActive(false);
            search.PrepareSearch();
            search.PrepareObjective();
        }

        if (!assistOn)
        {
            objectivePlaced = false;
            placingObjective = false;
        }

        if (!placingObjective)
        {
            radius.SetActive(false);
        }
        
        
    }
}
