using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct HiddenRoute : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
