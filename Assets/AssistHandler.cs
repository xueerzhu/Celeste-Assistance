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
    public Material objectivePlacingMat;
    public float radiusFloat = 5f;

    private Image image;
    private bool assistOn;
    private Color c = new Color();
    
    [Header("SerializedField")]
    [SerializeField]
    private bool objectivePlaced;  // controls obj following mouse, ray related code
    [SerializeField]
    private bool placingObjective; // placing obj down

    [SerializeField]
    private bool canPlaceObjective;
    [SerializeField]// valid according to radius check
    private bool searching;  // is ghost search
    
    private Material defaultObjectiveMat;


    private void Awake()
    {
        search.PrepareSearch();
    }

    private void Start()
    {
        image = gameObject.GetComponent<Image>();
        defaultObjectiveMat = objective.GetComponent<SpriteRenderer>().material;
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
        
        // button change
        GameObject myEventSystem = GameObject.Find("EventSystem");
        myEventSystem .GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(null);
        
    }

    private void Update()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = -1;
        
        if (assistOn && (!objectivePlaced || searching))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray,Mathf.Infinity);
            
            if (Input.GetButtonDown("Fire1") && hit.collider != null && hit.collider.transform == objective.transform && !placingObjective)  // click on obj to star placing
            {

                placingObjective = true;
                
            }
            else
            {
                // placing objective
                // set radius active
                if (placingObjective)
                {
                    radius.SetActive(true);
                    float dis = Vector3.Distance(radius.transform.position, objective.transform.position);
                    if (dis > radiusFloat) // not in radius
                    {
                        objective.GetComponent<SpriteRenderer>().material = objectivePlacingMat;
                        canPlaceObjective = false;
                    }
                    else  // in radius
                    {
                        objective.GetComponent<SpriteRenderer>().material = defaultObjectiveMat;
                        canPlaceObjective = true;

                    }
                
                    objective.transform.position = mousePos;  // obj follow mouse
                
                    if (Input.GetButtonDown("Fire1") && canPlaceObjective)
                    {
                        placingObjective = false;
                        objectivePlaced = true;
                        searching = false;
                    }
                }
            }
            
        }
        
        
        if (assistOn && objectivePlaced && !searching)
        {
            Debug.Log("prepare called");
            radius.SetActive(false);
            
            // prepare new search
            search.clearSearch();
            search.PrepareObjectiveAndPlayer();
            
            if (search.searchisPrepared)
            {
                search.StartSearch();
                searching = true;
            }
            
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
