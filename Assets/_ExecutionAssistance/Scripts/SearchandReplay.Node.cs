public partial class SearchandReplay {
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

        public void PrintNode() {
            state.print();
            //Debug.Log("heuristic is " + heuristic);
        }
    }
}