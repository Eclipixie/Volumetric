using System.Text.RegularExpressions;

namespace Volumetric.VSys;

public struct VSysNodeData {
    public enum VSysNodeIOType {
        Float, // Distance
        Vec2,  // Direction
        Vec3,  // Normal
        Vec4   // Colour
    };

    // io & shit
    public VSysNodeIOType[] inputs;
    public VSysNodeIOType output;

    // code / symbols
    public string Code { get; private set; }
    public string Symbol { get; private set; } // TODO: generate this at runtime

    public VSysNodeData(VSysNodeIOType[] inputs, VSysNodeIOType output, VSysNode parent, string code) {
        this.inputs = inputs;
        this.output = output;

        Code = code;

        string symPattern = @".*?{";

        Regex symReg = new(symPattern);

        Match symMatch = symReg.Match(Code);

        //#region sanitisation
        if (symMatch.Success) {
            Symbol = symMatch.Groups[0].Value;
            Console.WriteLine(Symbol);
        }
        else
            throw new Exception("Failed to find GLSL symbol in the following code:\n" + Code);
        //#endregion
    }
}
