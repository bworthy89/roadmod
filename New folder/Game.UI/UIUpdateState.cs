using Game.Simulation;
using Unity.Entities;

namespace Game.UI;

public class UIUpdateState
{
	private readonly SimulationSystem m_SimulationSystem;

	private readonly uint m_UpdateInterval;

	private bool m_ForceUpdate;

	private uint m_LastTickIndex;

	private UIUpdateState(World world, int updateInterval)
	{
		m_SimulationSystem = world.GetOrCreateSystemManaged<SimulationSystem>();
		m_UpdateInterval = (uint)updateInterval;
		m_ForceUpdate = true;
	}

	public static UIUpdateState Create(World world, int updateInterval)
	{
		return new UIUpdateState(world, updateInterval);
	}

	public bool Advance()
	{
		uint num = m_SimulationSystem.frameIndex - m_LastTickIndex;
		if (m_ForceUpdate || num >= m_UpdateInterval)
		{
			m_LastTickIndex = m_SimulationSystem.frameIndex;
			m_ForceUpdate = false;
			return true;
		}
		return false;
	}

	public void ForceUpdate()
	{
		m_ForceUpdate = true;
	}
}
