Shader "Custom/Point Surface" {
	
	Properties {
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	
	SubShader{

	CGPROGRAM
	
	// compiler directive; instructs shader compiler to generate a surface shader with standard lighting and full support for shadows
	#pragma surface ConfigureSurface Standard fullforwardshadows  
	// minimum for shader's target level and quality
	#pragma target 3.0

	// define input structure for configuration function
	struct Input {
		float3 worldPos;
	};

	float _Smoothness;

	// define ConfigureSurface method - result indicated by inout
	void ConfigureSurface (Input input, inout SurfaceOutputStandard surface) {
		surface.Albedo = saturate(input.worldPos * 0.5 + 0.5); // saturate clamps components to [0, 1]
		surface.Smoothness = _Smoothness;
	}

	ENDCG
	
	}

	FallBack "Diffuse"
}
