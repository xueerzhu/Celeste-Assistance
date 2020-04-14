public partial class SearchandReplay {
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
}