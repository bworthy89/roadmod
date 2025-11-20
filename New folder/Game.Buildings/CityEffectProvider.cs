using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct CityEffectProvider : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
