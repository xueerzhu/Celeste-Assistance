// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class JumpTrajectory : MonoBehaviour
// {
//     public GameObject assistAI;
//     public Transform referenceAI;
//     public GameObject objectsToSpawn;

//     public GameObject marker;
//     private List<GameObject> markers = new List<GameObject>();
//     //private Dictionary<string, >

//     private Scene mainScene;
//     private Scene physicsScene;

//     void Start() {
//         Physics.autoSimulation = false;

//         mainScene = SceneManager.GetActiveScene();
//         physicsScene = SceneManager.CreateScene("physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
//         PreparePhysicsScene();
//     }

//     void FixedUpdate() {
//         if (Input.GetMouseButton(0))
//         {
//             ShowTrajectory();
//         }

//         mainScene.GetPhysicsScene2D().Simulate(Time.deltaTime);
//     }

//     //TODO
//     public void RegisterJumpGizmo(JumpGizmo jump)
//     {
//         if (!allArrows.ContainsKey(jump.gameObject.name))
//         {
//             allArrows[jump.gameObject.name] = new RegisteredJumps();

//             var jumps = allArrows[jump.gameObject.name];
//             if (string.Compare(SyncArrows.gameObject.scene.name, physicsScene.name) == 0)
//             {
//                 SyncArrows.hidden = SyncArrows;
//             }
//             else
//             {
//                 SyncArrows.real = SyncArrows; 
//             }

//             allArrows[jump.gameObject.name] = arrows;
//         }
//     }
//     public void PreparePhysicsScene()
//     {
//         SceneManager.SetActiveScene(physicsScene);

//         GameObject g =  GameObject.Instantiate(objectsToSpawn);
//         g.transform.name = "ReferenceJump";
//         g.GetComponent<JumpGizmo>().isReference = true;
//         Destroy(g.GetComponent<MeshRenderer>());

//         SceneManager.SetActiveScene(mainScene);

//     }

//     //TODO
//     public void CreateMovementMarkers()
//     {
//         foreach (var arrowType in allArrows)
//         {
//             var arrows
//         }
//     }
//     public void ShowTrajectory()
//     {
        
//     }

//     public void SyncArrows()
//     {
//         foreach (var arrowType in allArrows)
//         {
//             var arrows = arrowType.Value;

//             Arrow visual = arrows.real;
//             Arrow hidden = arrows.hidden;
//             var rb = hidden.GetComponent<Rigidbody2D>();

//             rb.velocity = Vector2.zero;
//             rb.angularVelocity = Vector2.zero;

//             hidden.transform.postion = visual.transform.position;
//             hidden.transform.rotation = visual.transform.rotation;
//         }
//     }
// }
