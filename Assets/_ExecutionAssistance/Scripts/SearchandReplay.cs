using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SearchandReplay : MonoBehaviour {

    [Header("Public References")]
    public GameObject mainPlayer;
    public GameObject levelToCopy;

    [Space]
    [Header("Parameters")]
    public int simulationSteps = 0;

    [Space]
    [Header("Bool")]
    public bool reachedGoal = false;

    /// PRIVATE
    private Scene mainScene;
    private Scene simScene;

    private PhysicsScene2D simPhysicsScene2D;

    private GameObject simPlayer;
    private SpriteRenderer simPlayerRenderer;

    private Movement simMovement;
    private Movement playerMovement;

    bool isReplaying = false;
    bool replayDone = false;

    [Space]
    [Header("Replay System")]
    private Queue<Action> actionReplayQueue = new Queue<Action>();


    Dictionary<int, Vector2> DashDirectionDict = new Dictionary<int, Vector2>();
    public enum ActionType { WalkR, WalkL, Noop, Jump, Dash };

    public struct Action {
        // One of the possible action types
        public ActionType actionType;

        // For Walk and Jump modifier is how many frames it has been pressed.
        // For Dash, it is direction of the dash:
        // {0:right, 1:upright, 2:up, 3:upleft, 4:left ...}
        public int modifier;

        public Action(ActionType type, int m) {
            actionType = type;
            modifier = m;
        }
    }

    public struct State {
        // One of the possible action types
        public Vector2 madelinePos;
        public Vector2 madelineVel;
        public bool jumped;
        public bool dashed;
        public bool climbing;

        public State(Vector2 pos, Vector2 vel, bool j, bool d, bool c) {
            madelinePos = pos;
            madelineVel = vel;
            jumped = j;
            dashed = d;
            climbing = c;
        }
    }

    private void Start() {
        // int to dash direction. Go to action struct for explanation
        DashDirectionDict.Add(0, new Vector2(1, 0));
        DashDirectionDict.Add(1, new Vector2(1, 1));
        DashDirectionDict.Add(2, new Vector2(0, 1));
        DashDirectionDict.Add(3, new Vector2(-1, 1));
        DashDirectionDict.Add(4, new Vector2(-1, 0));
        DashDirectionDict.Add(5, new Vector2(-1, -1));
        DashDirectionDict.Add(6, new Vector2(0, -1));
        DashDirectionDict.Add(7, new Vector2(1, -1));

        // leave it = true so physics si simulated normally in main scene
        //Physics2D.autoSimulation = false;
        mainScene = SceneManager.GetActiveScene();
        simScene = SceneManager.CreateScene("sim-physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();

        // Set these when we have the simulation scene working
        simMovement = simPlayer.GetComponent<Movement>();
        playerMovement = mainPlayer.GetComponent<Movement>();

    }

    public void PreparePhysicsScene() {
        SceneManager.SetActiveScene(simScene);
        simPhysicsScene2D = simScene.GetPhysicsScene2D();

        simPlayer = Instantiate(mainPlayer, mainPlayer.transform.position, Quaternion.identity);
        simPlayer.transform.name = "simPlayer";

        simPlayerRenderer = simPlayer.transform.GetChild(0).GetComponent<SpriteRenderer>();
        simPlayerRenderer.color = Color.red;

        GameObject levelGeometry = Instantiate(levelToCopy, levelToCopy.transform.position, Quaternion.identity);
        levelGeometry.transform.name = "simLevel";
    }

    void FixedUpdate() {
        if (replayDone) {
            return;
        }

        // If you reached the goal...
        if (reachedGoal) {
            // and you are not replaying.
            if (!isReplaying) {
                // start the replay!
                StartCoroutine(Replay());
            }
        // Otherwise keep simulating the physics scene.
        } else {
            for (int i = 0; i < simulationSteps; i++) {
                // Here is an example action. In this case just moving right for 1 frame.
                actionReplayQueue.Enqueue(new Action(ActionType.WalkR, 1));

                // Take the action on the simulatedPlayer...
                simMovement.Walk(new Vector2(1, 0));

                // and actually simualte the physics for it for 1 frame.
                simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
            }
        }
    }

    public IEnumerator Replay() {
        isReplaying = true;
        print("Replay started with " + actionReplayQueue.Count + " actions");

        // Replay as long as there is something to replay in the Queue.
        while (actionReplayQueue.Count > 0) {

            // If there is no action that is being taken (because an action can be multiple frames)
            // pop the next action from the queue, and do it on the main player.
            if (activeAction == null) {
                Action act = actionReplayQueue.Dequeue();
                print("Action: " + act.actionType.ToString() + " has been taken and " + actionReplayQueue.Count + " remain.");
                TakeAction(act);
                
                // This line is possibly wrong! Might need to wait for end of fixed update.
                yield return null;
            } else {
                print("Action already active");
                // This line is possibly wrong! Might need to wait for end of fixed update.
                yield return null;
            }
        }
        isReplaying = false;
        replayDone = true;
    }

    Coroutine activeAction = null;
    // Given an action decides how to make the player game object execute it.
    void TakeAction(Action action) {
        // StartCoroutine returns an Coroutine object. We use that to indiciate an action is underway.
        switch (action.actionType) {
            case ActionType.WalkL:
                activeAction = StartCoroutine(ExecuteWalkForNFrames(-1, action.modifier));
                break;
            case ActionType.WalkR:
                activeAction = StartCoroutine(ExecuteWalkForNFrames(1, action.modifier));
                break;

            case ActionType.Jump:
                activeAction = StartCoroutine(ExecuteJumpForNFrames(action.modifier));
                break;

            case ActionType.Dash:
                Vector2 dir = DashDirectionDict[action.modifier];
                activeAction = StartCoroutine(ExecuteDashForNFrames(dir, 5));
                break;
        }
    }

    // The three different actions that are slightly different.
    IEnumerator ExecuteWalkForNFrames(int dir, int frameCount) {
        for (int i = 0; i < frameCount; i++) {
            Vector2 walkDir = new Vector2(dir, 0);
            playerMovement.Walk(walkDir);

            // This line might be problematic!
            yield return new WaitForFixedUpdate();

            //yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        activeAction = null;
        yield return null;
    }

    IEnumerator ExecuteJumpForNFrames(int frameCount) {
        for (int i = 0; i < frameCount; i++) {
            if (!playerMovement.wallGrab) {
                playerMovement.Jump(Vector2.up, false);
            } else {
                playerMovement.WallJump();
            }
            yield return new WaitForFixedUpdate();
        }
        activeAction = null;
        yield return null;
    }

    IEnumerator ExecuteDashForNFrames(Vector2 dir, int frameCount) {
        playerMovement.Dash(dir.x, dir.y);
        for (int i = 0; i < frameCount; i++) {
            yield return new WaitForFixedUpdate();
        }
        activeAction = null;
        yield return null;
    }
}