using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct ServiceFeeCollector : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
