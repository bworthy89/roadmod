using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AreaBorderRenderSystem : GameSystemBase
{
	private struct Border : IEquatable<Border>
	{
		public float3 m_StartPos;

		public float3 m_EndPos;

		public bool Equals(Border other)
		{
			return m_StartPos.Equals(other.m_StartPos) & m_EndPos.Equals(other.m_EndPos);
		}

		public override int GetHashCode()
		{
			return m_StartPos.GetHashCode();
		}
	}

	[BurstCompile]
	private struct AreaBorderRenderJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Lot> m_LotType;

		[ReadOnly]
		public ComponentTypeHandle<Batch> m_BatchType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> m_MapTileType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Warning> m_WarningType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> m_ColorData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public RenderingSettingsData m_RenderingSettingsData;

		[ReadOnly]
		public NativeArray<Vector4> m_InfoviewColors;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_InfoviewEnabled;

		public OverlayRenderSystem.Buffer m_OverlayBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Area> nativeArray = archetypeChunk.GetNativeArray(ref m_AreaType);
				BufferAccessor<Node> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_NodeType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					if ((nativeArray[j].m_Flags & AreaFlags.Slave) == 0)
					{
						num += bufferAccessor[j].Length;
					}
				}
			}
			NativeParallelHashSet<Border> borderMap = new NativeParallelHashSet<Border>(num, Allocator.Temp);
			NativeParallelHashSet<float3> nodeMap = new NativeParallelHashSet<float3>(num, Allocator.Temp);
			if (m_InfoviewEnabled)
			{
				for (int k = 0; k < m_Chunks.Length; k++)
				{
					AddBorders(m_Chunks[k], borderMap, infoview: true);
				}
				for (int l = 0; l < m_Chunks.Length; l++)
				{
					DrawInfoviewBorders(m_Chunks[l], borderMap);
				}
				borderMap.Clear();
			}
			for (int m = 0; m < m_Chunks.Length; m++)
			{
				AddBorders(m_Chunks[m], borderMap, infoview: false);
			}
			for (int n = 0; n < m_Chunks.Length; n++)
			{
				DrawBorders(m_Chunks[n], borderMap, nodeMap);
			}
			borderMap.Dispose();
			nodeMap.Dispose();
		}

		private void AddBorders(ArchetypeChunk chunk, NativeParallelHashSet<Border> borderMap, bool infoview)
		{
			NativeArray<Area> nativeArray = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			bool flag;
			if (infoview)
			{
				if (!chunk.Has(ref m_LotType) || chunk.Has(ref m_BatchType) || nativeArray2.Length == 0)
				{
					return;
				}
				flag = false;
			}
			else if (chunk.Has(ref m_ErrorType))
			{
				flag = false;
			}
			else if (chunk.Has(ref m_WarningType))
			{
				flag = false;
			}
			else
			{
				if (nativeArray3.Length == 0)
				{
					return;
				}
				flag = true;
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (flag)
				{
					Temp temp = nativeArray3[i];
					if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0 || (temp.m_Flags & TempFlags.Hidden) != 0)
					{
						continue;
					}
				}
				Area area = nativeArray[i];
				if ((area.m_Flags & AreaFlags.Slave) != 0)
				{
					continue;
				}
				Entity prefab = nativeArray4[i].m_Prefab;
				DynamicBuffer<Node> dynamicBuffer = bufferAccessor[i];
				AreaGeometryData areaGeometryData = m_PrefabGeometryData[prefab];
				if (!m_EditorMode && (areaGeometryData.m_Flags & Game.Areas.GeometryFlags.HiddenIngame) != 0)
				{
					continue;
				}
				if (infoview)
				{
					if ((areaGeometryData.m_Flags & Game.Areas.GeometryFlags.SubAreaBatch) != 0)
					{
						continue;
					}
					Owner owner = nativeArray2[i];
					bool flag2 = false;
					while (true)
					{
						if (m_ColorData.HasComponent(owner.m_Owner))
						{
							flag2 = m_ColorData[owner.m_Owner].m_Index != 0;
							break;
						}
						if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
						{
							break;
						}
						owner = componentData;
					}
					if (!flag2)
					{
						continue;
					}
				}
				float3 @float = dynamicBuffer[0].m_Position;
				for (int j = 1; j < dynamicBuffer.Length; j++)
				{
					float3 position = dynamicBuffer[j].m_Position;
					if ((area.m_Flags & AreaFlags.CounterClockwise) != 0)
					{
						borderMap.Add(new Border
						{
							m_StartPos = position,
							m_EndPos = @float
						});
					}
					else
					{
						borderMap.Add(new Border
						{
							m_StartPos = @float,
							m_EndPos = position
						});
					}
					@float = position;
				}
				if ((area.m_Flags & AreaFlags.Complete) != 0)
				{
					float3 position2 = dynamicBuffer[0].m_Position;
					if ((area.m_Flags & AreaFlags.CounterClockwise) != 0)
					{
						borderMap.Add(new Border
						{
							m_StartPos = position2,
							m_EndPos = @float
						});
					}
					else
					{
						borderMap.Add(new Border
						{
							m_StartPos = @float,
							m_EndPos = position2
						});
					}
				}
			}
		}

		private void DrawInfoviewBorders(ArchetypeChunk chunk, NativeParallelHashSet<Border> borderMap)
		{
			if (!chunk.Has(ref m_LotType) || chunk.Has(ref m_BatchType))
			{
				return;
			}
			NativeArray<Area> nativeArray = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			if (nativeArray2.Length == 0)
			{
				return;
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Area area = nativeArray[i];
				if ((area.m_Flags & AreaFlags.Slave) != 0)
				{
					continue;
				}
				Entity prefab = nativeArray3[i].m_Prefab;
				DynamicBuffer<Node> dynamicBuffer = bufferAccessor[i];
				AreaGeometryData geometryData = m_PrefabGeometryData[prefab];
				if ((!m_EditorMode && (geometryData.m_Flags & Game.Areas.GeometryFlags.HiddenIngame) != 0) || (geometryData.m_Flags & Game.Areas.GeometryFlags.SubAreaBatch) != 0)
				{
					continue;
				}
				UnityEngine.Color color = new UnityEngine.Color(-1f, -1f, -1f);
				Owner owner = nativeArray2[i];
				while (true)
				{
					if (m_ColorData.HasComponent(owner.m_Owner))
					{
						Game.Objects.Color color2 = m_ColorData[owner.m_Owner];
						if (color2.m_Index != 0)
						{
							color = m_InfoviewColors[color2.m_Index * 3];
						}
						break;
					}
					if (!m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
					{
						break;
					}
					owner = componentData;
				}
				if (color.r < 0f)
				{
					continue;
				}
				float3 @float = dynamicBuffer[0].m_Position;
				geometryData.m_SnapDistance *= 2f;
				for (int j = 1; j < dynamicBuffer.Length; j++)
				{
					float3 position = dynamicBuffer[j].m_Position;
					Border item = (((area.m_Flags & AreaFlags.CounterClockwise) == 0) ? new Border
					{
						m_StartPos = position,
						m_EndPos = @float
					} : new Border
					{
						m_StartPos = @float,
						m_EndPos = position
					});
					if (!borderMap.Contains(item))
					{
						DrawEdge(color, @float, position, geometryData, dashedLines: false, (OverlayRenderSystem.StyleFlags)0);
					}
					@float = position;
				}
				if ((area.m_Flags & AreaFlags.Complete) != 0)
				{
					float3 position2 = dynamicBuffer[0].m_Position;
					Border item2 = (((area.m_Flags & AreaFlags.CounterClockwise) == 0) ? new Border
					{
						m_StartPos = position2,
						m_EndPos = @float
					} : new Border
					{
						m_StartPos = @float,
						m_EndPos = position2
					});
					if (!borderMap.Contains(item2))
					{
						DrawEdge(color, @float, position2, geometryData, dashedLines: false, (OverlayRenderSystem.StyleFlags)0);
					}
				}
			}
		}

		private void DrawBorders(ArchetypeChunk chunk, NativeParallelHashSet<Border> borderMap, NativeParallelHashSet<float3> nodeMap)
		{
			NativeArray<Area> nativeArray = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			OverlayRenderSystem.StyleFlags styleFlags = (OverlayRenderSystem.StyleFlags)0;
			bool flag = (chunk.Has(ref m_LotType) ? true : false);
			bool flag2;
			bool dashedLines;
			if (chunk.Has(ref m_MapTileType))
			{
				styleFlags |= OverlayRenderSystem.StyleFlags.Projected;
				if (m_EditorMode)
				{
					flag2 = true;
					dashedLines = false;
				}
				else
				{
					flag2 = false;
					dashedLines = true;
				}
			}
			else
			{
				flag2 = true;
				dashedLines = false;
			}
			bool flag3;
			UnityEngine.Color color;
			if (chunk.Has(ref m_ErrorType))
			{
				flag3 = false;
				color = m_RenderingSettingsData.m_ErrorColor;
			}
			else if (chunk.Has(ref m_WarningType))
			{
				flag3 = false;
				color = m_RenderingSettingsData.m_WarningColor;
			}
			else
			{
				if (nativeArray2.Length == 0)
				{
					return;
				}
				flag3 = true;
				dashedLines = false;
				color = m_RenderingSettingsData.m_HoveredColor;
			}
			UnityEngine.Color color2 = new UnityEngine.Color(1f, 1f, 1f, 0.5f);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				UnityEngine.Color color3 = color;
				bool flag4 = flag2;
				if (nativeArray2.Length != 0)
				{
					Temp temp = nativeArray2[i];
					if (flag3)
					{
						if ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0 || (temp.m_Flags & TempFlags.Hidden) != 0)
						{
							continue;
						}
						if ((temp.m_Flags & TempFlags.Parent) != 0)
						{
							color3 = m_RenderingSettingsData.m_OwnerColor;
						}
					}
					flag4 &= (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0;
				}
				else
				{
					flag4 = false;
				}
				Area area = nativeArray[i];
				if ((area.m_Flags & AreaFlags.Slave) != 0)
				{
					continue;
				}
				Entity prefab = nativeArray3[i].m_Prefab;
				DynamicBuffer<Node> dynamicBuffer = bufferAccessor[i];
				AreaGeometryData geometryData = m_PrefabGeometryData[prefab];
				if (!m_EditorMode && (geometryData.m_Flags & Game.Areas.GeometryFlags.HiddenIngame) != 0)
				{
					continue;
				}
				float3 @float = dynamicBuffer[0].m_Position;
				if (dynamicBuffer.Length == 1)
				{
					if (flag4 && nodeMap.Add(@float))
					{
						DrawNode(color2, @float, geometryData, styleFlags);
					}
					continue;
				}
				for (int j = 1; j < dynamicBuffer.Length; j++)
				{
					float3 position = dynamicBuffer[j].m_Position;
					Border item = (((area.m_Flags & AreaFlags.CounterClockwise) == 0) ? new Border
					{
						m_StartPos = position,
						m_EndPos = @float
					} : new Border
					{
						m_StartPos = @float,
						m_EndPos = position
					});
					if (flag4 && nodeMap.Add(@float))
					{
						DrawNode(color2, @float, geometryData, styleFlags);
					}
					if (!borderMap.Contains(item))
					{
						if (flag && j == 1)
						{
							DrawEdge(color3 * 0.5f, @float, position, geometryData, dashedLines, styleFlags);
						}
						else
						{
							DrawEdge(color3, @float, position, geometryData, dashedLines, styleFlags);
						}
					}
					if (flag4 && nodeMap.Add(position))
					{
						DrawNode(color2, position, geometryData, styleFlags);
					}
					@float = position;
				}
				if ((area.m_Flags & AreaFlags.Complete) != 0)
				{
					float3 position2 = dynamicBuffer[0].m_Position;
					Border item2 = (((area.m_Flags & AreaFlags.CounterClockwise) == 0) ? new Border
					{
						m_StartPos = position2,
						m_EndPos = @float
					} : new Border
					{
						m_StartPos = @float,
						m_EndPos = position2
					});
					if (flag4 && nodeMap.Add(@float))
					{
						DrawNode(color2, @float, geometryData, styleFlags);
					}
					if (!borderMap.Contains(item2))
					{
						DrawEdge(color3, @float, position2, geometryData, dashedLines, styleFlags);
					}
					if (flag4 && nodeMap.Add(position2))
					{
						DrawNode(color2, position2, geometryData, styleFlags);
					}
				}
			}
		}

		private void DrawNode(UnityEngine.Color color, float3 position, AreaGeometryData geometryData, OverlayRenderSystem.StyleFlags styleFlags)
		{
			m_OverlayBuffer.DrawCircle(color, color, 0f, styleFlags, new float2(0f, 1f), position, geometryData.m_SnapDistance * 0.3f);
		}

		private void DrawEdge(UnityEngine.Color color, float3 startPos, float3 endPos, AreaGeometryData geometryData, bool dashedLines, OverlayRenderSystem.StyleFlags styleFlags)
		{
			Line3.Segment line = new Line3.Segment(startPos, endPos);
			if (dashedLines)
			{
				float num = math.distance(startPos.xz, endPos.xz);
				num /= math.max(1f, math.round(num / (geometryData.m_SnapDistance * 1.25f)));
				m_OverlayBuffer.DrawDashedLine(color, color, 0f, styleFlags, line, geometryData.m_SnapDistance * 0.2f, num * 0.55f, num * 0.45f);
			}
			else
			{
				m_OverlayBuffer.DrawLine(color, color, 0f, styleFlags, line, geometryData.m_SnapDistance * 0.3f, 1f);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lot> __Game_Areas_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Batch> __Game_Areas_Batch_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> __Game_Areas_MapTile_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Warning> __Game_Tools_Warning_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Color> __Game_Objects_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lot>(isReadOnly: true);
			__Game_Areas_Batch_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Batch>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MapTile>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Tools_Warning_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Warning>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Color>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
		}
	}

	private OverlayRenderSystem m_OverlayRenderSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_AreaBorderQuery;

	private EntityQuery m_AreaBorderInfoviewQuery;

	private EntityQuery m_RenderingSettingsQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_AreaBorderQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Area>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Error>(),
				ComponentType.ReadOnly<Warning>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_AreaBorderInfoviewQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Area>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Error>(),
				ComponentType.ReadOnly<Warning>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Lot>(),
				ComponentType.ReadOnly<Owner>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Hidden>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Batch>()
			}
		});
		m_RenderingSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<RenderingSettingsData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = ((m_ToolSystem.activeInfoview != null) ? m_AreaBorderInfoviewQuery : m_AreaBorderQuery);
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			RenderingSettingsData renderingSettingsData = new RenderingSettingsData
			{
				m_HoveredColor = new UnityEngine.Color(0.5f, 0.5f, 1f, 0.5f),
				m_ErrorColor = new UnityEngine.Color(1f, 0.25f, 0.25f, 0.5f),
				m_WarningColor = new UnityEngine.Color(1f, 1f, 0.25f, 0.5f),
				m_OwnerColor = new UnityEngine.Color(0.5f, 1f, 0.5f, 0.5f)
			};
			if (!m_RenderingSettingsQuery.IsEmptyIgnoreFilter)
			{
				RenderingSettingsData singleton = m_RenderingSettingsQuery.GetSingleton<RenderingSettingsData>();
				renderingSettingsData.m_HoveredColor = singleton.m_HoveredColor;
				renderingSettingsData.m_ErrorColor = singleton.m_ErrorColor;
				renderingSettingsData.m_WarningColor = singleton.m_WarningColor;
				renderingSettingsData.m_OwnerColor = singleton.m_OwnerColor;
			}
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle dependencies;
			JobHandle jobHandle = IJobExtensions.Schedule(new AreaBorderRenderJob
			{
				m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BatchType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Batch_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MapTileType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WarningType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Warning_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ColorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Color_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RenderingSettingsData = renderingSettingsData,
				m_InfoviewColors = m_ToolSystem.GetActiveInfoviewColors(),
				m_Chunks = chunks,
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_InfoviewEnabled = (m_ToolSystem.activeInfoview != null),
				m_OverlayBuffer = m_OverlayRenderSystem.GetBuffer(out dependencies)
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
			chunks.Dispose(jobHandle);
			m_OverlayRenderSystem.AddBufferWriter(jobHandle);
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
	public AreaBorderRenderSystem()
	{
	}
}
