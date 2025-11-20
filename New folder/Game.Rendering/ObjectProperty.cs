using Unity.Mathematics;

namespace Game.Rendering;

public enum ObjectProperty
{
	[InstanceProperty("colossal_BoneParameters", typeof(float2), BatchFlags.Bones, 0, false)]
	BoneParameters,
	[InstanceProperty("colossal_LightParameters", typeof(float2), BatchFlags.Emissive, 0, false)]
	LightParameters,
	[InstanceProperty("colossal_ColorMask0", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask1,
	[InstanceProperty("colossal_ColorMask1", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask2,
	[InstanceProperty("colossal_ColorMask2", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask3,
	[InstanceProperty("colossal_InfoviewColor", typeof(float2), BatchFlags.InfoviewColor, 0, false)]
	InfoviewColor,
	[InstanceProperty("colossal_BuildingState", typeof(float4), (BatchFlags)0, 0, false)]
	BuildingState,
	[InstanceProperty("_Outlines_Color", typeof(float4), BatchFlags.Outline, 0, true)]
	OutlineColors,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 0, false)]
	LodFade0,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 1, false)]
	LodFade1,
	[InstanceProperty("colossal_MetaParameters", typeof(float), BatchFlags.BlendWeights, 0, false)]
	MetaParameters,
	[InstanceProperty("colossal_Wetness", typeof(float4), BatchFlags.SurfaceState, 0, false)]
	SurfaceWetness,
	[InstanceProperty("colossal_Damage", typeof(float4), BatchFlags.SurfaceState, 0, false)]
	SurfaceDamage,
	[InstanceProperty("colossal_BaseState", typeof(float), BatchFlags.Base, 0, false)]
	BaseState,
	Count
}
