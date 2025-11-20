using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace Game.Tools;

public class ToolOutputBarrier : SafeCommandBufferSystem
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	[Preserve]
	public ToolOutputBarrier()
	{
	}
}
