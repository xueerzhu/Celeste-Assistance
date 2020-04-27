using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Priority_Queue;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine.UI;


public partial class SearchandReplay : MonoBehaviour {
    [Header("Public References")] public GameObject mainPlayer;
    public GameObject levelToCopy;
    public GameObject star;
    public TextMeshProUGUI replayActionText;

    Rigidbody2D mainPlayerRB;
    Rigidbody2D simPlayerRB;

    [Space] [Header("Parameters")] public int simulationSteps = 1;

    public int actionFrameCount = 1;
    public float baseActionCost = 1;

    [Space] [Header("Debug Controls")] public bool reachedGoal = false;

    public bool debug = false;
    public bool profile = false;
    [SerializeField] bool loopingReplay = false;
    [SerializeField] bool isGhostVisualized = true;
    /// PRIVATE
    private Scene mainScene;

    private Scene simScene;

    private PhysicsScene2D simPhysicsScene2D;

    private GameObject simPlayer;
    private SpriteRenderer simPlayerRenderer;

    private Movement simMovement;
    private Movement playerMovement;

    private Collision simCollision;

    SpriteRenderer simRenderer;

    bool isSearching = false;
    [SerializeField] bool isReplaying = false;

    int spaceSearched = 0;
    float startTime = 0;
    public float maxSearchTime = 10f;

    [Space] [Header("Replay System")] private Queue<Action> actionReplayQueue = new Queue<Action>();
    private Queue<State> stateReplayQueue = new Queue<State>();
    private Queue<Node> nodeReplayQueue = new Queue<Node>();


    Dictionary<int, Vector2> DashDirectionDict = new Dictionary<int, Vector2>();

    StringBuilder sb = new StringBuilder("", 20);
    public float cellSize = .5f;
    Dictionary<string, bool> queueCheck = new Dictionary<string, bool>();
    public bool hashOptimization = true;
    public enum ActionType {
        WalkR,
        WalkL,
        Noop,
        Jump,
        Dash
    };

    List<Action> actionSets = new List<Action>();

    private Action WalkR;
    private Action WalkL;
    private Action Jump;
    private Action DashR;
    private Action DashUR;
    private Action DashU;
    private Action DashUL;
    private Action DashL;

    [Space] [Header("Astar")] SimplePriorityQueue<Node> priorityQueue = new SimplePriorityQueue<Node>();
    Stack<State> exploredStack = new Stack<State>();
    Node finalNode = null;

    WaitForEndOfFrame WaitEndOfFrame = new WaitForEndOfFrame();
    WaitForFixedUpdate WaitFixedUpdate = new WaitForFixedUpdate();

    public struct State {
        // One of the possible action types
        public Vector2 madelinePos;
        public Vector2 madelineVel;
        public bool canMove;
        public bool onGround;
        public bool hasDashed;
        public bool onWall;

        public State(Vector2 pos, Vector2 vel, bool onground, bool candash, bool canmove, bool onwall) {
            madelinePos = pos;
            madelineVel = vel;
            canMove = canmove;
            hasDashed = candash;
            onGround = onground;
            onWall = onwall;
            string hashKey;
        }

        public void print() {
            Debug.Log("Pos: " + madelinePos + " Vel: " + madelineVel + ", Jump: " + canMove + ", Dash: " + hasDashed +
                      ", Climb: " + onGround);
        }
    }

    private void Awake() {
        // int to dash direction. Go to action struct for explanation
        DashDirectionDict.Add(0, new Vector2(1, 0));
        DashDirectionDict.Add(1, new Vector2(1, 1));
        DashDirectionDict.Add(2, new Vector2(0, 1));
        DashDirectionDict.Add(3, new Vector2(-1, 1));
        DashDirectionDict.Add(4, new Vector2(-1, 0));
        DashDirectionDict.Add(5, new Vector2(-1, -1));
        DashDirectionDict.Add(6, new Vector2(0, -1));
        DashDirectionDict.Add(7, new Vector2(1, -1));


        // leave it = true so physics simulated normally in main scene
        //Physics2D.autoSimulation = false;
        mainScene = SceneManager.GetActiveScene();
        simScene = SceneManager.CreateScene("sim-physics-scene", new CreateSceneParameters(LocalPhysicsMode.Physics2D));

        PreparePhysicsScene();

        // Set these when we have the simulation scene working
        simMovement = simPlayer.GetComponent<Movement>();
        simMovement.simulated = true;

        playerMovement = mainPlayer.GetComponent<Movement>();

        simCollision = simPlayer.GetComponent<Collision>();

        mainPlayerRB = mainPlayer.GetComponent<Rigidbody2D>();
        simPlayerRB = simPlayer.GetComponent<Rigidbody2D>();

        simRenderer = simPlayer.GetComponentInChildren<SpriteRenderer>();
        simRenderer.enabled = false;

        PrepareAStar();
    }

    // Action Sets
    void PrepareAStar() {
        WalkR = new Action(ActionType.WalkR, actionFrameCount);
        WalkL = new Action(ActionType.WalkL, actionFrameCount);
        Jump = new Action(ActionType.Jump, 8);
        DashR = new Action(ActionType.Dash, 0);
        DashUR = new Action(ActionType.Dash, 1);
        DashU = new Action(ActionType.Dash, 2);
        DashUL = new Action(ActionType.Dash, 3);
        DashL = new Action(ActionType.Dash, 4);


        // Actions that Astar will search for
        actionSets.Add(WalkR);
        actionSets.Add(WalkL);
        actionSets.Add(Jump);
        actionSets.Add(DashR);
        actionSets.Add(DashUR);
        actionSets.Add(DashU);
        actionSets.Add(DashUL);
        actionSets.Add(DashL);
        //actionSets.Add(new Action(ActionType.Dash, 1));
    }

    public void PreparePhysicsScene() {
        SceneManager.SetActiveScene(simScene);
        simPhysicsScene2D = simScene.GetPhysicsScene2D();

        simPlayer = Instantiate(mainPlayer, mainPlayer.transform.position, Quaternion.identity);
        simPlayer.transform.name = "simPlayer";

        simPlayerRenderer = simPlayer.transform.GetChild(0).GetComponent<SpriteRenderer>();
        simPlayerRenderer.color = new Color(0f, 0.31f, 0.29f);

        GameObject levelGeometry = Instantiate(levelToCopy, levelToCopy.transform.position, Quaternion.identity);
        levelGeometry.transform.name = "simLevel";
    }

    //optimize here
    string GetSimHashKey(State state) {
        sb.Clear();
        int xCell = Mathf.FloorToInt(state.madelinePos.x / cellSize);
        int yCell = Mathf.FloorToInt(state.madelinePos.y / cellSize);
        int vxCell = Mathf.FloorToInt(state.madelineVel.x / cellSize);
        int vyCell = Mathf.FloorToInt(state.madelineVel.y / cellSize);

        sb.Append(xCell);
        sb.Append(",");
        sb.Append(yCell);
        sb.Append(",");
        sb.Append(vxCell);
        sb.Append(",");
        sb.Append(vyCell);
        sb.Append(",");
        sb.Append(state.onGround);
        sb.Append(state.canMove);
        sb.Append(state.hasDashed);

        return sb.ToString();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            SceneManager.LoadScene(mainScene.name);
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            StartSearch();
        }

        HandleAStart();

    }

    void FixedUpdate() {
    }

    void HandleAStart() {
        if (reachedGoal && !isReplaying) {
            nodeReplayQueue = ReturnAStarResult();
            printFoundPath();

            StartCoroutine(ReplayFromNodeActions());
            // TODO: this is not working
            // freeze player at paths from astar result, instead of drop due to gravity
            mainPlayer.transform.position = simPlayer.transform.position;
        }

        if (isSearching) {
            if (profile && isProfilingTimeLimitReached()) return;

            RunAStar();
            isSearching = !reachedGoal;
            simRenderer.enabled = !reachedGoal;
        }
    }

    void StartSearch() {
        reachedGoal = false;
        isReplaying = false;
        isSearching = true;

        simRenderer.enabled = isGhostVisualized;

        simPlayer.transform.position = playerMovement.transform.position;
        priorityQueue.Enqueue(new Node(GetSimPlayerState(new Action(ActionType.WalkR, 0)), null, GetHeuristic(), 0),
            GetHeuristic());

        startTime = Time.unscaledTime;

        mainPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        simPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void printFoundPath() {
        Debug.Log("nodeReplayQueue is: ");
        foreach (var node in nodeReplayQueue) {
            //node.PrintNode();
        }
    }

    bool isProfilingTimeLimitReached() {
        if (Time.unscaledTime > startTime + maxSearchTime) {
            print("Spaces searched: " + spaceSearched);
            return true;
        }
        return false;
    }

    // var reuse for RunAStar
    private Node currentNode;
    private State currentState;
    float cost;
    State newState;
    private Node newNode;

    private void RunAStar() {
        for (int i = 0; i < simulationSteps; i++) {
            print("The priorityQueue length:" + priorityQueue.Count);

            currentNode = priorityQueue.Dequeue();
            currentState = currentNode.state;
            exploredStack.Push(currentState);
            if (hashOptimization) {
                string has = GetSimHashKey(currentState);
                queueCheck[has] = false;
            }

            if (debug)
                currentNode.PrintNode();

            resetActions();
            RestoreState(currentState);
            foreach (var pickedAction in availableActions(actionSets)) {
                RestoreState(currentState);
                TakeAction(pickedAction, simMovement);
                cost = GetHeuristic();
                newState = GetSimPlayerState(pickedAction);

                newNode = new Node(newState, currentNode, cost, currentNode.costToGetHere + baseActionCost,
                    pickedAction);

                if (reachedGoal) {
                    finalNode = newNode;
                    return;
                }

                spaceSearched++;
                string hash = "";
                if (hashOptimization) {
                    hash = GetSimHashKey(newState);
                    if (queueCheck.ContainsKey(hash) && queueCheck[hash]) {
                        print("hit");
                        continue;
                    }
                }
                priorityQueue.Enqueue(newNode, cost + newNode.costToGetHere);
                if (hashOptimization)
                    queueCheck[hash] = true;
            }
        }
    }

    private void resetActions() {
        actionSets.Clear();
        actionSets.Add(WalkR);
        actionSets.Add(WalkL);
        actionSets.Add(Jump);
        actionSets.Add(DashR);
        actionSets.Add(DashUR);
        actionSets.Add(DashU);
        actionSets.Add(DashUL);
        actionSets.Add(DashL);
    }

    private List<Action> availableActions(List<Action> actions) {
        List<Action> currActions = actions;
        simCollision.checkCollision();
        if (!simMovement.canMove) {
            //Debug.Log("Removed Walking");
            currActions.Remove(WalkR);
            currActions.Remove(WalkL);
        }

        // If not on the ground, simulation can't jump
        if (!simCollision.onGround) {
            currActions.Remove(Jump);
        }

        // If dashed and hasn't touched the ground, simulation can't dash
        if (simMovement.hasDashed) {
            Debug.Log("Dash Removed");
            currActions.Remove(DashR);
            currActions.Remove(DashUR);
            currActions.Remove(DashU);
            currActions.Remove(DashUL);
            currActions.Remove(DashL);
        }

        return currActions;
    }

    private Queue<Node> ReturnAStarResult() {
        // the last explored node should be final node
        //Node finalNode = exploredStack.Pop();
        // construct a queue of node base on final node: reverse linked list
        Node pointerNode = finalNode;
        while (pointerNode.prevNode != null) {
            nodeReplayQueue.Enqueue(pointerNode);
            pointerNode = pointerNode.prevNode;
        }

        nodeReplayQueue.Enqueue(pointerNode);
        nodeReplayQueue = new Queue<Node>(nodeReplayQueue.Reverse());

        return nodeReplayQueue;
    }

    // https://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
    // Diagonal distance for heuristic
    private float GetHeuristic() {
        float dx = Math.Abs(simPlayer.transform.position.x - star.transform.position.x);
        float dy = Math.Abs(simPlayer.transform.position.y - star.transform.position.y);
        float heuristic = 1 * (dx + dy) + (1 - 2 * 1) * Math.Min(dx, dy);

        return (float) Math.Round(heuristic, 2);
    }

    // get sim player runtime state
    // !!! Need to add CanDash
    public State GetSimPlayerState(Action action) {
        bool canMove = simMovement.canMove;
        bool onGround = simCollision.onGround;
        bool hasDashed = simMovement.hasDashed;
        bool onWall = simCollision.onWall;

        // if (action.Equals(Jump)) j = true;
        // if (action.Equals(DashR) || action.Equals(DashUR) || action.Equals(DashU) || action.Equals(DashUL) ||
        //     action.Equals(DashU)) d = true;

        return new State(simPlayer.transform.position, simPlayerRB.velocity, onGround, hasDashed, canMove, onWall);
    }

    public IEnumerator ReplayFromNodeActions()
    {
        //mainPlayerRB.isKinematic = true;
        isReplaying = true;
        // Replay as long as there is something to replay in the Queue.
        int count = 1;
        bool updateFirst = true;
        print("Replay started with " + nodeReplayQueue.Count + " node");

        while (nodeReplayQueue.Count > 0) {
            Node currentReplayNode = nodeReplayQueue.Dequeue();
            //currentReplayNode.PrintNode();

            Action act = currentReplayNode.action;

            State currReplayState = currentReplayNode.state;

            TakeAction(act, playerMovement);
            Debug.Log("Replaying action:" + act.actionType);
            //Debug.Log(mainPlayerRB.velocity);
            replayActionText.text = count + ": " + act.actionType.ToString();

            UpdateMainPlayer(currReplayState);

            yield return new WaitForFrames(act.modifier); // gives it the exact number frames it simulated to replay

            count++;
        }
        isReplaying = false;

        if (!loopingReplay)
            reachedGoal = false;
    }

    public IEnumerator ReplayFromNode() {
        isReplaying = true;
        while (nodeReplayQueue.Count > 0) {
            print("Replay started with " + nodeReplayQueue.Count + " node");
            State currReplayState = nodeReplayQueue.Dequeue().state;
            UpdateMainPlayer(currReplayState);
            yield return WaitEndOfFrame;
        }
        isReplaying = false;

        if (!loopingReplay)
            reachedGoal = false;
    }


    void UpdateMainPlayer(State currState) {
        mainPlayer.transform.position = currState.madelinePos;
    }

    void RestoreState(State state) {
        //sets the current Unity state to the state
        simPlayer.transform.position = state.madelinePos;
        simPlayerRB.velocity = state.madelineVel;

        simMovement.canMove = state.canMove;
        simCollision.onGround = state.onGround;
        simCollision.onWall = state.onWall;
        simMovement.hasDashed = state.hasDashed;
    }


    Coroutine activeAction = null;

    // Given an action decides how to make the player game object execute it.
    void TakeAction(Action action, Movement agentMovement) {
        // StartCoroutine returns an Coroutine object. We use that to indiciate an action is underway.
        switch (action.actionType) {
            case ActionType.WalkL:
                // activeAction = StartCoroutine(ExecuteWalkForNFrames(-1, action.modifier, agentMovement));
                SimulateWalkForNFrames(-1, action.modifier, agentMovement);
                break;
            case ActionType.WalkR:
                //activeAction = StartCoroutine(ExecuteWalkForNFrames(1, action.modifier, agentMovement));
                SimulateWalkForNFrames(1, action.modifier, agentMovement);
                break;

            case ActionType.Jump:
                //activeAction = StartCoroutine(ExecuteJumpForNFrames(action.modifier, agentMovement));
                SimulateJumpForNFrames(action.modifier, agentMovement);
                break;

            case ActionType.Dash:
                Vector2 dir = DashDirectionDict[action.modifier]; // this modifier determines direction
                //activeAction = StartCoroutine(ExecuteDashForNFrames(dir, 5, agentMovement));
                SimulateDashForNFrames(dir, 5, agentMovement);  // 5 is harded dash frame length

                break;
        }
    }

    void SimulateDashForNFrames(Vector2 dir, int frameCount, Movement agentMovement) {
        agentMovement.Dash(dir.x, dir.y);
        for (int i = 0; i < frameCount; i++) {
            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }

    void SimulateWalkForNFrames(int dir, int frameCount, Movement agentMovement) {
        for (int i = 0; i < frameCount; i++) {
            Vector2 walkDir = new Vector2(dir, 0);
            agentMovement.Walk(walkDir);
            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
            Debug.Log(mainPlayerRB.position);
        }
    }

    void SimulateJumpForNFrames(int frameCount, Movement agentMovement) {
        for (int i = 0; i < frameCount; i++) {
            if (!agentMovement.wallGrab) {
                agentMovement.Jump(Vector2.up, false);
            }
            else {
                agentMovement.WallJump();
            }

            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }

    // // The three different actions that are slightly different.
    // IEnumerator ExecuteWalkForNFrames(int dir, int frameCount, Movement agentMovement) {
    //     for (int i = 0; i < frameCount; i++) {
    //         Vector2 walkDir = new Vector2(dir, 0);
    //         agentMovement.Walk(walkDir);
    //         // Debug.Log(i);
    //
    //         // This line might be problematic!
    //         yield return WaitFixedUpdate;
    //         // THERE IS NO SIMULATION BEING DONE  HERE!
    //
    //         //yield return new WaitForSeconds(Time.fixedDeltaTime);
    //     }
    //
    //     activeAction = null;
    //     yield return null;
    // }
    //
    // IEnumerator ExecuteJumpForNFrames(int frameCount, Movement agentMovement) {
    //     for (int i = 0; i < frameCount; i++) {
    //         if (!agentMovement.wallGrab) {
    //             agentMovement.Jump(Vector2.up, false);
    //         }
    //         else {
    //             agentMovement.WallJump();
    //         }
    //         yield return WaitFixedUpdate;
    //     }
    //
    //     activeAction = null;
    //     yield return null;
    // }
    //
    // IEnumerator ExecuteDashForNFrames(Vector2 dir, int frameCount, Movement agentMovement) {
    //     agentMovement.Dash(dir.x, dir.y);
    //     for (int i = 0; i < frameCount; i++) {
    //         yield return WaitFixedUpdate;
    //     }
    //
    //     activeAction = null;
    //     yield return null;
    // }
}
