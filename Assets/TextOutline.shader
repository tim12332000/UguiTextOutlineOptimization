Shader "Custom/TextOutline" {
    Properties {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
    }

    SubShader {

        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Lighting Off Cull Off ZTest Always ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		//第一個Pass，實現Text內容背景顏色，並向外擴大_OutlineWidth
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _MainTex_TexelSize;
            uniform fixed4 _Color;
            uniform fixed4 _OutlineColor;


            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
                return o;
            }
			//確定每個像素周圍8個像素的座標。
            static const float2 dirList[9]={
                float2(-1,-1),float2(0,-1),float2(1,-1),
                float2(-1,0),float2(0,0),float2(1,0),
                float2(-1,1),float2(0,1),float2(1,1)
            };
			//謀取dirList第dirIndex個方位的透明度值。
            float getDirPosAlpha(float index, float2 xy){
				float2 curPos = xy;
                float2 dir = dirList[index];
				float2 dirPos = curPos + dir * _MainTex_TexelSize.xy * 0.6*1;
                return tex2D(_MainTex, dirPos).a;
            };
			//對於每個像素，傳入片元參數v2f i ，獲取次像素周圍和自身的共9個像素進行透明度疊加。
			//那麼得出的結果就是非透明的區域被放大了，形成了黑邊。
            float getShadowAlpha(float2 xy){
                float a = 0;
				float index = 0;
                a += getDirPosAlpha(index, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a += getDirPosAlpha(index++, xy);
                a = clamp(a,0,1);
                return a;
            }


            //由於渲染Text內容時，Text字上沒有被渲染的區域是透明的，也就是透明度a值是0，
			//所以只要將有內容的區域往外透明度爲0的區域擴展一些像素將就能夠形成描邊效果。
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;
				float2 xy = i.texcoord.xy;
                col.a *= getShadowAlpha(xy);
                return col;
            }
            ENDCG
        }
		//第二個Pass，常規渲染Text內容。
				
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO STEREO_INSTANCING_ON STEREO_MULTIVIEW_ON
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 texcoord : TEXCOORD0;
                //UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            uniform float4 _MainTex_TexelSize;
            uniform fixed4 _Color;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                step(v.texcoord, v.vertex.xy);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = i.color;
				col.a = tex2D(_MainTex, i.texcoord).a;
                return col;
            }
            ENDCG
        }
    }
}