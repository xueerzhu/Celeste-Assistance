using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class PhysicsDemo : MonoBehaviour
{

    public GameObject objectsToSpawn;
    public GameObject slopesToSpawn;

    private Scene mainScene;
    private Scene physicsScene;

    private GameObject go;
    public GameObject originPos;

    private int similuationLenght = 100;

    private void Start() {
        Physics2D.autoSimulation = false;

        mainScene = SceneManager.GetActiveScene();
        physicsScene = SceneManager.CreateScene("physics-scene-sim", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();
    }

    public void PreparePhysicsScene()
    {
        SceneManager.SetActiveScene(physicsScene);

        go =  GameObject.Instantiate(objectsToSpawn, objectsToSpawn.transform.position, Quaternion.identity);
        go.transform.name = "ReferenceBall";

        // go.GetComponent<Collision>().enabled = true;
        // go.GetComponent<Movement>().enabled = true;
        // go.GetComponent<BetterJumping>().enabled = true;

        //Destroy(go.GetComponent<MeshRenderer>());
        
        // go.GetComponent<SpriteRenderer>().enabled = false;

        go.GetComponent<SpriteRenderer>().color = Color.red;

        GameObject slopes =  GameObject.Instantiate(slopesToSpawn, slopesToSpawn.transform.position, Quaternion.identity);
        slopes.transform.name = "ReferenceLevel";
    }

    public float simulationSpeed = 0;
    public int simulationSteps = 0;

    void FixedUpdate(){
        for(int i = 0; i < simulationSteps; i++)
            physicsScene.GetPhysicsScene2D().Simulate(Time.fixedDeltaTime); 
            
        if (go.GetComponent<Rigidbody2D>().velocity == Vector2.zero)
        {
            // go.GetComponent<SpriteRenderer>().enabled = true;
            go.GetComponent<SpriteRenderer>().color = Color.green;
        }
        
        if(Input.GetKeyDown(KeyCode.R))
        {
            go.transform.position = originPos.transform.position;
        }

    }

    
}

