using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct RegisteredJumps
{
    public JumpGizmo real;
    public JumpGizmo hidden;
}
public class JumpTrajectory : MonoBehaviour
{
    public static bool jumping; 

    public GameObject jump;
    public Transform referenceJump;
    public GameObject objectsToSpawn;

    public GameObject marker;
    private List<GameObject> markers = new List<GameObject>();
    private Dictionary<string, RegisteredJumps> allJumps = new Dictionary<string, RegisteredJumps>();

    private Scene mainScene;
    private Scene physicsScene;

    void Start() {
        Physics.autoSimulation = false;

        mainScene = SceneManager.GetActiveScene();
        physicsScene = SceneManager.CreateScene("physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        PreparePhysicsScene();
    }

    void FixedUpdate() {
        if (Input.GetMouseButton(0))
        {
            ShowTrajectory();
        }

        mainScene.GetPhysicsScene2D().Simulate(Time.deltaTime);
    }

    public void RegisterJumpGizmo(JumpGizmo jump)
    {
        if (!allJumps.ContainsKey(jump.gameObject.name))
        {
            allJumps[jump.gameObject.name] = new RegisteredJumps();

            var jumps = allJumps[jump.gameObject.name];
            if (string.Compare(jump.gameObject.scene.name, physicsScene.name) == 0)
            {
                jumps.hidden = jump;
            }
            else
            {
                jumps.real = jump; 
            }

            allJumps[jump.gameObject.name] = jumps;
        }
    }
    public void PreparePhysicsScene()
    {
        SceneManager.SetActiveScene(physicsScene);

        GameObject g =  GameObject.Instantiate(objectsToSpawn);
        g.transform.name = "ReferenceJump";
        g.GetComponent<JumpGizmo>().isReference = true;
        Destroy(g.GetComponent<MeshRenderer>());

        SceneManager.SetActiveScene(mainScene);

    }

    public void CreateMovementMarkers()
    {
        foreach (var jumpType in allJumps)
        {
            var jumps = jumpType.Value;  // RegisteredJumps type
            JumpGizmo hidden = jumps.hidden;

            GameObject g = GameObject.Instantiate(marker, hidden.transform.position, Quaternion.identity); 
            //GameObject g = GameObject.Instantiate(marker, hidden.transform.position) as GameObject; 
            g.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            markers.Add(g);
        }
    }
    public void ShowTrajectory()
    {
        SyncJumps();
        allJumps["ReferenceJump"].hidden.transform.rotation = referenceJump.transform.rotation;
        allJumps["ReferenceJump"].hidden.GetComponent<Rigidbody2D>().velocity = referenceJump.transform.TransformDirection(Vector2.up * 15f); 
        //allJumps["ReferenceJump"].hidden.GetComponent<Rigidbody2D>().useGravity = true;

        int steps = (int)(2f / Time.fixedDeltaTime);
        for (int i = 0; i < steps; i++)
        {
            physicsScene.GetPhysicsScene2D().Simulate(Time.fixedDeltaTime);
            CreateMovementMarkers();
        }
    }

    public void SyncJumps()
    {
        foreach (KeyValuePair<string, RegisteredJumps> jumpType in allJumps)
        {
            RegisteredJumps jumps = jumpType.Value;

            JumpGizmo visual = jumps.real;
            JumpGizmo hidden = jumps.hidden;
            var rb = hidden.GetComponent<Rigidbody2D>();

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            hidden.transform.position = visual.transform.position;
            hidden.transform.rotation = visual.transform.rotation;
        }
    }
}
