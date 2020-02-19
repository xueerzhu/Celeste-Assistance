using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CelesteDemo : MonoBehaviour
{

    [Header("Public References")]
    public GameObject mainPlayer;
    public GameObject levelToCopy;

    [Space]
    [Header("Parameters")]
    public int simulationSteps = 0;
    public float horizontalSpeed = 10;
    public Vector2 jumpVelocity;

    [Space]
    [Header("Bool")]
    public bool reachedSteadyState = false;
    public bool ballMoved = false; // ballMoved is the dirty flag to run physics scene whenever main scene ball pos changed
    public bool reachedGoal = false;

    /// PRIVATE
    private Scene mainScene;
    private Scene physicsScene;
    private GameObject go;
    private SpriteRenderer ballSpriteRenderer;
    private Rigidbody2D referenceBallRigidbody2D;
    private Rigidbody2D mainSceneBallRigidbody2D;
    private PhysicsScene2D activePhysicsScene2D;
    private bool isJumpPressed = false;
    private float horizontalInput;
    private Vector2 horizontalDir;

    private Movement simMovement;
    private Movement playerMovement;

    [Space]
    [Header("Replay System")]
    private List<Vector2> actions = new List<Vector2>();

    private void Start() {
        // leave it = true so physics si simulated normally in main scene
        //Physics2D.autoSimulation = false;

        mainScene = SceneManager.GetActiveScene();
        physicsScene = SceneManager.CreateScene("sim-physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();

        // cache main scene components
        mainSceneBallRigidbody2D = mainPlayer.GetComponent<Rigidbody2D>();

        // Set these when we have the simulation scene working
        // simMovement =
        // playerMovement =

    }

    public void PreparePhysicsScene()
    {
        // There must always be one Scene marked as the active Scene.
        SceneManager.SetActiveScene(physicsScene);

        go = GameObject.Instantiate(mainPlayer, mainPlayer.transform.position, Quaternion.identity);
        go.transform.name = "ReferenceBall";

        // cache physics scene references
        ballSpriteRenderer = go.transform.GetChild(0).GetComponent<SpriteRenderer>();
        referenceBallRigidbody2D = go.GetComponent<Rigidbody2D>();
        referenceBallRigidbody2D.constraints =  RigidbodyConstraints2D.None;
        activePhysicsScene2D = physicsScene.GetPhysicsScene2D();
        ballSpriteRenderer.color = Color.red;
        //ballSpriteRenderer.enabled = false;

        //GameObject slopes =  GameObject.Instantiate(levelToCopy, levelToCopy.transform.position, Quaternion.identity);
        //slopes.transform.name = "ReferenceLevel";
    }

    void FixedUpdate(){
        Debug.Log(reachedGoal);
        if (!reachedGoal)
        {

            for(int i = 0; i < simulationSteps; i++)
            {

                activePhysicsScene2D.Simulate(Time.fixedDeltaTime);
            }
        }
        else
        {
            Replay();
        }

        // if (horizontalInput > 0.1 || horizontalInput < -0.1)
        // {
        //     ballMoved = true;
        //     Walk(horizontalDir);
        // }
        //
        //
        // // if (isJumpPressed == true)
        // // {
        // //     Jump(referenceBallRigidbody2D);
        // // }
        //
        // // if ball pos changed, and current sim has stopped, resimulate physics
        // if (ballMoved == true && referenceBallRigidbody2D.velocity == Vector2.zero)
        // {
        //     go.transform.position = mainPlayer.transform.position;
        //     //Jump(referenceBallRigidbody2D);
        //     ballMoved = false;
        // }
    }

    // private void Update() {
    //     // replay
    //     if(Input.GetKeyDown(KeyCode.R))
    //     {
    //         //go.transform.position = originPos.transform.position;
    //         ballSpriteRenderer.color = Color.red;
    //         //ballSpriteRenderer.enabled = false;
    //         reachedSteadyState = false;
    //     }
    //
    //     isJumpPressed = Input.GetKeyDown(KeyCode.Space);
    //
    //     horizontalInput = Input.GetAxis("Horizontal");
    //     horizontalDir = new Vector2(horizontalInput, 0);
    // }

    private void Jump(Rigidbody2D rb)
    {
        // a jump at 5,5, mimicking a starting horizontal initial velocity, to make the landign position more easier to debug
        rb.velocity += jumpVelocity;
    }

    // walk is applied to main scene ball
    private void Walk(Vector2 dir, Rigidbody2D rb2d)
    {
        rb2d.velocity = dir * horizontalSpeed;
    }

    public void Replay()
    {
        //Vector2 action = actions[0];
        //Debug.Log(action);
        //actions.RemoveAt(0);
        //mainBallRigidbody2D.velocity = action;
    }
}

// CODE FROM PHYSICS DEMO WITH THE BALL AND STAR
/*
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PhysicsDemo : MonoBehaviour
{

    [Header("References")]
    public GameObject mainBall;
    public GameObject levelToCopy;

    [Space]
    [Header("Parameters")]
    public int simulationSteps = 0;
    public float walkSpeed = 10;

    public bool reachedGoal = false;

    private Scene mainScene;
    private Scene physicsScene;
    private GameObject physicsBall;  // physics scene ball
    private BallCollision physicsBallColl;
    private BallCollision mainBallColl;
    private SpriteRenderer ballSpriteRenderer;
    private Rigidbody2D physicsBallRigidbody2D;
    private Rigidbody2D mainBallRigidbody2D;
    private PhysicsScene2D activePhysicsScene2D;

    [Space]
    [Header("Replay System")]
    private List<Vector2> actions = new List<Vector2>();

    private void Start() {
        mainScene = SceneManager.GetActiveScene();
        physicsScene = SceneManager.CreateScene("sim-physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();

        // cache main scene components
        mainBallRigidbody2D = mainBall.GetComponent<Rigidbody2D>();
    }

    private void PreparePhysicsScene()
    {
        // There must always be one Scene marked as the active Scene.
        SceneManager.SetActiveScene(physicsScene);

        physicsBall =  GameObject.Instantiate(mainBall, mainBall.transform.position, Quaternion.identity);
        physicsBall.transform.name = "ReferenceBall";

        // cache physics scene references
        ballSpriteRenderer = physicsBall.GetComponent<SpriteRenderer>();
        physicsBallRigidbody2D = physicsBall.GetComponent<Rigidbody2D>();
        physicsBallRigidbody2D.constraints =  RigidbodyConstraints2D.None;
        activePhysicsScene2D = physicsScene.GetPhysicsScene2D();
        ballSpriteRenderer.color = Color.red;
        physicsBallColl = physicsBall.GetComponent<BallCollision>();
        mainBallColl = mainBall.GetComponent<BallCollision>();
        //ballSpriteRenderer.enabled = false;

        GameObject physicsLevel =  GameObject.Instantiate(levelToCopy, levelToCopy.transform.position, Quaternion.identity);
        physicsLevel.transform.name = "ReferenceLevel";
    }

    void FixedUpdate(){

        if (!reachedGoal)
        {
            for(int i = 0; i < simulationSteps; i++)
            {

                    if (physicsBall.transform.position.y < 0)
                    {
                        Vector2 moveLeft = new Vector2(-1,0);
                        Walk(moveLeft, physicsBallRigidbody2D);
                    }
                    else{
                        Vector2 moveRight = new Vector2(1,0);
                        Walk(moveRight, physicsBallRigidbody2D);
                    }

                    actions.Add(physicsBallRigidbody2D.velocity);

                Debug.Log(actions.Count);
                activePhysicsScene2D.Simulate(Time.fixedDeltaTime);
            }
        }
        else
        {
            Debug.Log(actions.Count);
            Replay();
        }

    }

    private void Walk(Vector2 dir, Rigidbody2D rb2d)
    {
        rb2d.velocity = dir * walkSpeed;
    }

    public void Replay()
    {
        Vector2 action = actions[0];
        //Debug.Log(action);
        actions.RemoveAt(0);
        mainBallRigidbody2D.velocity = action;
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Goal reached");
        demo.reachedGoal = true;

        demo.Replay();

    }


}
*/