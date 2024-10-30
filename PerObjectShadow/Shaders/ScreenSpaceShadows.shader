Shader "Hidden/Miao/Shadow/ScreenSpaceShadows"
{
    Properties
    {
        // [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ScreenSpaceShadows"

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma multi_compile _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #pragma vertex   Vert
            #pragma fragment Fragment

            //Keep compiler quiet about Shadows.hlsl.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Assets/Scripts/Renderer/PerObjectShadow/Shaders/PerObjectShadow.hlsl"
            // Core.hlsl for XR dependencies
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if UNITY_REVERSED_Z
                float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
    #else
                float deviceDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.texcoord.xy).r;
                deviceDepth = deviceDepth * 2.0 - 1.0;
    #endif

                // Fetch shadow coordinates for cascade.
                float3 positionWS = ComputeWorldSpacePosition(input.texcoord.xy, deviceDepth, unity_MatrixInvVP);

                // Screenspace shadowmap is only used for directional lights which use orthogonal projection.
                half realtimeShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
                float perObjShadow = MainLightPerObjectSceneShadow(positionWS);
                return min(realtimeShadow, perObjShadow);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
