using static Volumetric.VSys.VSysNodeType;
namespace Volumetric.VSys;

public static class VSys {
    public static VSysNodeType[] NodeRegistry { get => nodeRegistry; }
    static readonly VSysNodeType[] nodeRegistry = {
        new([VSysNodeIOType.vec3, VSysNodeIOType.@float], VSysNodeIOType.@float, """
        float sdf_sphere(vec3 p, float r) {
            return sqrt(pow(p.x,2) + pow(p.y,2) + pow(p.z,2)) - r;
        }
        """)
    };

    public static string GenCompile(bool autoNormals) {
        // higher generation nodes (more relations between subject and root) go at the bottom, on a depth-first basis

        // todo: inject custom values for max distance, iterations, epsilon value etc
        string topLevel = """
        #version 330 core
        out vec4 FragColor;

        in vec2 uv;

        uniform float aspect;

        float maxDist = 256.;
        int maxIter = 256;
        float epsilon = 0.01;
        """;

        string srcSymbols = "";

        for (int i = 0; i < nodeRegistry.Length; i++)
            srcSymbols += nodeRegistry[i].Code;

        string mapString = "float gmt_dist_map(vec3 v) {";
        mapString += CompileSymbols(VSysNode.Nodes[0]);
        mapString += "}\n\n";

        string engine = """
        vec3 gmt_normal( in vec3 p ) // for function f(p)
        {
            const float h = 0.0001; // replace by an appropriate value
            const vec2 k = vec2(1,-1);
            return normalize(
                k.xyy*gmt_map( p + k.xyy*h ).w + 
                k.yyx*gmt_map( p + k.yyx*h ).w + 
                k.yxy*gmt_map( p + k.yxy*h ).w + 
                k.xxx*gmt_map( p + k.xxx*h ).w
            );
        }
        
        // #region Lighting
        // shadows
        float fx_shadow(vec3 ro, vec3 rd, float mint, float maxt, float w) {
            float res = 1.0;
            float t = epsilon;
            for( int i=0; i < maxIter && t < maxDist; i++ ) {
                float h = gmt_map(ro + t*rd).w;
                res = min( res, h / (w*t) );
                t += clamp(h, 0.005, 0.50);
                if( res < -1.0 || t > maxDist ) break;
            }
            res = max(res,-1.0);
            return 0.25*(1.0+res)*(1.0+res)*(2.0-res);
        }

        // ambient occlusion
        float fx_ao( in vec3 pos, in vec3 nor ) {
            float occ = 0.0;
            float sca = 1.0;
            for( int i=0; i < 5; i++ ) {
                float h = 0.001 + 0.15 * float(i) / 4.0;
                float d = gmt_map( pos + h*nor ).w;
                occ += (h-d) * sca;
                sca *= 0.95;
            }
            return clamp( 1.0 - 1.5 * occ, 0.0, 1.0 );    
        }
        // #endregion

        void main() {
            vec3 o = vec3(0.);
            float travel = 0;
            vec3 dir = normalize(vec3(uv.x*aspect, uv.y, 1.));

            vec3 gmt_col = vec3(0,0,0);

            for (int i = 0; i < maxIter; i++) {
                vec4 data = gmt_map(o + (travel * dir));
                gmt_col = data.xyz;
                float d = data.w;
                travel += d;

                if (d <= epsilon || travel >= maxDist) break;
            }

            vec3 sky = vec3(.3);

            vec3 r = o + (travel * dir);
            vec3 nor = gmt_normal(r);

            // key light
            vec3  lig = normalize( vec3(-0.1,  0.6,  -0.3) );
            vec3  hal = normalize( lig - dir );
            float shad = fx_shadow( r, lig, 0.01, 300.0, 0.1 );
            float dif = clamp( dot( nor, lig ), 0.0, 1.0 ) * shad;

            float spe = pow( clamp( dot( nor, hal ), 0.0, 1.0 ),16.0)*
                        dif *
                        (0.04 + 0.96*pow( clamp(1.0+dot(hal,dir),0.0,1.0), 5.0 ));

            // used to be multiplied by 4
            // weird shading on curved surfaces
            gmt_col =  vec3(1.2 * dif * gmt_col);
            gmt_col += vec3(12.0 * spe * (gmt_col.xyz));
            
            // ambient light
            float occ = fx_ao( r, nor );
            float amb = 1;//clamp( 0.5+0.5*nor.y, 0.0, 1.0 );
            gmt_col += vec3(amb*occ*vec3(0.0,0.08,0.1));

            gmt_col=mix(gmt_col, sky, min(travel/maxDist, 1));

            float t = travel / maxDist;
            FragColor = vec4((1-t)*gmt_col + t*sky, 1);
            // FragColor = vec4(nor, 1);
        }
        """;

        return srcSymbols + mapString + engine;
    }

    static string CompileSymbols(VSysNode node, string compiled = "") {
        string symbol;

        if (node.data.output == VSysNodeIOType.root) {
            // return_
            symbol = "return ";
            // assign required variables before usage
            if (node.children[0] != null)
                compiled = CompileSymbols(node.children[0], compiled);
            else // vec4 foo = bar(vec4(0, 1, 2, 3)
                symbol += CompileSymArg(node, 0);

            return compiled + symbol;
        }

        // vec4_
        symbol = node.data.output.ToString() + " ";
        // vec4 foo =_
        symbol += node.InternalName + " = ";
        // vec4 foo = bar(
        symbol += node.data.Symbol + "(";

        for (int i = 0; i < node.children.Length; i++) {
            // assign required variables before usage
            if (node.children[i] != null)
                compiled = CompileSymbols(node.children[i], compiled);
            else // vec4 foo = bar(vec4(0, 1, 2, 3)
                symbol += CompileSymArg(node, i);

            // vec4 foo = bar(vec4(0, 1, 2, 3),_
            symbol += ", ";
        }

        // vec4 foo = bar(vec4(0, 1, 2, 3)
        symbol = symbol.Remove(symbol.Length - 2);
        // vec4 foo = bar(vec4(0, 1, 2, 3));
        symbol += ");\n";

        return compiled + symbol;
    }

    static string CompileSymArg(VSysNode node, int childID) {
        string symbol = "";
        // vec4 foo = bar(vec4(
        if (node.data.inputs[childID] != VSysNodeIOType.@float)
            symbol += node.data.inputs[childID].ToString() + "(";
        
        // ffs
        // vec4 foo = bar(vec4(0, 1, 2, 3
        switch (node.data.inputs[childID]) {
            case VSysNodeIOType.@float:
                symbol += node.numInputs[childID].X;
                break;
            case VSysNodeIOType.vec2:
                symbol += node.numInputs[childID].X + ", ";
                symbol += node.numInputs[childID].Y;
                break;
            case VSysNodeIOType.vec3:
                symbol += node.numInputs[childID].X + ", ";
                symbol += node.numInputs[childID].Y + ", ";
                symbol += node.numInputs[childID].Z;
                break;
            case VSysNodeIOType.vec4:
                symbol += node.numInputs[childID].X + ", ";
                symbol += node.numInputs[childID].Y + ", ";
                symbol += node.numInputs[childID].Z + ", ";
                symbol += node.numInputs[childID].W;
                break;
            case VSysNodeIOType.root:
            default: 
                throw new Exception("How on fucking earth did you manage to do that?");
        }
        
        // vec4 foo = bar(vec4(0, 1, 2, 3)
        if (node.data.inputs[childID] != VSysNodeIOType.@float) 
            symbol += node.data.inputs[childID].ToString() + ")";
        
        return symbol;
    }
}
