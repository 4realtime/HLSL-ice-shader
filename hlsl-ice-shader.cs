float4x4 WorldViewProjection  : VIEWPROJECTION;
float4x4 Projection : PROJECTION;
float4x4 World : WORLD;
float4x4 View : VIEW;
float refractionScale = 1.0f;
float refractionIndex = 2.0f;
float Rzero = 1;
float fresnel_strenght;

float4x4 TexTransform
<
   string SasBindAddress = "Ventuz.Texture.Mapping";
>;

float3 lightDir
<
   string SasBindAddress ="Ventuz.Lights.Direction";
>;

float4 matAmbient
<
   string SasBindAddress ="Ventuz.Material.Ambient";
> = {1.0f, 1.0f, 1.0f, 1.0f};
float4 matDiffuse
<
   string SasBindAddress ="Ventuz.Material.Diffuse";
> = {1.0f, 1.0f, 1.0f, 1.0f};
float4 matEmissive
<
   string SasBindAddress ="Ventuz.Material.Emissive";
> = {1.0f, 1.0f, 1.0f, 1.0f};
float4 matSpecular
<
   string SasBindAddress ="Ventuz.Material.Specular";
> = {1.0f, 1.0f, 1.0f, 1.0f};

float4 litAmbient
<
   string SasBindAddress = "Ventuz.Lights.Ambient";
> = {1.0f, 1.0f, 1.0f, 1.0f};
float4 litDiffuse
<
   string SasBindAddress = "Ventuz.Lights.Diffuse";
> = {1.0f, 1.0f, 1.0f, 1.0f};
float4 litSpecular
<
   string SasBindAddress = "Ventuz.Lights.Specular";
> = {1.0f, 1.0f, 1.0f, 1.0f};

float SpecularExponent
<
   string SasBindAddress = "Ventuz.Material.Sharpness";   
>;

//DIFUSSE
texture colorTex;
sampler colorTexture = sampler_state
{
   Texture = <colorTex>;
};
//NORMAL
texture normalTex;
sampler normalTexture = sampler_state
{
   Texture = <normalTex>;
};
//REFRACTION
texture refractionTex;
sampler refractionTexture = sampler_state
{
   Texture = <refractionTex>;
};

struct VS_Input
{
   float4 position : POSITION;
   float2 tex : TEXCOORD0;
   float3 Normal : NORMAL;
};

//The PixelInputType structure has a new refractionPosition variable for the refraction vertex coordinates that will be passed into the pixel shader.

struct VS_OUTPUT
{
   float4 position : SV_POSITION;
   float2 tex : TEXCOORD0;
   float4 refractionPosition : TEXCOORD1;
   float3 Normal : TEXCOORD2;
   float3 ViewVec : TEXCOORD3;
   float3 Light : TEXCOORD4;

};

// Vertex Shader Program

VS_OUTPUT VS( VS_Input Input )
{
   VS_OUTPUT output;
   float4x4 viewProjectWorld;

   // Change the position vector to be 4 units for proper matrix calculations.
   Input.position.w = 1.0f;

   // Calculate the position of the vertex against the world, view, and projection matrices.
   output.position = mul(Input.position, World);
   output.position = mul(output.position, View);
   output.position = mul(output.position, Projection);
  
   // Store the texture coordinates for the pixel shader.
   output.tex = Input.tex;

   //Create the matrix used for transforming the Input vertex coordinates to the projected coordinates.

   // Create the view projection world matrix for refraction.
   viewProjectWorld = mul(View, Projection);
   viewProjectWorld = mul(World, viewProjectWorld);


   // Calculate the Input position against the viewProjectWorld matrix.
   output.refractionPosition = mul(Input.position, viewProjectWorld);
   output.ViewVec = -normalize( mul(Input.position, World).xyz );
   output.Normal = mul( float4(Input.Normal, 0.0f), World ).xyz;
   output.Light = -normalize( mul(float4(lightDir, 0.0f), View).xyz );

   return output;
}

// Pixel Shader Program

float4 PS( VS_OUTPUT Input ) : COLOR
{
   float2 refractTexCoord;
   float4 normalMap;
   float3 normal;
   float4 refractionColor;
   float4 textureColor;
   float4 result;

   //Refraction
  
   refractTexCoord.x = float(Input.refractionPosition.x / Input.refractionPosition.w / refractionIndex);
   refractTexCoord.y = float(-Input.refractionPosition.y / Input.refractionPosition.w / refractionIndex);
   normalMap = tex2D( normalTexture, Input.tex);
   normal = (normalMap.xyz * 2.0f) - 1.0f;
   refractTexCoord = refractTexCoord + (normal.xy * refractionScale);
   refractionColor = tex2D( refractionTexture, refractTexCoord);
   textureColor = tex2D( colorTexture, Input.tex);
  
  
   //Specular light
  
   float3 vReflect = normalize( 2 * dot( Input.Normal, Input.Light) * Input.Normal - Input.Light );      
   float4 AmbientColor = matAmbient * litAmbient;
   float4 DiffuseColor = matDiffuse * max( 0, dot( Input.Normal, Input.Light )) * litDiffuse;
   float4 SpecularColor = float4(0,0,0,0);
  
   //if ( SpecularExponent >= 0 )
   SpecularColor = matSpecular * litSpecular * pow( max( 0, dot(vReflect, Input.Light)), SpecularExponent );

 
   //Fresnel effect
   float4 fresnel = Rzero + (1.0f - Rzero) * pow(1.0f - dot(Input.Normal, Input.ViewVec) , fresnel_strenght);   
   result = lerp(refractionColor, textureColor, 1-fresnel) + SpecularColor;
   //result.a = matDiffuse.a * t.a;
   return result;
}

technique SimpleReflection
{
   pass pass0
   {
      vertexshader = compile vs_3_0 VS();
      pixelshader  = compile ps_3_0 PS();
   }
}



