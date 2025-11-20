using Unity.Collections;
using UnityEngine.Rendering;

namespace Game.Simulation;

public interface IWaterSimulation
{
	float MaxVelocity { get; set; }

	float Damping { get; set; }

	float Evaporation { get; set; }

	float RainConstant { get; set; }

	float PollutionDecayRate { get; set; }

	float Fluidness { get; set; }

	float WindVelocityScale { get; set; }

	float WaterSourceSpeed { get; set; }

	void SourceStep(CommandBuffer cmd, NativeList<WaterSourceCache> LastFrameSourceCache);

	void EvaporateStep(CommandBuffer cmd);

	void VelocityStep(CommandBuffer cmd);

	void DepthStep(CommandBuffer cmd);

	void OnDestroy();
}
