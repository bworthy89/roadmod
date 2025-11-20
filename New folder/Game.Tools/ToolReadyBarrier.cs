using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace Game.Tools;

public class ToolReadyBarrier : SafeCommandBufferSystem
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	[Preserve]
	public ToolReadyBarrier()
	{
	}
}
