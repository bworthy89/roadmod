using Colossal.UI.Binding;
using Game.Rendering;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class CameraUISystem : UISystemBase
{
	private const string kGroup = "camera";

	private CameraUpdateSystem m_CameraUpdateSystem;

	private GetterValueBinding<Entity> m_FocusedEntityBinding;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_FocusedEntityBinding = new GetterValueBinding<Entity>("camera", "focusedEntity", GetFocusedEntity));
		AddBinding(new TriggerBinding<Entity>("camera", "focusEntity", FocusEntity));
		m_CameraUpdateSystem = base.World.GetOrCreateSystemManaged<CameraUpdateSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_FocusedEntityBinding.Update();
	}

	private Entity GetFocusedEntity()
	{
		if (!(m_CameraUpdateSystem.orbitCameraController != null))
		{
			return Entity.Null;
		}
		return m_CameraUpdateSystem.orbitCameraController.followedEntity;
	}

	private void FocusEntity(Entity entity)
	{
		if (entity != Entity.Null && m_CameraUpdateSystem.orbitCameraController != null && entity != m_CameraUpdateSystem.orbitCameraController.followedEntity)
		{
			m_CameraUpdateSystem.orbitCameraController.followedEntity = entity;
			m_CameraUpdateSystem.orbitCameraController.TryMatchPosition(m_CameraUpdateSystem.activeCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.orbitCameraController;
		}
		if (entity == Entity.Null && m_CameraUpdateSystem.activeCameraController == m_CameraUpdateSystem.orbitCameraController)
		{
			m_CameraUpdateSystem.gamePlayController.TryMatchPosition(m_CameraUpdateSystem.orbitCameraController);
			m_CameraUpdateSystem.activeCameraController = m_CameraUpdateSystem.gamePlayController;
		}
	}

	[Preserve]
	public CameraUISystem()
	{
	}
}
