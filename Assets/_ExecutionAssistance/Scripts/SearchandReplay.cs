using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Priority_Queue;
using System.Linq;

public class SearchandReplay : MonoBehaviour {

    [Header("Public References")]
    public GameObject mainPlayer;
    public GameObject levelToCopy;
    public GameObject star;

    Rigidbody2D mainPlayerRB;
    Rigidbody2D simPlayerRB;

    [Space]
    [Header("Parameters")]
    public int simulationSteps = 1;

    public int actionFrameCount = 1;
    public float baseActionCost = 1;

    [Space]
    [Header("Bool")]
    public bool reachedGoal = false;

    public bool debug = false;
    public bool profile = false;
    
    /// PRIVATE
    private Scene mainScene;
    private Scene simScene;

    private PhysicsScene2D simPhysicsScene2D;

    private GameObject simPlayer;
    private SpriteRenderer simPlayerRenderer;

    private Movement simMovement;
    private Movement playerMovement;

    private Collision simCollision;

    bool beginSearch = false;
    bool isReplaying = false;
    bool replayDone = false;

    int spaceSearched = 0;
    float startTime = 0;
    public float maxSearchTime = 10f;

    [Space]
    [Header("Replay System")]
    private Queue<Action> actionReplayQueue = new Queue<Action>();
    private Queue<State>  stateReplayQueue = new Queue<State>();
    private Queue<Node>  nodeReplayQueue = new Queue<Node>();


    Dictionary<int, Vector2> DashDirectionDict = new Dictionary<int, Vector2>();
    public enum ActionType { WalkR, WalkL, Noop, Jump, Dash };
    List<Action> actionSets = new List<Action>();

    private Action WalkR;
    private Action WalkL;
    private Action Jump;
    private Action DashR;
    private Action DashUR;
    private Action DashU;
    private Action DashUL;
    private Action DashL;

    [Space]
    [Header("Astar")]
    SimplePriorityQueue<Node> priorityQueue = new SimplePriorityQueue<Node>();
    Stack<State> exploredStack = new Stack<State>();
    Node finalNode = null;

    WaitForEndOfFrame WaitEndOfFrame = new WaitForEndOfFrame();
    WaitForFixedUpdate WaitFixedUpdate = new WaitForFixedUpdate();
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
            Debug.Log("Pos: " + madelinePos + " Vel: " + madelineVel + ", Jump: " + jumped + ", Dash: " + dashed + ", Climb: " + climbing);
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
        playerMovement = mainPlayer.GetComponent<Movement>();

        simCollision = simPlayer.GetComponent<Collision>();

        mainPlayerRB = mainPlayer.GetComponent<Rigidbody2D>();
        simPlayerRB = simPlayer.GetComponent<Rigidbody2D>();
        
        PrepareAStar();
    }

    private void Start()
    {
        //nodeReplayQueue = RunAStar();
        //TakeAction(Jump, playerMovement);
    }

    // Action Sets
    void PrepareAStar()
    {
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

        //starting node add to queue
        priorityQueue.Enqueue(new Node(GetSimPlayerState(new Action(ActionType.WalkR, 0)), null, GetHeuristic(), 0), GetHeuristic());
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

    void FixedUpdate() 
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(mainScene.name);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            beginSearch = true;
            startTime = Time.unscaledTime;

            mainPlayerRB.constraints = RigidbodyConstraints2D.None;
            mainPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            simPlayerRB.constraints = RigidbodyConstraints2D.None;
            simPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (replayDone || isReplaying)
        {
            return;
        }

        if (beginSearch)
        {
            if (profile && Time.unscaledTime > startTime + maxSearchTime)
            {
                print("Spaces searched: " + spaceSearched);
                return;
            }

            if (reachedGoal)
            {
                nodeReplayQueue = ReturnAStarResult();
                Debug.Log("nodeReplayQueue is: ");
                foreach (var node in nodeReplayQueue)
                {
                    node.PrintNode();
                }

                if (!isReplaying)
                {
                    StartCoroutine(ReplayFromNode());
                    //StartCoroutine(ReplayFromNodeActions());

                    // TODO: this is not working
                    // freeze player at paths from astar result, instead of drop due to gravity
                    mainPlayer.transform.position = mainPlayer.transform.position;
                }
            }
            else
            {
                for (int i = 0; i < simulationSteps; i++)
                {
                    RunAStar();
                    if (reachedGoal) return;
                }
            }
        }
        else
        {
            mainPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
            simPlayerRB.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void RunAStar()
    {
       
        // Keeps searching while it hasn't reached the goal
        if (priorityQueue.Count() != 0)
        {
            Node currentNode = priorityQueue.Dequeue();

            State baseState = currentNode.state;

            //Debug.Log("base state is:");
            //baseState.print();
            if (!exploredStack.Contains(baseState))
            {
                exploredStack.Push(baseState);
                if (debug)
                    currentNode.PrintNode();
                resetActions();
                
                foreach (var pickedAction in availableActions (actionSets))
                {
                    //currentNode.state.print();
                    RestoreState(baseState);
                    TakeAction(pickedAction, simMovement);

                    //Debug.Log(pickedAction.actionType + ", new state is:");

                    // Is it correct to simiulate here?
                    //simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
                   
                    float cost = GetHeuristic();
                    State newState = GetSimPlayerState(pickedAction);
                    //newState.print();

                    Node newNode = new Node(newState, currentNode, cost, currentNode.costToGetHere + baseActionCost, pickedAction);
                    if (reachedGoal)
                    {
                        Debug.Log("reached goal");
                        finalNode = newNode;
                        return;
                    }

                    if (!exploredStack.Contains(newNode.state))
                    {
                        spaceSearched++;
                        priorityQueue.Enqueue(newNode, cost + newNode.costToGetHere);

                    }
                }

                //Debug.Log("priority queue is: ");
                //foreach (var node in priorityQueue)
                //{
                //node.PrintNode();
                //}
            }
        }
        else
        {
            Debug.Log("No Path Found");
        }
    }

    private void resetActions()
    {
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

    private List<Action> availableActions (List<Action> actions)
    {
        List<Action> currActions = actions;

        if (!simMovement.canMove)
        {
            //Debug.Log("Removed Walking");
            currActions.Remove(WalkR);
            currActions.Remove(WalkL);
        }
        if (!simCollision.onGround)
        {
            //Debug.Log("Removed Jump");
            currActions.Remove(Jump);
        }
        if (simMovement.hasDashed)
        {
            currActions.Remove(DashR);
            currActions.Remove(DashUR);
            currActions.Remove(DashU);
            currActions.Remove(DashUL);
            currActions.Remove(DashL);
        }

        return currActions;
    }

    private Queue<Node> ReturnAStarResult()
    {
        // the last explored node should be final node 
        //Node finalNode = exploredStack.Pop();
        // construct a queue of node base on final node: reverse linked list
        Node pointerNode = finalNode;
        while (pointerNode.prevNode != null)
        {
            nodeReplayQueue.Enqueue(pointerNode);
            pointerNode = pointerNode.prevNode;
        }
        nodeReplayQueue.Enqueue(pointerNode);
        nodeReplayQueue = new Queue<Node>(nodeReplayQueue.Reverse());
        
        //nodeReplayQueue.Dequeue().PrintNode();
        return nodeReplayQueue;
    }

    private float GetHeuristic()
    {
        float heuristic = Vector2.Distance(simPlayer.transform.position, star.transform.position);
        return (float)Math.Round(heuristic, 2);
    }
    
    // get sim player runtime state
    // !!! Need to add CanDash
    public State GetSimPlayerState(Action action)
    {
        bool j = false;
        bool d = false;
        bool c = false;

        if (action.Equals(Jump)) j = true;
        if (action.Equals(DashR) || action.Equals(DashUR) || action.Equals(DashU) || action.Equals(DashUL) || action.Equals(DashU)) d = true;

        return new State(simPlayer.transform.position, simPlayerRB.velocity, j, d, c);
    }

    public class Node {
        // One of the possible action types
        public State state;
        public Node prevNode;
        public float heuristic;
        public float costToGetHere;
        public Action action;
        
        public Node(State mState, Node prev, float heu, float cost) {
            state = mState;
            prevNode = prev;
            heuristic = heu;
            costToGetHere = cost;
        }
        
        public Node(State mState, Node prev, float heu, float cost, Action act) {
            state = mState;
            prevNode = prev;
            heuristic = heu;
            costToGetHere = cost;
            action = act;
        }

        public void PrintNode()
        {
            state.print();
            //Debug.Log("heuristic is " + heuristic);
            
        }
    }

    public IEnumerator ReplayFromNodeActions() {
        isReplaying = true;
        //print("Replay started with " + actionReplayQueue.Count + " actions");

        // Replay as long as there is something to replay in the Queue.
        while (nodeReplayQueue.Count > 0) {
            print("Replay started with " + nodeReplayQueue.Count + " node");
            Action act = nodeReplayQueue.Dequeue().action;
            TakeAction(act, playerMovement);
            yield return null;
        }

        isReplaying = false;
        replayDone = true;
    }

    public IEnumerator ReplayFromNode() {
        isReplaying = true;
        //print("Replay started with " + actionReplayQueue.Count + " actions");
        // Replay as long as there is something to replay in the Queue.
        while (nodeReplayQueue.Count > 0) {
            print("Replay started with " + nodeReplayQueue.Count + " node");
            State currReplayState = nodeReplayQueue.Dequeue().state;
            UpdateMainPlayer(currReplayState);
            yield return WaitFixedUpdate;
        }
        
        isReplaying = false;
        replayDone = true;
    }

    
    void UpdateMainPlayer(State currState) {
        mainPlayer.transform.position = currState.madelinePos;
        //mainPlayerRB.velocity = currState.madelineVel;
        // set the flags here as well.
    }

    void RestoreState(State state){
        //sets the current Unity state to the state
        simPlayer.transform.position = state.madelinePos;
        simPlayerRB.velocity = state.madelineVel;

        //jumped = j;
        //dashed = d;
        //climbing = c;
    }
    
    // dated replay code
    public IEnumerator Replay()
    {
        isReplaying = true;
        //print("Replay started with " + actionReplayQueue.Count + " actions");

        // Replay as long as there is something to replay in the Queue.
        while (actionReplayQueue.Count > 0)
        {

            // If there is no action that is being taken (because an action can be multiple frames)
            // pop the next action from the queue, and do it on the main player.
            if (activeAction == null)
            {
                Action act = actionReplayQueue.Dequeue();
                print("Action: " + act.actionType.ToString() + " has been taken and " + actionReplayQueue.Count + " remain.");
                TakeAction(act, playerMovement);

                // This line is possibly wrong! Might need to wait for end of fixed update.
                // yield return WaitEndOfFrame;
            }
            else
            {
                print("Action already active");
                // This line is possibly wrong! Might need to wait for end of fixed update.
                yield return WaitEndOfFrame;
            }
        }
        isReplaying = false;
        replayDone = true;
    }

    Coroutine activeAction = null;
    // Given an action decides how to make the player game object execute it.
    void TakeAction(Action action, Movement agentMovement )
    {
        // StartCoroutine returns an Coroutine object. We use that to indiciate an action is underway.
        switch (action.actionType)
        {
            case ActionType.WalkL:
                //activeAction = StartCoroutine(ExecuteWalkForNFrames(-1, action.modifier, agentMovement));
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
                Vector2 dir = DashDirectionDict[action.modifier];
                //activeAction = StartCoroutine(ExecuteDashForNFrames(dir, 5, agentMovement));
                SimulateDashForNFrames(dir, 5, agentMovement);
                
                break;
        }
    }

    // The three different actions that are slightly different.
    IEnumerator ExecuteWalkForNFrames(int dir, int frameCount, Movement agentMovement)
    {
        for (int i = 0; i < frameCount; i++)
        {
            Vector2 walkDir = new Vector2(dir, 0);
            agentMovement.Walk(walkDir);
            // Debug.Log(i);

            // This line might be problematic!
            yield return WaitFixedUpdate;

            //yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
        activeAction = null;
        yield return null;
    }


    IEnumerator ExecuteJumpForNFrames(int frameCount, Movement agentMovement)
    {
        for (int i = 0; i < frameCount; i++)
        {
            if (!agentMovement.wallGrab)
            {
                agentMovement.Jump(Vector2.up, false);
            }
            else
            {
                agentMovement.WallJump();
            }
            yield return WaitFixedUpdate;
        }
        activeAction = null;
        yield return null;
    }

    IEnumerator ExecuteDashForNFrames(Vector2 dir, int frameCount, Movement agentMovement)
    {
        agentMovement.Dash(dir.x, dir.y);
        for (int i = 0; i < frameCount; i++)
        {
            yield return WaitFixedUpdate;
        }
        activeAction = null;
        yield return null;
    }
    
    void SimulateDashForNFrames(Vector2 dir, int frameCount, Movement agentMovement)
    {
        agentMovement.Dash(dir.x, dir.y);
        for (int i = 0; i < frameCount; i++)
        {
            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }
    void SimulateWalkForNFrames(int dir, int frameCount, Movement agentMovement)
    {
        for (int i = 0; i < frameCount; i++)
        {
            Vector2 walkDir = new Vector2(dir, 0);
            agentMovement.Walk(walkDir);
            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }
    
    
    void SimulateJumpForNFrames(int frameCount, Movement agentMovement)
    {
        for (int i = 0; i < frameCount; i++)
        {
            if (!agentMovement.wallGrab)
            {
                agentMovement.Jump(Vector2.up, false);
            }
            else
            {
                agentMovement.WallJump();
            }
            simPhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }
 }
