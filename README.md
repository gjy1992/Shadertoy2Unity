# Shadertoy2Unity
Convert most shadertoy's shader to Unity unlit shader.

This program can convert most shaders on shadertoy to Unity unlit shaders. The macros for conditional compiling are converted to shader_feature in Unity.

Current unsupported functions:
<li>outerProduct
<li>textureProjLod
<li>textureProjLodOffset
<li>textureProjGrad
<li>uintBitsToFloat
<li>floatBitsToUint
<li>packSnorm2x16
<li>packUnorm2x16
<li>unpackSnorm2x16
<li>unpackUnorm2x16

And there is some another problems: <br>
1. When vec2, vec3, vec4 is used with one parameter, it may be narrow convertion or wide convertion. However, Unity doesn't allow these convertion. In this program, we suppose all these calls are wide conversion and change vec4(value) to (value).xxxx. So the result for narrow conversion case will be incorrect.
2. All structs are not checked or converted. There may be some problems.