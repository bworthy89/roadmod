using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Audio;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class TerrainToolSystem : ToolBaseSystem
{
	private enum State
	{
		Default,
		Adding,
		Removing
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_Brush;

		[ReadOnly]
		public float m_Size;

		[ReadOnly]
		public float m_Angle;

		[ReadOnly]
		public float m_Strength;

		[ReadOnly]
		public float m_Time;

		[ReadOnly]
		public float3 m_Target;

		[ReadOnly]
		public float3 m_ApplyStart;

		[ReadOnly]
		public ControlPoint m_StartPoint;

		[ReadOnly]
		public ControlPoint m_EndPoint;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (!m_EndPoint.Equals(default(ControlPoint)))
			{
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = m_Brush
				};
				BrushDefinition component2 = new BrushDefinition
				{
					m_Tool = m_Prefab
				};
				if (m_StartPoint.Equals(default(ControlPoint)))
				{
					component2.m_Line = new Line3.Segment(m_EndPoint.m_Position, m_EndPoint.m_Position);
				}
				else
				{
					component2.m_Line = new Line3.Segment(m_StartPoint.m_Position, m_EndPoint.m_Position);
				}
				component2.m_Size = m_Size;
				component2.m_Angle = m_Angle;
				component2.m_Strength = m_Strength;
				component2.m_Time = m_Time;
				component2.m_Target = m_Target;
				component2.m_Start = m_ApplyStart;
				Entity e = m_CommandBuffer.CreateEntity();
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, component2);
				m_CommandBuffer.AddComponent(e, default(Updated));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Brush_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>(isReadOnly: true);
		}
	}

	public const string kToolID = "Terrain Tool";

	public const string kTerrainToolKeyGroup = "tool/terrain";

	private AudioManager m_AudioManager;

	private AudioSource m_AudioSource;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_BrushQuery;

	private EntityQuery m_TempQuery;

	private EntityQuery m_SoundQuery;

	private EntityQuery m_VisibleQuery;

	private IProxyAction m_EraseMaterial;

	private IProxyAction m_EraseResource;

	private IProxyAction m_FastSoften;

	private IProxyAction m_LevelTerrain;

	private IProxyAction m_LowerTerrain;

	private IProxyAction m_PaintMaterial;

	private IProxyAction m_PaintResource;

	private IProxyAction m_RaiseTerrain;

	private IProxyAction m_SetLevelTarget;

	private IProxyAction m_SetSlopeTarget;

	private IProxyAction m_SlopeTerrain;

	private IProxyAction m_SoftenTerrain;

	private ControlPoint m_RaycastPoint;

	private ControlPoint m_StartPoint;

	private float3 m_TargetPosition;

	private float3 m_ApplyPosition;

	private bool m_TargetSet;

	private State m_State;

	private TypeHandle __TypeHandle;

	public override string toolID => "Terrain Tool";

	public TerraformingPrefab prefab { get; private set; }

	public override bool brushing => true;

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_EraseMaterial;
			yield return m_EraseResource;
			yield return m_FastSoften;
			yield return m_LevelTerrain;
			yield return m_LowerTerrain;
			yield return m_PaintMaterial;
			yield return m_PaintResource;
			yield return m_RaiseTerrain;
			yield return m_SetLevelTarget;
			yield return m_SetSlopeTarget;
			yield return m_SlopeTerrain;
			yield return m_SoftenTerrain;
		}
	}

	public float brushHeight
	{
		get
		{
			if (!m_TargetSet)
			{
				return m_WaterSystem.SeaLevel;
			}
			return m_TargetPosition.y;
		}
		set
		{
			m_TargetPosition.y = value;
			m_TargetSet = true;
		}
	}

	public void SetPrefab(TerraformingPrefab value)
	{
		m_TargetSet = false;
		m_TargetPosition = new float3(0f, 0f, 0f);
		m_ApplyPosition = new float3(0f, 0f, 0f);
		prefab = value;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_BrushQuery = GetBrushQuery();
		m_VisibleQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.ReadOnly<Temp>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_EraseMaterial = InputManager.instance.toolActionCollection.GetActionState("Erase Material", "TerrainToolSystem");
		m_EraseResource = InputManager.instance.toolActionCollection.GetActionState("Erase Resource", "TerrainToolSystem");
		m_FastSoften = InputManager.instance.toolActionCollection.GetActionState("Fast Soften", "TerrainToolSystem");
		m_LevelTerrain = InputManager.instance.toolActionCollection.GetActionState("Level Terrain", "TerrainToolSystem");
		m_LowerTerrain = InputManager.instance.toolActionCollection.GetActionState("Lower Terrain", "TerrainToolSystem");
		m_PaintMaterial = InputManager.instance.toolActionCollection.GetActionState("Paint Material", "TerrainToolSystem");
		m_PaintResource = InputManager.instance.toolActionCollection.GetActionState("Paint Resource", "TerrainToolSystem");
		m_RaiseTerrain = InputManager.instance.toolActionCollection.GetActionState("Raise Terrain", "TerrainToolSystem");
		m_SetLevelTarget = InputManager.instance.toolActionCollection.GetActionState("Set Level Target", "TerrainToolSystem");
		m_SetSlopeTarget = InputManager.instance.toolActionCollection.GetActionState("Set Slope Target", "TerrainToolSystem");
		m_SlopeTerrain = InputManager.instance.toolActionCollection.GetActionState("Slope Terrain", "TerrainToolSystem");
		m_SoftenTerrain = InputManager.instance.toolActionCollection.GetActionState("Soften Terrain", "TerrainToolSystem");
		base.brushSize = 100f;
		base.brushAngle = 0f;
		base.brushStrength = 0.5f;
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		base.brushType = FindDefaultBrush(m_BrushQuery);
		base.brushSize = 100f;
		base.brushAngle = 0f;
		base.brushStrength = 0.5f;
	}

	public void SetDisableFX()
	{
		if (m_AudioSource != null)
		{
			m_AudioManager.StopExclusiveUISound(m_AudioSource);
			m_AudioSource = null;
		}
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_RaycastPoint = default(ControlPoint);
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
			if (prefab.m_Type == TerraformingType.Shift)
			{
				if (prefab.m_Target == TerraformingTarget.Height)
				{
					base.applyActionOverride = m_RaiseTerrain;
					base.secondaryApplyActionOverride = m_LowerTerrain;
				}
				else if (prefab.m_Target == TerraformingTarget.Material)
				{
					base.applyActionOverride = m_PaintMaterial;
					base.secondaryApplyActionOverride = m_EraseMaterial;
				}
				else
				{
					base.applyActionOverride = m_PaintResource;
					base.secondaryApplyActionOverride = m_EraseResource;
				}
			}
			else if (prefab.m_Type == TerraformingType.Level)
			{
				base.applyActionOverride = m_LevelTerrain;
				base.secondaryApplyActionOverride = m_SetLevelTarget;
			}
			else if (prefab.m_Type == TerraformingType.Slope)
			{
				base.applyActionOverride = m_SlopeTerrain;
				base.secondaryApplyActionOverride = m_SetSlopeTarget;
			}
			else if (prefab.m_Type == TerraformingType.Soften)
			{
				base.applyActionOverride = m_SoftenTerrain;
				base.secondaryApplyActionOverride = m_FastSoften;
			}
			else
			{
				base.applyActionOverride = null;
				base.secondaryApplyActionOverride = null;
			}
		}
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is TerraformingPrefab terraformingPrefab)
		{
			SetPrefab(terraformingPrefab);
			return true;
		}
		return false;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		if (prefab != null && base.brushType != null)
		{
			m_ToolRaycastSystem.typeMask = TypeMask.Terrain;
			m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Outside;
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.None;
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		if (base.brushType == null)
		{
			base.brushType = FindDefaultBrush(m_BrushQuery);
		}
		base.requireNet = Layer.Road | Layer.TrainTrack | Layer.Pathway | Layer.TramTrack | Layer.SubwayTrack | Layer.PublicTransportRoad;
		base.requirePipelines = true;
		if (m_FocusChanged)
		{
			return inputDeps;
		}
		UpdateActions();
		if (prefab != null && base.brushType != null && m_HasFocus)
		{
			UpdateInfoview(m_PrefabSystem.GetEntity(prefab));
			GetAvailableSnapMask(out m_SnapOnMask, out m_SnapOffMask);
			if (m_State != State.Default && !base.applyAction.enabled)
			{
				m_State = State.Default;
			}
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				if (m_State != State.Default)
				{
					if (base.applyAction.WasPressedThisFrame() || base.applyAction.WasReleasedThisFrame())
					{
						return Apply(inputDeps);
					}
					if (base.secondaryApplyAction.WasPressedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
					{
						return Cancel(inputDeps);
					}
					return Update(inputDeps);
				}
				if (base.secondaryApplyAction.WasPressedThisFrame())
				{
					return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
				}
				if (base.applyAction.WasPressedThisFrame())
				{
					return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
				}
				return Update(inputDeps);
			}
		}
		else
		{
			UpdateInfoview(Entity.Null);
		}
		if (m_State != State.Default && (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame() || !m_HasFocus))
		{
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
		}
		return Clear(inputDeps);
	}

	public override void GetAvailableSnapMask(out Snap onMask, out Snap offMask)
	{
		base.GetAvailableSnapMask(out onMask, out offMask);
		if (prefab != null && prefab.m_Target == TerraformingTarget.Height)
		{
			onMask |= Snap.ContourLines;
			offMask |= Snap.ContourLines;
		}
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		SetDisableFX();
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (m_State == State.Default)
		{
			base.applyMode = ((prefab.m_Type != TerraformingType.Slope && GetAllowApply()) ? ApplyMode.Apply : ApplyMode.Clear);
			if (!singleFrameOnly)
			{
				m_StartPoint = m_RaycastPoint;
				m_State = State.Removing;
			}
			if (m_AudioSource == null && !m_ToolSystem.actionMode.IsEditor())
			{
				m_AudioSource = m_AudioManager.PlayExclusiveUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TerraformSound);
			}
			GetRaycastResult(out m_RaycastPoint);
			m_TargetSet = true;
			m_TargetPosition = m_RaycastPoint.m_HitPosition;
			inputDeps = InvertBrushes(m_TempQuery, inputDeps);
			return UpdateDefinitions(inputDeps);
		}
		if (m_State == State.Removing)
		{
			base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			SetDisableFX();
			return UpdateDefinitions(inputDeps);
		}
		base.applyMode = ApplyMode.Clear;
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		GetRaycastResult(out m_RaycastPoint);
		SetDisableFX();
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		if (m_State == State.Default)
		{
			base.applyMode = ((prefab.m_Type != TerraformingType.Slope && GetAllowApply()) ? ApplyMode.Apply : ApplyMode.Clear);
			if (!singleFrameOnly)
			{
				m_StartPoint = m_RaycastPoint;
				m_State = State.Adding;
			}
			if (m_AudioSource == null && !m_ToolSystem.actionMode.IsEditor())
			{
				m_AudioSource = m_AudioManager.PlayExclusiveUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TerraformSound);
			}
			GetRaycastResult(out m_RaycastPoint);
			m_ApplyPosition = m_RaycastPoint.m_HitPosition;
			return UpdateDefinitions(inputDeps);
		}
		if (m_State == State.Adding)
		{
			base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
			m_StartPoint = default(ControlPoint);
			m_State = State.Default;
			GetRaycastResult(out m_RaycastPoint);
			SetDisableFX();
			return UpdateDefinitions(inputDeps);
		}
		base.applyMode = ApplyMode.Clear;
		m_StartPoint = default(ControlPoint);
		m_State = State.Default;
		GetRaycastResult(out m_RaycastPoint);
		SetDisableFX();
		return UpdateDefinitions(inputDeps);
	}

	private JobHandle Update(JobHandle inputDeps)
	{
		if (GetRaycastResult(out var controlPoint))
		{
			if (m_State != State.Default)
			{
				base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
				m_StartPoint = m_RaycastPoint;
				m_RaycastPoint = controlPoint;
				return UpdateDefinitions(inputDeps);
			}
			if (m_RaycastPoint.Equals(controlPoint))
			{
				if (HaveBrushSettingsChanged())
				{
					base.applyMode = ApplyMode.Clear;
					return UpdateDefinitions(inputDeps);
				}
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			base.applyMode = ApplyMode.Clear;
			m_StartPoint = controlPoint;
			m_RaycastPoint = controlPoint;
			return UpdateDefinitions(inputDeps);
		}
		if (m_RaycastPoint.Equals(default(ControlPoint)))
		{
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		if (m_State != State.Default)
		{
			base.applyMode = (GetAllowApply() ? ApplyMode.Apply : ApplyMode.Clear);
			m_StartPoint = m_RaycastPoint;
			m_RaycastPoint = default(ControlPoint);
		}
		else
		{
			base.applyMode = ApplyMode.Clear;
			m_StartPoint = default(ControlPoint);
			m_RaycastPoint = default(ControlPoint);
		}
		return UpdateDefinitions(inputDeps);
	}

	private bool HaveBrushSettingsChanged()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_VisibleQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			ComponentTypeHandle<Brush> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NativeArray<Brush> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (!nativeArray2[j].m_Size.Equals(base.brushSize))
					{
						return true;
					}
				}
			}
			return false;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (prefab != null && base.brushType != null)
		{
			JobHandle jobHandle2 = IJobExtensions.Schedule(new CreateDefinitionsJob
			{
				m_Prefab = m_PrefabSystem.GetEntity(prefab),
				m_Brush = m_PrefabSystem.GetEntity(base.brushType),
				m_Size = base.brushSize,
				m_Angle = math.radians(base.brushAngle),
				m_Strength = ((m_State == State.Removing) ? (0f - base.brushStrength) : base.brushStrength),
				m_Time = UnityEngine.Time.deltaTime,
				m_StartPoint = m_StartPoint,
				m_EndPoint = m_RaycastPoint,
				m_Target = (m_TargetSet ? m_TargetPosition : m_RaycastPoint.m_HitPosition),
				m_ApplyStart = m_ApplyPosition,
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			}, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			if (base.applyMode == ApplyMode.Apply)
			{
				EnsureCachedBrushData();
			}
		}
		return jobHandle;
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
	public TerrainToolSystem()
	{
	}
}
