using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Notifications;

public struct IconCommandBuffer
{
	public enum CommandFlags : byte
	{
		Add = 1,
		Remove = 2,
		Update = 4,
		Temp = 8,
		Hidden = 0x10,
		DisallowCluster = 0x20,
		All = 0x40
	}

	public struct Command : IComparable<Command>
	{
		public Entity m_Owner;

		public Entity m_Prefab;

		public Entity m_Target;

		public float3 m_Location;

		public CommandFlags m_CommandFlags;

		public IconPriority m_Priority;

		public IconClusterLayer m_ClusterLayer;

		public IconFlags m_Flags;

		public int m_BufferIndex;

		public float m_Delay;

		public int CompareTo(Command other)
		{
			int2 @int = math.select((int2)0, (int2)1, new bool2(m_Prefab != Entity.Null, other.m_Prefab != Entity.Null));
			return math.select(math.select(m_BufferIndex - other.m_BufferIndex, @int.x - @int.y, @int.x != @int.y), m_Owner.Index - other.m_Owner.Index, m_Owner.Index != other.m_Owner.Index);
		}
	}

	private NativeQueue<Command>.ParallelWriter m_Commands;

	private int m_BufferIndex;

	public IconCommandBuffer(NativeQueue<Command>.ParallelWriter commands, int bufferIndex)
	{
		m_Commands = commands;
		m_BufferIndex = bufferIndex;
	}

	public void Add(Entity owner, Entity prefab, IconPriority priority = IconPriority.Info, IconClusterLayer clusterLayer = IconClusterLayer.Default, IconFlags flags = (IconFlags)0, Entity target = default(Entity), bool isTemp = false, bool isHidden = false, bool disallowCluster = false, float delay = 0f)
	{
		m_Commands.Enqueue(new Command
		{
			m_Owner = owner,
			m_Prefab = prefab,
			m_Target = target,
			m_CommandFlags = (CommandFlags)(1 | (isTemp ? 8 : 0) | (isHidden ? 16 : 0) | (disallowCluster ? 32 : 0)),
			m_Priority = priority,
			m_ClusterLayer = clusterLayer,
			m_Flags = (flags & ~IconFlags.CustomLocation),
			m_BufferIndex = m_BufferIndex,
			m_Delay = delay
		});
	}

	public void Add(Entity owner, Entity prefab, float3 location, IconPriority priority = IconPriority.Info, IconClusterLayer clusterLayer = IconClusterLayer.Default, IconFlags flags = IconFlags.IgnoreTarget, Entity target = default(Entity), bool isTemp = false, bool isHidden = false, bool disallowCluster = false, float delay = 0f)
	{
		m_Commands.Enqueue(new Command
		{
			m_Owner = owner,
			m_Prefab = prefab,
			m_Target = target,
			m_Location = location,
			m_CommandFlags = (CommandFlags)(1 | (isTemp ? 8 : 0) | (isHidden ? 16 : 0) | (disallowCluster ? 32 : 0)),
			m_Priority = priority,
			m_ClusterLayer = clusterLayer,
			m_Flags = (flags | IconFlags.CustomLocation),
			m_BufferIndex = m_BufferIndex,
			m_Delay = delay
		});
	}

	public void Remove(Entity owner, Entity prefab, Entity target = default(Entity), IconFlags flags = (IconFlags)0)
	{
		m_Commands.Enqueue(new Command
		{
			m_Owner = owner,
			m_Prefab = prefab,
			m_Target = target,
			m_CommandFlags = CommandFlags.Remove,
			m_Flags = flags,
			m_BufferIndex = m_BufferIndex
		});
	}

	public void Remove(Entity owner, IconPriority priority)
	{
		m_Commands.Enqueue(new Command
		{
			m_Owner = owner,
			m_CommandFlags = (CommandFlags)66,
			m_Priority = priority,
			m_BufferIndex = m_BufferIndex
		});
	}

	public void Update(Entity owner)
	{
		m_Commands.Enqueue(new Command
		{
			m_Owner = owner,
			m_CommandFlags = CommandFlags.Update,
			m_BufferIndex = m_BufferIndex
		});
	}
}
