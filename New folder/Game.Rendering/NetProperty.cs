using Unity.Mathematics;

namespace Game.Rendering;

public enum NetProperty
{
	[InstanceProperty("colossal_CompositionMatrix0", typeof(float4x4), (BatchFlags)0, 0, false)]
	CompositionMatrix0,
	[InstanceProperty("colossal_CompositionMatrix1", typeof(float4x4), (BatchFlags)0, 0, false)]
	CompositionMatrix1,
	[InstanceProperty("colossal_CompositionMatrix2", typeof(float4x4), (BatchFlags)0, 0, false)]
	CompositionMatrix2,
	[InstanceProperty("colossal_CompositionMatrix3", typeof(float4x4), (BatchFlags)0, 0, false)]
	CompositionMatrix3,
	[InstanceProperty("colossal_CompositionMatrix4", typeof(float4x4), BatchFlags.Node, 0, false)]
	CompositionMatrix4,
	[InstanceProperty("colossal_CompositionMatrix5", typeof(float4x4), BatchFlags.Roundabout, 0, false)]
	CompositionMatrix5,
	[InstanceProperty("colossal_CompositionMatrix6", typeof(float4x4), BatchFlags.Node, 0, false)]
	CompositionMatrix6,
	[InstanceProperty("colossal_CompositionMatrix7", typeof(float4x4), BatchFlags.Node, 0, false)]
	CompositionMatrix7,
	[InstanceProperty("colossal_CompositionSync0", typeof(float4), BatchFlags.Node, 0, false)]
	CompositionSync0,
	[InstanceProperty("colossal_CompositionSync1", typeof(float4), BatchFlags.Node, 0, false)]
	CompositionSync1,
	[InstanceProperty("colossal_CompositionSync2", typeof(float4), BatchFlags.Node, 0, false)]
	CompositionSync2,
	[InstanceProperty("colossal_CompositionSync3", typeof(float4), BatchFlags.Node, 0, false)]
	CompositionSync3,
	[InstanceProperty("colossal_NetInfoviewColor", typeof(float4), BatchFlags.InfoviewColor, 0, false)]
	InfoviewColor,
	[InstanceProperty("_Outlines_Color", typeof(float4), BatchFlags.Outline, 0, true)]
	OutlineColors,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 0, false)]
	LodFade0,
	[InstanceProperty("colossal_LodFade", typeof(float), BatchFlags.LodFade, 1, false)]
	LodFade1,
	Count
}
