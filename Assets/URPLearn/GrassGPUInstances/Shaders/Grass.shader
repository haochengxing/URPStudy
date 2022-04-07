Shader "URPLearn/Grass"
{
    Properties
    {
        [MainTexture]_BaseMap("Albedo",2D) = "white"{}
        [MainColor]_BaseColor("Color",Color)=(1,1,1,1)
        _NoiseMap("WaveNoiseMap",2D)="white"{}
        _Cutoff("Alpha Cutoff",Range(0.0,1.0))=0.5
        _Wind("Wind(x,y,z,strength)",Vector)=(1,0,0,10)
        _WindNoiseStrength("WindNoiseStrength",Range(0,20))=10
        _StormParams("Storm(Begin,Keep,End,Slient)",Vector)=(1,100,40,100)
        _StormStrength("StormStrength",Range(0,40))=20
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode"="UniversalForward"}

            ZWrite On
            ZTest On
            Cull Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            #pragma vertex vert
            #pragma fragment frag
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma shader_feature _RECEIVE_SHADOWS_OFF
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };
            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            float _Cutoff;
            half4 _BaseColor;

            TEXTURE2D_X(_BaseMap);
            SAMPLER(sampler_BaseMap);
            sampler2D _NoiseMap;

            float4x4 _TerrianLocalToWorld;
            float2 _GrassQuadSize;
            float4 _Wind;
            float _WindNoiseStrength;
            float4 _StormParams;
            float _StormStrength;

            #define StormFront _StormParams.x
            #define StormMiddle _StormParams.y
            #define StormEnd _StormParams.z
            #define StormSlient _StormParams.w

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

                struct GrassInfo
                {
                    float4x4 localToTerrian;
                    float4 texParams;
                };

                StructuredBuffer<GrassInfo> _GrassInfos;
            #endif

            void setup(){}

            float3 applyWind(float3 positionWS,float3 grassUpWs,float3 windDir,float windStrength,float vertexLocalHeight )
            {
                float rad = windStrength*PI*0.9/2;
                windDir = normalize(windDir-dot(windDir,grassUpWs)*grassUpWs);
                float x,y;
                sincos(rad,x,y);
                float windedPos = x * windDir+y*grassUpWs;
                return positionWS +(windedPos-grassUpWs)*vertexLocalHeight;
            }

            float applyStorm(float3 positionWS,float3 windDir,float windStrength)
            {
                float stormInterval = StormFront+StormMiddle+StormEnd+StormSlient;
                float disInWindDir = dot(positionWS-windDir*_Time.y*(windStrength+_StormStrength),windDir);
                float offsetInInterval = stormInterval-(disInWindDir%stormInterval)-step(disInWindDir,0)*stormInterval;
                float x=0;
                if(offsetInInterval<StormFront)
                {
                    x = offsetInInterval*rcp(StormFront);
                }
                else if(offsetInInterval<StormFront+StormMiddle)
                {
                    x = 1;
                }
                else if(offsetInInterval<StormFront+StormMiddle+StormEnd)
                {
                    x=(StormFront+StormMiddle+StormEnd-offsetInInterval)/StormEnd;
                }
                return windStrength+_StormStrength*x;
            }



            Varyings vert (Attributes input)
            {
                Varyings output;
                float2 uv = input.uv;
                float3 positionOS = input.positionOS;
                float3 normalOS = input.normalOS;
                uint instanceID = input.instanceID;
                positionOS.xy = positionOS.xy*_GrassQuadSize;

                float localVertexHeight = positionOS.y;
                float3 grassUpDir = float3(0,1,0);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

                GrassInfo grassInfo = _GrassInfos[instanceID];
                positionOS = mul(grassInfo.localToTerrian,float4(positionOS,1)).xyz;
                normalOS = mul(grassInfo.localToTerrian,float4(normalOS,0)).xyz;
                grassUpDir = mul(grassInfo.localToTerrian,float4(grassUpDir,0)).xyz;

                uv = uv*grassInfo.texParams.xy+grassInfo.texParams.zw;

                #endif

                float4 positionWS = mul(_TerrianLocalToWorld,float4(positionOS,1));
                positionWS/=positionWS.w;

                grassUpDir=normalize(mul(_TerrianLocalToWorld,float4(grassUpDir,0)));

                float time = _Time.y;

                float3 windDir = normalize(_Wind.xyz);

                float windStrength = _Wind.w;

                windStrength = applyStorm(positionWS.xyz,windDir,windStrength);

                float2 noiseUV = (positionWS.wz-time)/30;
                float noiseValue = tex2Dlod(_NoiseMap,float4(noiseUV,0,0)).r;
                noiseValue = sin(noiseValue*windStrength);

                windStrength += noiseValue*_WindNoiseStrength;

                windStrength = saturate(windStrength/40);

                positionWS.xyz = applyWind(positionWS.xyz,grassUpDir,windDir,windStrength,localVertexHeight);


                output.uv = uv;
                output.positionWS = positionWS;
                output.positionCS = mul(UNITY_MATRIX_VP,positionWS);
                output.normalWS = mul(unity_ObjectToWorld,float4(normalOS,0)).xyz;
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                half4 diffuseColor = SAMPLE_TEXTURE2D_X(_BaseMap,sampler_BaseMap,input.uv);
                if(diffuseColor.a<_Cutoff)
                {
                    discard;
                    return 0;
                }
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;
                float3 normalWS = input.normalWS;
                float4 col = float4(1,1,1,1);
                float minDotLN = 0.2;
                col.rgb = max(minDotLN,abs(dot(lightDir,normalWS)))*lightColor*diffuseColor.rgb*_BaseColor.rgb*mainLight.shadowAttenuation;
                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
