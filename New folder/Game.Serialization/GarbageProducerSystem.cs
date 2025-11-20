using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Notifications;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class GarbageProducerSystem : GameSystemBase, IPostDeserialize
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_31347557_0;

	[Preserve]
	protected override void OnUpdate()
	{
	}

	public void PostDeserialize(Context context)
	{
		if (!(context.version < Version.garbageProducerFlags))
		{
			return;
		}
		EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GarbageProducer>());
		GarbageParameterData singleton = __query_31347557_0.GetSingleton<GarbageParameterData>();
		NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (!base.EntityManager.TryGetBuffer(nativeArray[i], isReadOnly: true, out DynamicBuffer<IconElement> buffer))
			{
				continue;
			}
			for (int j = 0; j < buffer.Length; j++)
			{
				if (base.EntityManager.TryGetComponent<PrefabRef>(buffer[j].m_Icon, out var component) && component.m_Prefab == singleton.m_GarbageNotificationPrefab)
				{
					if (base.EntityManager.TryGetComponent<GarbageProducer>(nativeArray[i], out var component2) && (component2.m_Flags & GarbageProducerFlags.GarbagePilingUpWarning) == 0)
					{
						component2.m_Flags |= GarbageProducerFlags.GarbagePilingUpWarning;
						base.EntityManager.SetComponentData(nativeArray[i], component2);
					}
					break;
				}
			}
		}
		nativeArray.Dispose();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<GarbageParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_31347557_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public GarbageProducerSystem()
	{
	}
}
