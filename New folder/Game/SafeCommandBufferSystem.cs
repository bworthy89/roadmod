using System;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game;

public class SafeCommandBufferSystem : EntityCommandBufferSystem
{
	private bool m_IsAllowed = true;

	public void AllowUsage()
	{
		m_IsAllowed = true;
	}

	public new EntityCommandBuffer CreateCommandBuffer()
	{
		if (m_IsAllowed)
		{
			return base.CreateCommandBuffer();
		}
		throw new Exception("Trying to create EntityCommandBuffer when it's not allowed!");
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_IsAllowed = false;
		base.OnUpdate();
	}

	[Preserve]
	public SafeCommandBufferSystem()
	{
	}
}
