using System.Text.RegularExpressions;

namespace Volumetric.VSys;

public struct VSysNodeType {
    public enum VSysNodeIOType {
        @float, // Distance
        vec2,   // Direction
        vec3,   // Normal
        vec4,   // Colour
        root    // None
    };

    // io & shit
    public VSysNodeIOType[] inputs;
    public VSysNodeIOType output;

    // code / symbols
    public string Code { get; private set; }
    public string Symbol { get; private set; } // TODO: generate this at runtime

    public VSysNodeType(VSysNodeIOType[] inputs, VSysNodeIOType output, string code) {
        this.inputs = inputs;
        this.output = output;

        Code = code;

        string symPattern = @".*?{";

        Regex symReg = new(symPattern);

        Match symMatch = symReg.Match(Code);

        string[] symArgs = [];

        //#region sanitisation
        if (symMatch.Success) {
            Symbol = symMatch.Groups[0].Value;
            Console.WriteLine(Symbol);
            
            symArgs = symMatch.Groups[1].Value.Split(",");
        }
        else
            throw new Exception("Failed to find GLSL symbol in the following code:\n" + Code);
        
        if (symArgs.Length != inputs.Length) 
            throw new Exception("GLSL symbol arguments do not match provided input data (" + string.Join(" ", inputs) + "):\n" + Code);

        for (int i = 0; i < symArgs.Length; i++) {
            symArgs[i] = symArgs[i].Trim().Split(" ")[0];

            if (symArgs[i] != inputs[i].ToString())
                throw new Exception("GLSL symbol arguments do not match provided input data (" + string.Join(" ", inputs) + "):\n" + Code);
        }
        //#endregion
    }
}