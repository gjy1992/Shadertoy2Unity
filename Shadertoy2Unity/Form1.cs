﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Shadertoy2Unity
{
    public partial class Form1 : Form
    {
        bool success = true;
        string errorMSG = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "Convert")
            {
                button1.Text = "Reset";
                string res = Convert(textBox1.Text);
                textBox1.Text = Format(res);
                textBox1.ReadOnly = true;
                Clipboard.SetText(textBox1.Text);
            }
            else
            {
                button1.Text = "Convert";
                textBox1.ReadOnly = false;
                textBox1.Text = "";
            }
        }

        Regex word = new Regex(@"\w+", RegexOptions.Compiled);
        Regex macroSearch = new Regex(@"^\s*#define\s+(\w+)\s*(\w*)$", RegexOptions.Multiline | RegexOptions.Compiled);
        Regex titlecomment = new Regex(@"^(\s*//.*\n+)+", RegexOptions.Compiled);
        
        Regex functions = new Regex(@"\b(vec2|vec3|vec4|mod|fract|mix|texture|textureLod|inversesqrt|textureProj|textureSize|dFdx|dFdy)\b", RegexOptions.Compiled);
        Dictionary<string, string> functionsMap = new Dictionary<string, string> {
            { "vec2", "float2"},
            { "vec3", "float3"},
            { "vec4", "float4"},
            { "mod", "fmod"},
            { "fract", "frac"},
            { "mix", "lerp"},
            { "inversesqrt", "rsqrt"},
            { "texture", "tex2D"},
            { "textureLod", "tex2Dlod"},
            { "textureLodOffset", "tex2Dlod"},
            { "textureGrad", "tex2D"},
            { "textureGradOffset", "tex2D"},
            { "textureProj", "tex2Dproj"},
//textureProjLod
//textureProjLodOffset
//textureProjGrad
            { "texelFetch", "tex2Dfetch"},
            { "texelFetchOffset", "tex2Dfetch"},
            { "textureSize", "tex2Dsize"},
//outerProduct
            { "dFdx", "ddx"},
            { "dFdy", "ddy"},
//uintBitsToFloat
//floatBitsToUint
//packSnorm2x16
//packUnorm2x16
//unpackSnorm2x16
//unpackUnorm2x16
        };
        //These regexp cannot handle nested case, but fortunately these functions usually aren't nested.
        Regex floatN = new Regex(@"\b(float2|float3|float4)\b\s*(\(([^(),]*(\([^)]*?\))*)*\))", RegexOptions.Singleline | RegexOptions.Compiled);
        Regex compare = new Regex(@"\b(lessThan|lessThanEqual|greaterThan|greaterThanEqual|equal|notEqual|matrixCompMult)\b\s*\(((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*)\)",
            RegexOptions.Singleline | RegexOptions.Compiled);
        Regex texFunc3 = new Regex(@"\b(tex2Dlod|tex2Dfetch)\b\s*\(((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*)\)",
            RegexOptions.Singleline | RegexOptions.Compiled);
        Regex texFunc4 = new Regex(@"\b(tex2Dlod|tex2Dfetch)\b\s*\(((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*)\),\s*((?:[^(),]*(?:\([^)]*?\))*)*)",
            RegexOptions.Singleline | RegexOptions.Compiled);
        Regex texFunc5 = new Regex(@"\b(tex2D)\b\s*\(((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*),\s*((?:[^(),]*(?:\([^)]*?\))*)*)\),\s*((?:[^(),]*(?:\([^)]*?\))*)*,\s*((?:[^(),]*(?:\([^)]*?\))*)*)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        Regex iTime = new Regex(@"\biTime\b", RegexOptions.Compiled);
        Regex iTimeDelta = new Regex(@"\biTimeDelta\b", RegexOptions.Compiled);
        Regex iFrame = new Regex(@"\biFrame\b", RegexOptions.Compiled);
        Regex iFrameRate = new Regex(@"\biFrameRate\b", RegexOptions.Compiled);
        Regex iChannelTime = new Regex(@"\biChannelTime\b\[(.*?)\]", RegexOptions.Compiled);
        Regex iChannelResolution = new Regex(@"\biChannelResolution\b\[(.*?)\]", RegexOptions.Compiled);
        Regex iMouse = new Regex(@"\biMouse\b", RegexOptions.Compiled);
        Regex iChannel0 = new Regex(@"\biChannel0\b", RegexOptions.Compiled);
        Regex iChannel1 = new Regex(@"\biChannel1\b", RegexOptions.Compiled);
        Regex iChannel2 = new Regex(@"\biChannel2\b", RegexOptions.Compiled);
        Regex iChannel3 = new Regex(@"\biChannel3\b", RegexOptions.Compiled);
        Regex iDate = new Regex(@"\biDate\b", RegexOptions.Compiled);
        Regex iSampleRate = new Regex(@"\biSampleRate\b", RegexOptions.Compiled);

        //Regex fragColor = new Regex(@"\bfragColor\b\s*=\s*", RegexOptions.Compiled);
        Regex mainImage = new Regex(@"^\s*void\s+mainImage\s*\(\s*out\s+float4\s+fragColor\s*,\s*in\s+float2\s+fragCoord\s*\).*?\{",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline);

        string Convert(string input)
        {
            success = true;
            string res = input;
            List<string> shaderFeatures = new List<string>();
            int feature_startpos = -1;
            //search macro
            var matches = macroSearch.Matches(input);
            foreach(Match m in matches)
            {
                if(m.Success)
                {
                    string s2 = m.Groups[2].Value;
                    if (s2 == "")
                    {
                        //this is a feature macro
                        if (feature_startpos < 0)
                            feature_startpos = m.Index;
                        res = res.Remove(m.Index, m.Length);
                        if (res.Substring(m.Index).StartsWith("\r\n"))
                            res = res.Remove(m.Index, 2);
                        else if (res.Length > m.Index && (res[m.Index] == '\r' || res[m.Index] == '\n'))
                            res = res.Remove(m.Index, 1);
                        shaderFeatures.Add(m.Groups[1].Value);
                    }
                }
            }

            //replace function name
            res = functions.Replace(res, (Match m) =>
            {
                if(m.Success && m.Length > 0)
                {
                    return functionsMap[m.Value];
                }
                return "";
            });
            res = floatN.Replace(res, (Match m) =>
            {
                if (m.Success && m.Length > 0)
                {
                    string fname = m.Groups[1].Value;
                    string content = m.Groups[2].Value;
                    switch (fname)
                    {
                        case "float2":
                            return content + ".xx";
                        case "float3":
                            return content + ".xxx";
                        case "float4":
                            return content + ".xxxx";
                    }
                }
                return "";
            });
            {
                //recursive replace
                while (true)
                {
                    int count = 0;
                    res = compare.Replace(res, (Match m) =>
                    {
                        count++;
                        if (m.Success && m.Length > 0)
                        {
                            string fname = m.Groups[1].Value;
                            string content1 = m.Groups[2].Value;
                            string content2 = m.Groups[3].Value;
                            switch (fname)
                            {
                                case "lessThan":
                                    return "(" + content1 + ")<(" + content2 + ")";
                                case "lessThanEqual":
                                    return "(" + content1 + ")<=(" + content2 + ")";
                                case "greaterThan":
                                    return "(" + content1 + ")>(" + content2 + ")";
                                case "greaterThanEqual":
                                    return "(" + content1 + ")>=(" + content2 + ")";
                                case "equal":
                                    return "(" + content1 + ")==(" + content2 + ")";
                                case "notEqual":
                                    return "(" + content1 + ")!=(" + content2 + ")";
                                case "matrixCompMult":
                                    return "(" + content1 + ") * (" + content2 + ")";
                            }
                        }
                        return "";
                    });
                    if (count == 0)
                        break;
                }
            }
            //replace some functions with different signature
            {
                //recursive replace
                while(true)
                {
                    int count = 0;
                    res = texFunc3.Replace(res, (Match m) =>
                    {
                        count++;
                        if (m.Success && m.Length > 0)
                        {
                            string fname = m.Groups[1].Value;
                            string content1 = m.Groups[2].Value;
                            string content2 = m.Groups[3].Value;
                            string content3 = m.Groups[4].Value;
                            switch (fname)
                            {
                                case "tex2Dlod":
                                    return fname + "(" + content1 + ", float4(" + content2 + ", 1.0, " + content3 + "))";
                                case "tex2Dfetch":
                                    return fname + "(" + content1 + ", int4(" + content2 + ", 1.0, " + content3 + "))";
                            }
                        }
                        return "";
                    });
                    if (count == 0)
                        break;
                }
            }
            {
                //recursive replace
                while (true)
                {
                    int count = 0;
                    res = texFunc4.Replace(res, (Match m) =>
                    {
                        count++;
                        if (m.Success && m.Length > 0)
                        {
                            string fname = m.Groups[1].Value;
                            string content1 = m.Groups[2].Value;
                            string content2 = m.Groups[3].Value;
                            string content3 = m.Groups[4].Value;
                            string content4 = m.Groups[5].Value;
                            switch (fname)
                            {
                                case "tex2Dlod":
                                    return fname + "(" + content1 + ", int4((" + content2 + ") + (" + content4 + ")/tex2Dsize(" + content1 + ", 0).xy, 1.0, " + content3 + "))";
                                case "tex2Dfetch":
                                    return fname + "(" + content1 + ", int4((" + content2 + ") + (" + content4 + "), 1.0, " + content3 + "))";
                            }
                        }
                        return "";
                    });
                    if (count == 0)
                        break;
                }
            }
            {
                //recursive replace
                while (true)
                {
                    int count = 0;
                    res = texFunc5.Replace(res, (Match m) =>
                    {
                        count++;
                        if (m.Success && m.Length > 0)
                        {
                            string fname = m.Groups[1].Value;
                            string content1 = m.Groups[2].Value;
                            string content2 = m.Groups[3].Value;
                            string content3 = m.Groups[4].Value;
                            string content4 = m.Groups[5].Value;
                            string content5 = m.Groups[6].Value;
                            switch (fname)
                            {
                                case "tex2D":
                                    return fname + "(" + content1 + ", (" + content2 + ") + (" + content5 + ")/tex2Dsize(" + content1 + ", 0).xy, " + content3 + ", " + content4 + ")";
                            }
                        }
                        return "";
                    });
                    if (count == 0)
                        break;
                }
            }

            //generate shader property
            List<string> shaderProperty = new List<string>();
            List<string> shaderVariant = new List<string>();
            shaderProperty.Add("_MainTex (\"Texture\", 2D) = \"white\" {}");
            shaderVariant.Add("sampler2D _MainTex;");
            shaderVariant.Add("float4 _MainTex_ST;");
            shaderVariant.Add("float4 _MainTex_TexelSize;");

            //add shadertoy inputs as properties
            if(iTime.IsMatch(res))
            {
                //TODO: use feature to control whether use Unity built-in _Time or use shader property
                shaderFeatures.Add("USE_BUILTIN_TIME");
                shaderProperty.Add("iTime (\"Elapsed Time\", Float) = 0");
                shaderVariant.Add("float iTime;");
            }
            if (iTimeDelta.IsMatch(res))
            {
                //TODO: use feature to control whether use Unity built-in unity_DeltaTime or use shader property
                shaderFeatures.Add("USE_BUILTIN_DELTATIME");
                shaderProperty.Add("iTimeDelta (\"Delta Time\", Float) = 0.0167");
                shaderVariant.Add("float iTimeDelta;");
            }
            if (iFrame.IsMatch(res))
            {
                shaderProperty.Add("iFrame (\"Current Frame Index\", Int) = 0");
                shaderVariant.Add("int iFrame;");
            }
            if (iFrameRate.IsMatch(res))
            {
                //TODO: use feature to control whether use Unity built-in unity_DeltaTime or use shader property
                shaderFeatures.Add("USE_BUILTIN_FRAMERATE");
                shaderProperty.Add("iFrameRate (\"Frame Rate\", Float) = 60");
                shaderVariant.Add("float iFrameRate;");
            }
            if (iMouse.IsMatch(res))
            {
                shaderProperty.Add("iMouse (\"Mouse Position\", Vector) = (0, 0, 0, 0)");
                shaderVariant.Add("float4 iMouse;");
            }
            if (iDate.IsMatch(res))
            {
                shaderProperty.Add("iDate (\"Date (Year, Month, Day, Time in Seconds)\", Vector) = (2020, 1, 1, 1)");
                shaderVariant.Add("float iDate;");
            }
            if (iSampleRate.IsMatch(res))
            {
                shaderProperty.Add("iSampleRate (\"Sound SampleRate\", Float) = 44100");
                shaderVariant.Add("float iSampleRate;");
            }
            res = iChannel0.Replace(res, "_MainTex");
            if (iChannel1.IsMatch(res))
            {
                shaderProperty.Add("iChannel1 (\"Texture 1\", 2D) = \"white\" {}");
                shaderVariant.Add("sampler2D iChannel1;");
            }
            if (iChannel2.IsMatch(res))
            {
                shaderProperty.Add("iChannel2 (\"Texture 1\", 2D) = \"white\" {}");
                shaderVariant.Add("sampler2D iChannel2;");
            }
            if (iChannel3.IsMatch(res))
            {
                shaderProperty.Add("iChannel3 (\"Texture 1\", 2D) = \"white\" {}");
                shaderVariant.Add("sampler2D iChannel3;");
            }
            //TODO: handle iChannelTime and iChannelResolution

            //TODO: guess title from first comment line
            var npos = res.IndexOfAny(new char[]{ '\n', '\r' });
            if (npos < 0)
                npos = 0;
            var fline = res.Substring(0, npos).Trim();
            var title = "Default";
            if(fline.StartsWith(@"//"))
            {
                var m = word.Match(fline);
                if (m.Success)
                    title = m.Value;
            }

            //TODO: add prefix and suffix of Unity Shader
            string prefix = $"Shader \"Unlit/{title}\"\n";
            prefix += @"{
                Properties
                {
            ";
            prefix += string.Join("\n", shaderProperty);
            prefix += @"
                }
                SubShader
                {
                    Tags { ""RenderType""=""Opaque"" }
                    LOD 100

                    Blend One OneMinusSrcAlpha

                Pass
                {
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
          
                    #include ""UnityCG.cginc""

			        struct appdata
                    {
                        float4 vertex : POSITION;
                        float2 uv : TEXCOORD0;
                    };

                    struct v2f
                    {
                        float2 uv : TEXCOORD0;
                        //UNITY_FOG_COORDS(1)
                        float4 vertex : SV_POSITION;
                    };

            ";
            //Generate features
            if (feature_startpos >= 0)
            {
                foreach(var f in shaderFeatures)
                {
                    prefix += "#pragma shader_feature " + f + "\n";
                }
                prefix += '\n';
            }
            prefix += string.Join("\n", shaderVariant);
            prefix += @"
                v2f vert (appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                    return o;
                }
            ";
            string suffix = @"
                        ENDCG
                    }
                }
            }
            ";

            //change mainFunction and fragColor
            var mainFunc = mainImage.Match(res);
            if(mainFunc.Success)
            {
                string main = "fixed4 frag (v2f i) : SV_Target\n{\n fixed4 fragColor;\n float3 iResolution = float3(_MainTex_TexelSize.zw, 1.0);\nfloat2 fragCoord = i.uv * iResolution.xy;";
                res = res.Substring(0, mainFunc.Index) + main + res.Substring(mainFunc.Index + mainFunc.Length);
            }
            //res = fragColor.Replace(res, "return ");

            var resfuncs = SplitFunctions(res);
            for (int i = 0; i < resfuncs.Count; i++)
            {
                var funcs = resfuncs[i];
                if (funcs.StartsWith("{"))
                {
                    funcs = AddVariableAssignment(funcs);
                }
                if (funcs.EndsWith("fixed4 frag (v2f i) : SV_Target\n"))
                {
                    resfuncs[i + 1] = resfuncs[i + 1].Substring(0, resfuncs[i + 1].Length - 1) + "\nreturn fragColor;\n}";
                }
            }
            res = string.Join("", resfuncs);

            //move begin comment to start
            string comment = "";
            {
                Match m = titlecomment.Match(res);
                if(m.Success)
                {
                    comment = m.Value;
                    res = res.Remove(m.Index, m.Length);
                }
            }

            res = comment + prefix + res + suffix;
            return res;
        }



        string AddVariableAssignment(string input)
        {
            string extra = "\n";
            if (iTime.IsMatch(input))
            {
                extra += @"#ifdef USE_BUILTIN_TIME
                    iTime=_Time.y;
                    ";
            }
            if (iTimeDelta.IsMatch(input))
            {
                extra += @"#ifdef USE_BUILTIN_DELTATIME
                    iTime=unity_DeltaTime.x;
                    ";
            }
            if (iFrameRate.IsMatch(input))
            {
                //use 1/smoothDt
                extra += @"#ifdef USE_BUILTIN_FRAMERATE
                    iFrameRate=unity_DeltaTime.w;
                    ";
            }
            return "{" + extra + input.Substring(1);
        }

        List<string> SplitFunctions(string input)
        {
            List<string> res = new List<string>();
            int start = 0;
            while(true)
            {
                int spos = input.IndexOf('{', start);
                if (spos < 0)
                    break;
                int p = 1;
                int epos = spos + 1;
                while(true)
                {
                    epos = input.IndexOfAny(new char[] { '{', '}' }, epos);
                    if(epos < 0)
                    {
                        break;
                    }
                    if (input[epos] == '{')
                        p++;
                    else if (input[epos] == '}')
                        p--;
                    if (p == 0)
                        break;
                    epos++;
                }
                if (spos < 0)
                    break;
                res.Add(input.Substring(start, spos - start));
                if (epos < 0)
                {
                    start = spos + 1;
                    break;
                }
                res.Add(input.Substring(spos, epos + 1 - spos));
                start = epos + 1;
            }
            res.Add(input.Substring(start));
            return res;
        }

        string Format(string input)
        {
            string[] texts = input.Replace("\r\n","\n").Replace("\r", "\n").Split('\n');
            string tab = "";
            for (int i = 0; i < texts.Length; i++)
            {
                var t = texts[i].Trim();
                if (t.StartsWith("//"))
                {
                    t = tab + t;
                }
                else if (t.StartsWith("}"))
                {
                    if (tab.Length > 0)
                        tab = tab.Remove(tab.Length - 1);
                    t = tab + t;
                    if (t.EndsWith("{"))
                        tab += "\t";
                }
                else
                {
                    t = tab + t;
                    int p = t.Split('{').Length - 1;
                    int n = t.Split('}').Length - 1;
                    p -= n;
                    if (p > 0)
                    {
                        while (p-- > 0)
                            tab += '\t';
                    }
                    else if (p < 0)
                    {
                        if (p + tab.Length >= 0)
                            tab = tab.Remove(tab.Length + p);
                        else
                            tab = "";
                    }
                }
                texts[i] = t;
            }
            return string.Join("\r\n", texts);
        }
    }
}