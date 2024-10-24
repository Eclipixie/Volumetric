#version 330 core
out vec4 FragColor;

in vec2 uv;

uniform float aspect;

float maxDist = 256.;
int maxIter = 256;
float epsilon = 0.01;

// #region SDFs
float sdf_sphere(vec3 p, float r) {
    return sqrt(pow(p.x,2) + pow(p.y,2) + pow(p.z,2)) - r;
}
// #endregion

vec4 gmt_map(vec3 v) {
    return vec4(1, 1, 1, sdf_sphere(vec3(0,0,5) - v, 2));
}

vec3 gmt_normal( in vec3 p ) {
    const float h = 0.0001; // replace by an appropriate value
    const vec2 k = vec2(1,-1);
    return normalize( k.xyy*gmt_map( p + k.xyy*h ).w + 
                      k.yyx*gmt_map( p + k.yyx*h ).w + 
                      k.yxy*gmt_map( p + k.yxy*h ).w + 
                      k.xxx*gmt_map( p + k.xxx*h ).w );
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

    gmt_col =  vec3(4.0 *  dif * (gmt_col.xyz));
    gmt_col += vec3(12.0 * spe * (gmt_col.xyz));
    
    // ambient light
    float occ = fx_ao( r, nor );
    float amb = clamp( 0.5+0.5*nor.y, 0.0, 1.0 );
    gmt_col += vec3(amb*occ*vec3(0.0,0.08,0.1));

    gmt_col=mix(gmt_col, sky, min(travel/maxDist, 1));

    float t = travel / maxDist;
    FragColor = vec4((1-t)*gmt_col + t*sky, 1);
    // FragColor = vec4(nor, 1);
}