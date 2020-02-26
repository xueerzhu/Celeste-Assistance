using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhysicsSceneManager : MonoBehaviour
{

    public GameObject mainBall;
    public GameObject slopesToSpawn;
    public GameObject trailToSpawn;

    private Scene mainScene;
    private Scene physicsScene;

    private GameObject go;

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

        go =  GameObject.Instantiate(mainBall, mainBall.transform.position, Quaternion.identity);
        go.transform.name = "ReferencePlayer";
        go.GetComponent<Rigidbody2D>().gravityScale = 1;

        go.GetComponent<Collision>().enabled = true;
        go.GetComponent<Movement>().enabled = true;
        go.GetComponent<BetterJumping>().enabled = true;

        //Destroy(go.GetComponent<MeshRenderer>());
        //Destroy(go.GetComponentInChildren<SpriteRenderer>());

        GameObject slopes =  GameObject.Instantiate(slopesToSpawn, slopesToSpawn.transform.position, Quaternion.identity);
        slopes.transform.name = "ReferenceLevel";
    }

    int counter = 0;
    void FixedUpdate(){
            counter++;
            if (counter % 2 == 0)
            {
                GameObject g =  GameObject.Instantiate(trailToSpawn, go.transform.position, Quaternion.identity);
            }
            physicsScene.GetPhysicsScene2D().Simulate(Time.deltaTime * 2);
    }
}
