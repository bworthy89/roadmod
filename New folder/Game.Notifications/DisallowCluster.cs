using System.Runtime.InteropServices;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Notifications;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct DisallowCluster : IComponentData, IQueryTypeParameter, IEmptySerializable
{
}
