Shader "TSF Shaders/UI/OutlineEx" 
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Int) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

		
        Pass
        {
            Name "OUTLINE"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _MainTex_TexelSize;

            float4 _OutlineColor;
            int _OutlineWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float2 uvOriginXY : TEXCOORD1;
                float2 uvOriginZW : TEXCOORD2;
                fixed4 color : COLOR;
            };

            v2f vert(appdata IN)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(IN.vertex);
                o.texcoord = IN.texcoord;
                o.uvOriginXY = IN.texcoord1;
                o.uvOriginZW = IN.texcoord2;
                o.color = IN.color * _Color;

                return o;
            }

            fixed IsInRect(float2 pPos, float4 pClipRect)
            {
			    //pClipRect.xy => rect.min 
				//pClipRect.zw => rect.max
			    //判斷 pPos 是不是在 pClipRect 內
                pPos = step(pClipRect.xy, pPos) * step(pPos, pClipRect.zw);
                return pPos.x * pPos.y;
            }

            fixed SampleAlpha(int pIndex, v2f IN)
            {
                //const fixed sinArray[12] = { 0, 0.5, 0.866, 1, 0.866, 0.5, 0, -0.5, -0.866, -1, -0.866, -0.5 };
                //const fixed cosArray[12] = { 1, 0.866, 0.5, 0, -0.5, -0.866, -1, -0.866, -0.5, 0, 0.5, 0.866 };

				//使用越多方向越清楚 ( 原本UGUI只用了四個方向 )
				const fixed sinArray[8] = { 0, 0.707, 1,  0.707,  0, -0.707 , -1 , -0.707 };
                const fixed cosArray[8] = { 1, 0.707, 0, -0.707, -1, -0.707 ,  0 ,  0.707 };

                float2 pos = IN.texcoord + _MainTex_TexelSize.xy * float2(cosArray[pIndex], sinArray[pIndex]) * _OutlineWidth;
                float4 pClipRect = float4( IN.uvOriginXY, IN.uvOriginZW );
				return IsInRect(pos, pClipRect ) * (tex2D(_MainTex, pos) + _TextureSampleAdd).w * _OutlineColor.w;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
		
				float4 pClipRect = float4 (IN.uvOriginXY, IN.uvOriginZW);
                color.a *= IsInRect(IN.texcoord, pClipRect);
                half4 val = half4(_OutlineColor.r, _OutlineColor.g, _OutlineColor.b, 0);//(r,g,b,0)

				//只取四個方向(跟Outline取樣數量一樣)
                //val.a += SampleAlpha(0, IN);
                val.w += SampleAlpha(1, IN);
                //val.a += SampleAlpha(2, IN);
                val.w += SampleAlpha(3, IN);
                //val.a += SampleAlpha(4, IN);
                val.a += SampleAlpha(5, IN);
                //val.a += SampleAlpha(6, IN);
                val.w += SampleAlpha(7, IN);
                //val.a += SampleAlpha(8, IN);
                //val.w += SampleAlpha(9, IN);
                //val.a += SampleAlpha(10, IN);
                //val.a += SampleAlpha(11, IN);

                val.a = clamp(val.a, 0, 1);
                
				color.a = color.a *1.05;//經驗來說這樣文字比較清楚
				half orginA = color.a;
				color = (val * (1.0 - color.a)) + (color * color.a);
				color.a = max(orginA,val.a);
				
				//color.a = max(orginA,val.a) * IN.color.a; //處理文字半透明 ( 可以不處理 )
                return color;
				
            }
            ENDCG
        }
    }
}