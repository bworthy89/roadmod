using System.Runtime.CompilerServices;
using Colossal;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class EditorGizmoSystem : GameSystemBase
{
	[BurstCompile]
	private struct EditorGizmoJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Node> m_NetNodeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_NetCurveType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentTypeHandle<Warning> m_WarningType;

		[ReadOnly]
		public ComponentTypeHandle<Highlighted> m_HighlightedType;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> m_CullingInfoType;

		[ReadOnly]
		public UnityEngine.Color m_HoveredColor;

		[ReadOnly]
		public UnityEngine.Color m_ErrorColor;

		[ReadOnly]
		public UnityEngine.Color m_WarningColor;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Node> nativeArray = chunk.GetNativeArray(ref m_NetNodeType);
			NativeArray<Curve> nativeArray2 = chunk.GetNativeArray(ref m_NetCurveType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Temp> nativeArray4 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<CullingInfo> nativeArray5 = chunk.GetNativeArray(ref m_CullingInfoType);
			bool flag;
			UnityEngine.Color color;
			UnityEngine.Color color2;
			UnityEngine.Color color3;
			UnityEngine.Color color4;
			if (chunk.Has(ref m_ErrorType))
			{
				flag = false;
				color = m_ErrorColor;
				color2 = m_ErrorColor;
				color3 = m_ErrorColor;
				color4 = m_ErrorColor;
			}
			else if (chunk.Has(ref m_WarningType))
			{
				flag = false;
				color = m_WarningColor;
				color2 = m_WarningColor;
				color3 = m_WarningColor;
				color4 = m_WarningColor;
			}
			else if (chunk.Has(ref m_HighlightedType))
			{
				flag = false;
				color = m_HoveredColor;
				color2 = m_HoveredColor;
				color3 = m_HoveredColor;
				color4 = m_HoveredColor;
			}
			else
			{
				flag = nativeArray4.Length != 0;
				color = UnityEngine.Color.white;
				color2 = UnityEngine.Color.red;
				color3 = UnityEngine.Color.green;
				color4 = UnityEngine.Color.blue;
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (!IsNearCamera(nativeArray5[i]))
				{
					continue;
				}
				UnityEngine.Color color5 = color;
				if (flag)
				{
					Temp temp = nativeArray4[i];
					if ((temp.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						color5 = m_HoveredColor;
					}
				}
				Node node = nativeArray[i];
				float3 @float = math.rotate(node.m_Rotation, math.right());
				float3 float2 = math.rotate(node.m_Rotation, math.up());
				float3 float3 = math.rotate(node.m_Rotation, math.forward());
				m_GizmoBatcher.DrawLine(node.m_Position - @float, node.m_Position + @float, color5);
				m_GizmoBatcher.DrawLine(node.m_Position - float2, node.m_Position + float2, color5);
				m_GizmoBatcher.DrawLine(node.m_Position - float3, node.m_Position + float3, color5);
			}
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				if (!IsNearCamera(nativeArray5[j]))
				{
					continue;
				}
				UnityEngine.Color color6 = color;
				if (flag)
				{
					Temp temp2 = nativeArray4[j];
					if ((temp2.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp2.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						color6 = m_HoveredColor;
					}
				}
				Curve curve = nativeArray2[j];
				m_GizmoBatcher.DrawCurve(curve, color6);
			}
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				if (!IsNearCamera(nativeArray5[k]))
				{
					continue;
				}
				UnityEngine.Color color7 = color2;
				UnityEngine.Color color8 = color3;
				UnityEngine.Color color9 = color4;
				if (flag)
				{
					Temp temp3 = nativeArray4[k];
					if ((temp3.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
					if ((temp3.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						color7 = m_HoveredColor;
						color8 = m_HoveredColor;
						color9 = m_HoveredColor;
					}
				}
				Game.Objects.Transform transform = nativeArray3[k];
				float3 float4 = math.rotate(transform.m_Rotation, math.right());
				float3 float5 = math.rotate(transform.m_Rotation, math.up());
				float3 float6 = math.rotate(transform.m_Rotation, math.forward());
				m_GizmoBatcher.DrawArrow(transform.m_Position - float4, transform.m_Position + float4, color7);
				m_GizmoBatcher.DrawArrow(transform.m_Position - float5, transform.m_Position + float5, color8);
				m_GizmoBatcher.DrawArrow(transform.m_Position - float6, transform.m_Position + float6, color9);
			}
		}

		private bool IsNearCamera(CullingInfo cullingInfo)
		{
			if (cullingInfo.m_CullingIndex != 0)
			{
				return (m_CullingData[cullingInfo.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Warning> __Game_Tools_Warning_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Highlighted> __Game_Tools_Highlighted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CullingInfo> __Game_Rendering_CullingInfo_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Warning>(isReadOnly: true);
			__Game_Tools_Highlighted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Highlighted>(isReadOnly: true);
			__Game_Rendering_CullingInfo_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CullingInfo>(isReadOnly: true);
		}
	}

	private EntityQuery m_RenderQuery;

	private EntityQuery m_RenderingSettingsQuery;

	private GizmosSystem m_GizmosSystem;

	private PreCullingSystem m_PreCullingSystem;

	private RenderingSystem m_RenderingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_PreCullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_RenderQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Tools.EditorContainer>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Hidden>());
		m_RenderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<RenderingSettingsData>());
		RequireForUpdate(m_RenderQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_RenderingSystem.hideOverlay)
		{
			UnityEngine.Color hoveredColor = new UnityEngine.Color(0.5f, 0.5f, 1f, 1f);
			UnityEngine.Color errorColor = new UnityEngine.Color(1f, 0.5f, 0.5f, 1f);
			UnityEngine.Color warningColor = new UnityEngine.Color(1f, 1f, 0.5f, 1f);
			if (!m_RenderingSettingsQuery.IsEmptyIgnoreFilter)
			{
				RenderingSettingsData singleton = m_RenderingSettingsQuery.GetSingleton<RenderingSettingsData>();
				hoveredColor = singleton.m_HoveredColor;
				errorColor = singleton.m_ErrorColor;
				warningColor = singleton.m_WarningColor;
				hoveredColor.a = 1f;
				errorColor.a = 1f;
				warningColor.a = 1f;
			}
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new EditorGizmoJob
			{
				m_NetNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NetCurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WarningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HighlightedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Highlighted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CullingInfoType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_CullingInfo_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HoveredColor = hoveredColor,
				m_ErrorColor = errorColor,
				m_WarningColor = warningColor,
				m_CullingData = m_PreCullingSystem.GetCullingData(readOnly: true, out dependencies),
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2)
			}, m_RenderQuery, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
			m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
			m_PreCullingSystem.AddCullingDataReader(jobHandle);
			base.Dependency = jobHandle;
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
	public EditorGizmoSystem()
	{
	}
}
