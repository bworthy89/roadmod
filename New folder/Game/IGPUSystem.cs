using UnityEngine.Rendering;

namespace Game;

public interface IGPUSystem
{
	bool Enabled { get; }

	bool IsAsync { get; set; }

	void OnSimulateGPU(CommandBuffer cmd);
}
