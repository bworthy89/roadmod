using Unity.Mathematics;

namespace Game.Rendering;

public enum MaterialProperty
{
	[MaterialProperty("DefaultPVTStack_atlasParams0", typeof(float4), false)]
	DefaultPVTStack_Transform,
	[MaterialProperty("ExtendedPVTStack_atlasParams0", typeof(float4), false)]
	ExtendedPVTStack_Transform,
	[MaterialProperty("DefaultPVTStack_atlasParams1", typeof(float4), false)]
	DefaultPVTStack_TextureInfo,
	[MaterialProperty("ExtendedPVTStack_atlasParams1", typeof(float4), false)]
	ExtendedPVTStack_TextureInfo,
	[MaterialProperty("_AlbedoAffectEmissive", typeof(float), false)]
	AlbedoAffectEmissive,
	[MaterialProperty("_Snow", typeof(float), false)]
	Snow,
	[MaterialProperty("colossal_SingleLightsOffset", typeof(float), false)]
	SingleLightsOffset,
	[MaterialProperty("colossal_TextureArea", typeof(float4), false)]
	TextureArea,
	[MaterialProperty("colossal_MeshSize", typeof(float4), false)]
	MeshSize,
	[MaterialProperty("colossal_LodDistanceFactor", typeof(float), false)]
	LodDistanceFactor,
	[MaterialProperty("_BaseColor", typeof(float4), false)]
	BaseColor,
	[MaterialProperty("colossal_DilationParams", typeof(float4), false)]
	DilationParams,
	[MaterialProperty("_ImpostorFrames", typeof(float), false)]
	ImpostorFrames,
	[MaterialProperty("_ImpostorSize", typeof(float), false)]
	ImpostorSize,
	[MaterialProperty("_ImpostorOffset", typeof(float3), false)]
	ImpostorOffset,
	[MaterialProperty("_TextureScaleFactor", typeof(float), false)]
	TextureScaleFactor,
	[MaterialProperty("_SmoothingDistance", typeof(float), false)]
	SmoothingDistance,
	[MaterialProperty("_WindRangeLvlB", typeof(float), false)]
	WindRangeLvlB,
	[MaterialProperty("_WindElasticityLvlB", typeof(float), false)]
	WindElasticityLvlB,
	[MaterialProperty("colossal_ShapeParameters1", typeof(float4), false)]
	ShapeParameters1,
	[MaterialProperty("colossal_ShapeParameters2", typeof(float4), false)]
	ShapeParameters2,
	[MaterialProperty("colossal_CullParameters", typeof(float3), false)]
	CullParameters,
	[MaterialProperty("colossal_OverlayParameters", typeof(float2), false)]
	OverlayParameters,
	[MaterialProperty("_AlphaMask_IndexRange", typeof(float2), false)]
	AlphaMaskIndexRange,
	Count
}
