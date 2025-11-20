using Game.Input;
using Unity.Entities;

namespace Game.Tutorials;

public struct ControlSchemeActivationData : IComponentData, IQueryTypeParameter
{
	public InputManager.ControlScheme m_ControlScheme;

	public ControlSchemeActivationData(InputManager.ControlScheme controlScheme)
	{
		m_ControlScheme = controlScheme;
	}
}
