using System.Numerics;
namespace Volumetric.VSys;

public class VSysNode {
    // "scene"
    public static VSysNode[] Nodes = [];

    // linked tree format
    public VSysNode?[] children;
    public Vector4[] numInputs;
    public VSysNode parent;
    
    public string name;
    public string InternalName { get {
        if (parent is VSysRootNode)
            return name;

        return parent.InternalName + "_" + name;
    } }

    public VSysNodeType data;

    // avoiding expensive searching
    public int NodeID { get; private set; }
    public int SiblingNodeID { get; private set; }

    public VSysNode(string name, VSysNodeType data, VSysNode? parent, int siblingNodeID) {
        this.data = data;
        this.parent = parent;
        this.name = name;
        SiblingNodeID = siblingNodeID;

        if (parent != null && !VSysIOSanityCheck(parent, this, siblingNodeID)) return;

        children = new VSysNode[data.inputs.Length];
        numInputs = new Vector4[data.inputs.Length];

        NodeID = Nodes.Length;

        _ = Nodes.Append(this);
    }

    static bool VSysIOSanityCheck(VSysNode parent, VSysNode child, int siblingNodeID) {
        if (siblingNodeID >= parent.data.inputs.Length)
            throw new IndexOutOfRangeException("Invalid sibling placement.");

        // tying multiple outputs to multiple inputs might be a thing later, but tbh it's
        //    v messy and i don't want to implement that
        if (parent.data.inputs[siblingNodeID] != child.data.output)
            throw new ArrayTypeMismatchException(
                "Target input field (" + parent.data.inputs[siblingNodeID] + ") does not match created node's output field (" + child.data.output + ")"
            );

        return true;
    }

    /// <summary>
    /// Creates a new Volumetric geometry node.
    /// </summary>
    /// <param name="type">The type of node to be created.</param>
    /// <param name="parentTarget">The first parent of the node.</param>
    /// <returns>The created node.</returns>
    public static VSysNode CreateNodeOn(string name, VSysNodeType type, VSysNode parentTarget, int siblingNodeID) => 
        new(name, type, parentTarget, siblingNodeID);

    /// <summary>
    /// Deletes the node. Note that this will delete all children nodes as well if they are not referenced by anything else.
    /// </summary>
    public void DeleteNode() =>
        parent.children.SetValue(null, SiblingNodeID);

    public static void MoveNode(VSysNode target, VSysNode newParent, int siblingNodeID) =>
        target.MoveNode(newParent, siblingNodeID);

    public void MoveNode(VSysNode newParent, int siblingNodeID) {
        if (!VSysIOSanityCheck(newParent, this, siblingNodeID)) return;
        if (FindNode(newParent.NodeID, out _)) return;

        parent.children.SetValue(null, SiblingNodeID);

        parent = newParent;
        SiblingNodeID = siblingNodeID;
    }

    public void MoveNode(int siblingNodeID) => 
        MoveNode(parent, siblingNodeID);

    public bool FindNode(int id, out VSysNode? target) {
        target = null;

        if (id != NodeID) {
            for (int i = 0; i < children.Length; i++) {
                // compiler's yapping about a possible null deref, despite me determining it's not a deref
                if (children[i] != null && children[i].FindNode(id, out VSysNode? found)) {
                    target = found;
                    return true;
                }
            }
        }

        return false;
    }

    public static VSysNode GetNodeByID(int id) => 
        Nodes[id];

    public VSysNode? GetChildNodeByID(int childID) => 
        children[childID];
}

public class VSysRootNode : VSysNode {
    public VSysRootNode() : base("Scene", new([VSysNodeType.VSysNodeIOType.@float], VSysNodeType.VSysNodeIOType.root, "return m"), null, 0) { }

    new public string InternalName { get { return ""; } }

    new public void DeleteNode() =>
        throw new Exception("Cannot delete root node.");

    new public void MoveNode(VSysNode newParent, int siblingNodeID) => MoveNode(siblingNodeID);

    new public void MoveNode(int siblingNodeID) => 
        throw new Exception("Cannot move root node.");
}