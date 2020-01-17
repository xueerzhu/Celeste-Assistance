
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhysicsDemo : MonoBehaviour
{

    [Header("Public References")]
    public GameObject ballToSpawn;
    public GameObject slopesToSpawn;
    public GameObject originPos;

    [Space]
    [Header("Parameters")]
    public int simulationSteps = 0;
    public float horizontalSpeed = 10;
    public  Vector2 jumpVelocity;

    [Space]
    [Header("Bool")]
    public bool reachedSteadyState = false;
    public bool ballMoved = false; // ballMoved is the dirty flag to run physics scene whenever main scene ball pos changed

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

    private void Start() {
        // leave it = true so physics si simulated normally in main scene
        //Physics2D.autoSimulation = false;

        mainScene = SceneManager.GetActiveScene();
        physicsScene = SceneManager.CreateScene("sim-physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();

        // cache main scene components
        mainSceneBallRigidbody2D = ballToSpawn.GetComponent<Rigidbody2D>();
    }

    public void PreparePhysicsScene()
    {
        // There must always be one Scene marked as the active Scene.
        SceneManager.SetActiveScene(physicsScene);

        go =  GameObject.Instantiate(ballToSpawn, ballToSpawn.transform.position, Quaternion.identity);
        go.transform.name = "ReferenceBall";

        // cache physics scene references
        ballSpriteRenderer = go.GetComponent<SpriteRenderer>();
        referenceBallRigidbody2D = go.GetComponent<Rigidbody2D>();
        referenceBallRigidbody2D.constraints =  RigidbodyConstraints2D.None;
        activePhysicsScene2D = physicsScene.GetPhysicsScene2D();
        ballSpriteRenderer.color = Color.red;
        //ballSpriteRenderer.enabled = false;

        GameObject slopes =  GameObject.Instantiate(slopesToSpawn, slopesToSpawn.transform.position, Quaternion.identity);
        slopes.transform.name = "ReferenceLevel";
    }

    void FixedUpdate(){
            for(int i = 0; i < simulationSteps; i++)
            {
                activePhysicsScene2D.Simulate(Time.fixedDeltaTime);
                if (referenceBallRigidbody2D.velocity == Vector2.zero)
                {
                    reachedSteadyState = true;
                    ballSpriteRenderer.color = Color.green;
                    ballSpriteRenderer.enabled = true;
                    break;
                }
                else
                {
                    ballSpriteRenderer.color = Color.red;
                    //ballSpriteRenderer.enabled = false;
                }
            }

            if (horizontalInput > 0.1 || horizontalInput < -0.1)
            {
                ballMoved = true;
                Walk(horizontalDir);
            }


            // if (isJumpPressed == true)
            // {
            //     Jump(referenceBallRigidbody2D);
            // }

            // if ball pos changed, and current sim has stopped, resimulate physics
            if (ballMoved == true && referenceBallRigidbody2D.velocity == Vector2.zero)
            {

                go.transform.position = ballToSpawn.transform.position;
                //Jump(referenceBallRigidbody2D);
                ballMoved = false;
            }

    }
    private void Update() {
        // replay
        if(Input.GetKeyDown(KeyCode.R))
        {
            go.transform.position = originPos.transform.position;
            ballSpriteRenderer.color = Color.red;
            //ballSpriteRenderer.enabled = false;
            reachedSteadyState = false;
        }

        isJumpPressed = Input.GetKeyDown(KeyCode.Space);

        horizontalInput = Input.GetAxis("Horizontal");
        horizontalDir = new Vector2(horizontalInput, 0);
    }

    private void Jump(Rigidbody2D rb)
    {
        // a jump at 5,5, mimicking a starting horizontal initial velocity, to make the landign position more easier to debug
        rb.velocity += jumpVelocity;
    }

    // walk is applied to main scene ball
    private void Walk(Vector2 dir)
    {
        mainSceneBallRigidbody2D.velocity = new Vector2(dir.x * horizontalSpeed, mainSceneBallRigidbody2D.velocity.y);
    }


}
