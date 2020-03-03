using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SearchandReplay : MonoBehaviour {

    [Header("Public References")]
    public GameObject mainPlayer;
    public GameObject levelToCopy;
    public GameObject star;

    Rigidbody2D mainPlayerRB;
    Rigidbody2D simPlayerRB;

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
    private Queue<State>  stateReplayQueue = new Queue<State>();
    private Queue<Vector2>  vectorReplayQueue = new Queue<Vector2>();


    Dictionary<int, Vector2> DashDirectionDict = new Dictionary<int, Vector2>();
    public enum ActionType { WalkR, WalkL, Noop, Jump, Dash };
    public enum ActionSet {WalkR, WalkL, Jump, DashUp, DashRight, DashLeft};

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

        public void print()
        {
            Debug.Log("Pos: " + madelinePos + " Vel: " + madelineVel + " Jump: " + jumped + " Dash: " + dashed + " Climb: " + climbing);
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

        mainPlayerRB = mainPlayer.GetComponent<Rigidbody2D>();
        simPlayerRB = simPlayer.GetComponent<Rigidbody2D>();
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
                StartCoroutine(ReplayFromState());
            }
        // Otherwise keep simulating the physics scene.
        } else {
            vectorReplayQueue = RunAStar();
            for (int i = 0; i < simulationSteps; i++) {
                // Here is an example action. In this case just moving right for 1 frame.



                //actionReplayQueue.Enqueue(new Action(ActionType.WalkR, 1));

                // !!!
                // In order to record the state correctly, we need to know if madeline can Dash/Jump whether she is climbing.
                // There most likely are varibles in the Movement script that correspond to these three bools directly,
                // find them and add them to current state! If there doesn't exist a simple bool just make it (isgrounded vs hasjumped can be combined i
                // into can jmp
                //State currentState = new State(simPlayer.transform.position, simPlayerRB.velocity, false, false, false);
                //currentState.print();
                //stateReplayQueue.Enqueue(currentState);

                // Take the action on the simulatedPlayer...
                // We would be doing the search here.
            //    simMovement.Walk(new Vector2(1, 0));

                // and actually simualte the physics for it for 1 frame.
                simPhysicsScene2D.Simulate(Time.fixedDeltaTime);

            }
        }
        //Debug.Log(stateReplayQueue.Count);
    }


    // We need to implement this function now.
    public Queue<Vector2> RunAStar() {
        // Node -> Vector2, List of Vector2
        Queue<Node> priorityQueue = new Queue<Node>();

        // Starting state
        //State currentState = new State(simPlayer.transform.position, simPlayerRB.velocity, false, false, false);
        priorityQueue.Enqueue(new Node(simPlayer.transform.position, null));

        // Positions explored
        List<Vector2> explored = new List<Vector2>();

        // Successor positions
        List<Vector2> avaliableActions = new List<Vector2>();
        avaliableActions.Add(new Vector2(-1, 0));
        avaliableActions.Add(new Vector2(1, 0));

        // Keeps searching while it hasn't reached the goal
        while (!reachedGoal)
        {
            // Heuristic as the distance between player and star
            float distance = Vector2.Distance(simPlayer.transform.position, star.transform.position);

            Node currentNode = priorityQueue.Dequeue();
            Vector2 currentPos = currentNode.position;

            // Only searches through position if it hasn't been searched before
            if (!explored.Contains(currentPos))
            {
                explored.Add(currentPos);

                State baseState = currentNode.state;
                for (int i = 0; i < avaliableActions.Count(); i++)
                {
                    RestoreStrate(baseState);
                    pickedACtion = avaliableActions[i];
                    takeAction(pickedACtion);
                    simPhysicsScene2D.sim()
                    cost = distance;
                    newState = new State(bla bla bla), // cost
                    newNode = new Node(newState, prevNode, cost);
                    priorityQueue.add(newNode);

                    Vector2 newPos = currentPos;
                    newPos += avaliableActions[i];

                    if (!explored.Contains(newPos))
                    {
                        Node tempNode = currentNode.prevNode;
                        tempNode.Add(currentPos);

                        Node newNode = new Node(newPos, tempNode);
                        priorityQueue.Enqueue(newNode);
                    }
                }
            }
        }

        // while goal not reached keep searching
            // goal reached colliding with goal (there is a goal script that checks for that)
        //Have a sorted queue, that sorts the states using (cost of getting there + heuristic)
        // check out the A* psudocode and message me if you need help.

        /*
        frontier = util.PriorityQueue()
        frontier.push((problem.startingState(), [], 0), 0)  # position, path, cost
        explored = []   # Explored paths

        while not frontier.isEmpty():
            node, path, cost = frontier.pop()
            if not node in explored:

                # If the goal is found, return the path taken to the goal
                if (problem.isGoal(node)):
                    return path

                explored.append(node)

                # Only adds sucessor nodes, updated path, and cost if it hasn't been explored yet
                for nextNode in problem.successorStates(node):
                    if not nextNode[0] in explored:
                        frontier.push((nextNode[0], path + [nextNode[1]], cost + nextNode[2]),
                            cost + nextNode[2] + heuristic(nextNode[0], problem))
        */
        return null;
    }

    public List<Node> pathOfNode(Node finalNode)
    {
        List<Node> finalPath = new List<Node>();
        Node currentNode = finalNode;
        while (currentNode.prevNode != null)
        {
            finalPath.Add(currentNode);
            currentNode = currentNode.prevNode;
        }

        finalPath.Reverse();
        return finalPath;
    }

    public struct Node {
        // One of the possible action types
        public Vector2 position;
        public Node prevNode;

        public Node(Vector2 pos, List<Vector2> prev) {
            position = pos;
            prevNode = prev;
        }
    }

    public IEnumerator ReplayFromState() {
        isReplaying = true;
        //print("Replay started with " + actionReplayQueue.Count + " actions");

        // Replay as long as there is something to replay in the Queue.
        while (stateReplayQueue.Count > 0) {
            print("Replay started with " + stateReplayQueue.Count + " state");
            State currReplayState = stateReplayQueue.Dequeue();
            UpdateMainPlayer(currReplayState);
            yield return new WaitForFixedUpdate();
        }
        isReplaying = false;
        replayDone = true;
    }

    void UpdateMainPlayer(State currState) {
        mainPlayer.transform.position = currState.madelinePos;
        //mainPlayerRB.velocity = currState.madelineVel;

        // set the flags here as well.
    }
}

//     public IEnumerator Replay() {
//         isReplaying = true;
//         //print("Replay started with " + actionReplayQueue.Count + " actions");
//
//         // Replay as long as there is something to replay in the Queue.
//         while (actionReplayQueue.Count > 0) {
//
//             // If there is no action that is being taken (because an action can be multiple frames)
//             // pop the next action from the queue, and do it on the main player.
//             if (activeAction == null) {
//                 Action act = actionReplayQueue.Dequeue();
//                 print("Action: " + act.actionType.ToString() + " has been taken and " + actionReplayQueue.Count + " remain.");
//                 TakeAction(act);
//
//                 // This line is possibly wrong! Might need to wait for end of fixed update.
//                 yield return new WaitForEndOfFrame();
//             } else {
//                 print("Action already active");
//                 // This line is possibly wrong! Might need to wait for end of fixed update.
//                 yield return new WaitForEndOfFrame();
//             }
//         }
//         isReplaying = false;
//         replayDone = true;
//     }
//
//     Coroutine activeAction = null;
//     // Given an action decides how to make the player game object execute it.
//     void TakeAction(Action action) {
//
//         // StartCoroutine returns an Coroutine object. We use that to indiciate an action is underway.
//         switch (action.actionType) {
//             case ActionType.WalkL:
//                 activeAction = StartCoroutine(ExecuteWalkForNFrames(-1, action.modifier));
//                 break;
//             case ActionType.WalkR:
//                 activeAction = StartCoroutine(ExecuteWalkForNFrames(1, action.modifier));
//                 break;
//
//             case ActionType.Jump:
//                 activeAction = StartCoroutine(ExecuteJumpForNFrames(action.modifier));
//                 break;
//
//             case ActionType.Dash:
//                 Vector2 dir = DashDirectionDict[action.modifier];
//                 activeAction = StartCoroutine(ExecuteDashForNFrames(dir, 5));
//                 break;
//         }
//     }
//
//     // The three different actions that are slightly different.
//     IEnumerator ExecuteWalkForNFrames(int dir, int frameCount) {
//         for (int i = 0; i < frameCount; i++) {
//             Vector2 walkDir = new Vector2(dir, 0);
//             playerMovement.Walk(walkDir);
//
//             // This line might be problematic!
//             yield return new WaitForFixedUpdate();
//
//             //yield return new WaitForSeconds(Time.fixedDeltaTime);
//         }
//         activeAction = null;
//         yield return null;
//     }
//
//     IEnumerator ExecuteJumpForNFrames(int frameCount) {
//         for (int i = 0; i < frameCount; i++) {
//             if (!playerMovement.wallGrab) {
//                 playerMovement.Jump(Vector2.up, false);
//             } else {
//                 playerMovement.WallJump();
//             }
//             yield return new WaitForFixedUpdate();
//         }
//         activeAction = null;
//         yield return null;
//     }
//
//     IEnumerator ExecuteDashForNFrames(Vector2 dir, int frameCount) {
//         playerMovement.Dash(dir.x, dir.y);
//         for (int i = 0; i < frameCount; i++) {
//             yield return new WaitForFixedUpdate();
//         }
//         activeAction = null;
//         yield return null;
//     }
// }
