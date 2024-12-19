namespace Volumetric.VSys;

public class VSysNode {
    // "scene"
    public static VSysNode[] Nodes = [];

    // linked tree format
    public VSysNode[] children;
    public VSysNode parent;
    
    public VSysNodeData data;

    // avoiding expensive searching
    public int NodeID { get; private set; }
    public int SiblingNodeID { get; private set; }

    public VSysNode(VSysNodeData data, VSysNode parent) {
        this.data = data;
        this.parent = parent;

        // tying multiple outputs to multiple inputs might be a thing later, but tbh it's
        //    v messy and i don't want to implement that
        for (int i = 0; i < parent.data.inputs.Length; i++) {
            if (parent.data.inputs[i] == data.output)
                SiblingNodeID = i;
        }

        children = [];

        NodeID = Nodes.Length;

        _ = Nodes.Append(this);
    }

    public static VSysNode GetNodeByID(int id) => Nodes[id];

    public VSysNode GetChildNodeByID(int childID) => children[childID];
}
