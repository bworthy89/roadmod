using Unity.Mathematics;

namespace Game.Rendering;

public enum LaneProperty
{
	[InstanceProperty("colossal_CurveMatrix", typeof(float4x4), (BatchFlags)0, 0, false)]
	CurveMatrix,
	[InstanceProperty("colossal_CurveParams", typeof(float4), (BatchFlags)0, 0, false)]
	CurveParams,
	[InstanceProperty("colossal_CurveScale", typeof(float4), (BatchFlags)0, 0, false)]
	CurveScale,
	[InstanceProperty("colossal_NetInfoviewColor", typeof(float4), BatchFlags.InfoviewColor, 0, false)]
	InfoviewColor,
	[InstanceProperty("colossal_CurveDeterioration", typeof(float4), (BatchFlags)0, 0, false)]
	CurveDeterioration,
	[InstanceProperty("_Outlines_Color", typeof(float4), BatchFlags.Outline, 0, true)]
	OutlineColors,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 0, false)]
	LodFade0,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 1, false)]
	LodFade1,
	[InstanceProperty("colossal_FlowMatrix", typeof(float4x4), BatchFlags.InfoviewFlow, 0, false)]
	FlowMatrix,
	[InstanceProperty("colossal_FlowOffset", typeof(float), BatchFlags.InfoviewFlow, 0, false)]
	FlowOffset,
	[InstanceProperty("colossal_HangingDistances", typeof(float4), BatchFlags.Hanging, 0, false)]
	HangingDistances,
	[InstanceProperty("colossal_ColorMask0", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask1,
	[InstanceProperty("colossal_ColorMask1", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask2,
	[InstanceProperty("colossal_ColorMask2", typeof(float4), BatchFlags.ColorMask, 0, false)]
	ColorMask3,
	Count
}
