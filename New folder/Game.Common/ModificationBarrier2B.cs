using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

namespace Game.Common;

public class ModificationBarrier2B : SafeCommandBufferSystem
{
	[MethodImpl(MethodImplOptions.NoInlining)]
	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
	}

	[Preserve]
	public ModificationBarrier2B()
	{
	}
}
