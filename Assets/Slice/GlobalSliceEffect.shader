Shader "Hidden/GlobalSliceEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Global slice parameters
        float4 _SlicePlane;
        float _Threshold;
        float4 _SliceColor;
        float _ColorThreshold; // New parameter for controlling slice color extent

        half4 FragmentSlice(Varyings IN) : SV_Target
        {
            // Sample the original color from the blit source
            half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);

            // Sample depth and reconstruct world position
            float depth = SampleSceneDepth(IN.texcoord);
            float3 worldPos = ComputeWorldSpacePosition(IN.texcoord, depth, UNITY_MATRIX_I_VP);

             // Calculate signed distance to plane
              float sd = dot(_SlicePlane.xyz, worldPos) - _SlicePlane.w;

            // Show original color within the threshold (at the slice)
            if (abs(sd) <= _Threshold)
            {
                return color;
            }
            // Show slice color for everything in front of the slice up to ColorThreshold
            else if (sd > -_Threshold && sd > -_ColorThreshold)
            {
                return _SliceColor;
            }
            // Make everything behind the slice invisible
            else
            {
                return float4(0, 0, 0, 0);
            }
            
        }
        ENDHLSL

        Pass
        {
            Name "GlobalSlice"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragmentSlice
            ENDHLSL
        }
    }
}