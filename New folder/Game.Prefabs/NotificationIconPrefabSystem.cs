using System.Runtime.CompilerServices;
using Game.Common;
using Game.Rendering;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class NotificationIconPrefabSystem : GameSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NotificationIconDisplayData> __Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NotificationIconDisplayData>();
		}
	}

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_PrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private NotificationIconRenderSystem m_NotificationIconRenderSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_NotificationIconRenderSystem = base.World.GetOrCreateSystemManaged<NotificationIconRenderSystem>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<NotificationIconData>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_PrefabQuery = GetEntityQuery(ComponentType.ReadOnly<NotificationIconData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_UpdatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		int num = 1;
		try
		{
			ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NotificationIconDisplayData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NotificationIconDisplayData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<NotificationIconDisplayData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					NotificationIconPrefab prefab = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(nativeArray2[j]);
					NotificationIconDisplayData value = nativeArray3[j];
					value.m_IconIndex = num++;
					value.m_MinParams = new float2(prefab.m_DisplaySize.min, prefab.m_PulsateAmplitude.min);
					value.m_MaxParams = new float2(prefab.m_DisplaySize.max, prefab.m_PulsateAmplitude.max);
					value.m_CategoryMask = math.select(2147483648u, value.m_CategoryMask, value.m_CategoryMask != 0);
					nativeArray3[j] = value;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		m_NotificationIconRenderSystem.DisplayDataUpdated();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public NotificationIconPrefabSystem()
	{
	}
}
