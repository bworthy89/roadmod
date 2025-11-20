using System.Runtime.CompilerServices;
using Game.Common;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TempCostTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
		}
	}

	private CitySystem m_CitySystem;

	private EntityQuery m_TempQuery;

	private IntTooltip m_Cost;

	private IntTooltip m_Refund;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_Cost = new IntTooltip
		{
			path = "cost",
			icon = "Media/Game/Icons/Money.svg",
			unit = "money"
		};
		m_Refund = new IntTooltip
		{
			path = "refund",
			icon = "Media/Game/Icons/Money.svg",
			label = LocalizedString.Id("Tools.REFUND_AMOUNT_LABEL"),
			unit = "money"
		};
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		CompleteDependency();
		NativeArray<ArchetypeChunk> nativeArray = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			int num = 0;
			ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			foreach (ArchetypeChunk item in nativeArray)
			{
				foreach (Temp item2 in item.GetNativeArray(ref typeHandle))
				{
					if ((item2.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade | TempFlags.RemoveCost)) != 0 && (item2.m_Flags & TempFlags.Cancel) == 0)
					{
						num += item2.m_Cost;
					}
				}
			}
			if (num > 0)
			{
				m_Cost.value = num;
				m_Cost.color = ((m_CitySystem.moneyAmount < num) ? TooltipColor.Error : TooltipColor.Info);
				AddMouseTooltip(m_Cost);
			}
			else if (num < 0)
			{
				m_Refund.value = -num;
				AddMouseTooltip(m_Refund);
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
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
	public TempCostTooltipSystem()
	{
	}
}
