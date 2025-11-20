using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[FormerlySerializedAs("Colossal.Terrain.TerrainMaterialPropertiesData, Game")]
public struct TerrainMaterialPropertiesData : IComponentData, IQueryTypeParameter
{
}
