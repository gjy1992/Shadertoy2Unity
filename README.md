# Shadertoy2Unity
Convert most shadertoy's shader to Unity unlit shader.

This program can convert most shaders on shadertoy to Unity unlit shaders. The macros for conditional compiling are converted to shader_feature in Unity. Some built-in variables of sahdertoy can be set both from shader param or from Unity built-in variable. The corresponding shader_feature decide which one to use. For example, *iTime* in shadertoy will generate a shader_feature named "USE_BUILTIN_TIME". When this keyword is enabled from the material, the value of Unity built-in time is used for *iTime*. If the keywords is not enabled, the value of *iTime* is set from the shader parameter.

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
3. Operator "\*" in GLSL mean linear algebra, where in Cg is component-wise operation. What's more, Cg doesn't allow a float 4*4 multiply a float4 using "\*". Perfect comversion needs type analysis.