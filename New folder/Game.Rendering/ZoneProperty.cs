using Unity.Mathematics;

namespace Game.Rendering;

public enum ZoneProperty
{
	[InstanceProperty("colossal_CellType0", typeof(float4x4), (BatchFlags)0, 0, false)]
	CellType0,
	[InstanceProperty("colossal_CellType1", typeof(float4x4), BatchFlags.Extended1, 0, false)]
	CellType1,
	[InstanceProperty("colossal_CellType2", typeof(float4x4), BatchFlags.Extended2, 0, false)]
	CellType2,
	[InstanceProperty("colossal_CellType3", typeof(float4x4), BatchFlags.Extended3, 0, false)]
	CellType3,
	Count
}
