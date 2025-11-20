using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class NetCourseTooltipSystem : TooltipSystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
		}
	}

	private const float kMinLength = 12f;

	private ToolSystem m_ToolSystem;

	private NetToolSystem m_NetTool;

	private EntityQuery m_NetCourseQuery;

	private TooltipGroup m_Group;

	private FloatTooltip m_Length;

	private FloatTooltip m_Slope;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NetTool = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_NetCourseQuery = GetEntityQuery(ComponentType.ReadOnly<CreationDefinition>(), ComponentType.ReadOnly<NetCourse>());
		RequireForUpdate(m_NetCourseQuery);
		m_Length = new FloatTooltip
		{
			icon = "Media/Glyphs/Length.svg",
			unit = "length"
		};
		m_Slope = new FloatTooltip
		{
			icon = "Media/Glyphs/Slope.svg",
			unit = "percentageSingleFraction",
			signed = true
		};
		m_Group = new TooltipGroup
		{
			path = "tempNetEdgeStart",
			horizontalAlignment = TooltipGroup.Alignment.Center,
			verticalAlignment = TooltipGroup.Alignment.Center,
			category = TooltipGroup.Category.Network
		};
		m_Group.children.Add(m_Length);
		m_Group.children.Add(m_Slope);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool != m_NetTool || m_NetTool.mode == NetToolSystem.Mode.Replace || !(Camera.main != null))
		{
			return;
		}
		CompleteDependency();
		NativeList<NetCourse> courses = new NativeList<NetCourse>(m_NetCourseQuery.CalculateEntityCount(), Allocator.Temp);
		NativeArray<ArchetypeChunk> nativeArray = m_NetCourseQuery.ToArchetypeChunkArray(Allocator.Temp);
		try
		{
			ComponentTypeHandle<NetCourse> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<CreationDefinition> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			float num = 0f;
			float num2 = 0f;
			foreach (ArchetypeChunk item in nativeArray)
			{
				NativeArray<NetCourse> nativeArray2 = item.GetNativeArray(ref typeHandle);
				NativeArray<CreationDefinition> nativeArray3 = item.GetNativeArray(ref typeHandle2);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					NetCourse value = nativeArray2[i];
					CreationDefinition creationDefinition = nativeArray3[i];
					if (!(creationDefinition.m_Original != Entity.Null) && (creationDefinition.m_Flags & (CreationFlags.Permanent | CreationFlags.Delete | CreationFlags.Upgrade | CreationFlags.Invert | CreationFlags.Align)) == 0 && (value.m_StartPosition.m_Flags & CoursePosFlags.IsParallel) == 0)
					{
						num += value.m_Length;
						Bezier4x2 xz = MathUtils.Cut(t: new float2(value.m_StartPosition.m_CourseDelta, value.m_EndPosition.m_CourseDelta), curve: value.m_Curve).xz;
						num2 += MathUtils.Length(xz);
						courses.Add(in value);
					}
				}
			}
			m_Length.value = num2;
			if (courses.Length != 0 && num2 >= 12f)
			{
				float y = courses[0].m_StartPosition.m_Position.y;
				float y2 = courses[courses.Length - 1].m_EndPosition.m_Position.y;
				float num3 = 100f * (y2 - y) / num2;
				m_Slope.value = math.select(num3, 0f, math.abs(num3) < 0.05f);
				SortCourses(courses);
				float length = num / 2f;
				bool onScreen;
				float2 @float = TooltipSystemBase.WorldToTooltipPos(GetWorldPosition(courses, length), out onScreen);
				if (!m_Group.position.Equals(@float))
				{
					m_Group.position = @float;
					m_Group.SetChildrenChanged();
				}
				if (onScreen)
				{
					AddGroup(m_Group);
					return;
				}
				AddMouseTooltip(m_Length);
				AddMouseTooltip(m_Slope);
			}
		}
		finally
		{
			courses.Dispose();
			nativeArray.Dispose();
		}
	}

	private static float3 GetWorldPosition(NativeList<NetCourse> courses, float length)
	{
		float num = 0f - length;
		foreach (NetCourse item in courses)
		{
			num += item.m_Length;
			if (num >= 0f && item.m_Length != 0f)
			{
				float t = math.lerp(item.m_StartPosition.m_CourseDelta, item.m_EndPosition.m_CourseDelta, 1f - num / item.m_Length);
				return MathUtils.Position(item.m_Curve, t);
			}
		}
		return courses[courses.Length - 1].m_EndPosition.m_Position;
	}

	private static void SortCourses(NativeList<NetCourse> courses)
	{
		NativeArray<NetCourse> nativeArray = courses.AsArray();
		for (int i = 0; i < nativeArray.Length; i++)
		{
			NetCourse value = courses[i];
			if ((value.m_StartPosition.m_Flags & CoursePosFlags.IsFirst) != 0)
			{
				courses[i] = courses[0];
				courses[0] = value;
				break;
			}
		}
		for (int j = 0; j < courses.Length - 1; j++)
		{
			NetCourse netCourse = courses[j];
			for (int k = j + 1; k < courses.Length; k++)
			{
				NetCourse value2 = courses[k];
				if (netCourse.m_EndPosition.m_Position.Equals(value2.m_StartPosition.m_Position))
				{
					courses[k] = courses[j + 1];
					courses[j + 1] = value2;
					break;
				}
			}
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
	public NetCourseTooltipSystem()
	{
	}
}
