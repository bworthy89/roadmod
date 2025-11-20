using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class DeserializationBarrier : SafeCommandBufferSystem
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	[Preserve]
	public DeserializationBarrier()
	{
	}
}
