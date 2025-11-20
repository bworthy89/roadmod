using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct TramTrack : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
