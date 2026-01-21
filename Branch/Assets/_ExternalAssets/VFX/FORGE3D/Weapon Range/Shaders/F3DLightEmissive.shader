Shader "FORGE3D/Weapon Range/Light Emissive"
{
	Properties
	{
		[HideInInspector] __dirty( "", Int ) = 1
		_HDRGlow("HDR Glow", Range( 0 , 100)) = 1
		_Color("Color", Color) = (1,1,1,0)
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			fixed filler;
		};

		uniform float4 _Color;
		uniform float _HDRGlow;

		void surf( Input input , inout SurfaceOutputStandard output )
		{
			output.Emission = ( _Color * _HDRGlow ).rgb;
		}

		ENDCG
	}
	Fallback "Diffuse"
}
