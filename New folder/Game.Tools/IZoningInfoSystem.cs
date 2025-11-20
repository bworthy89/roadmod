using Game.Simulation;
using Unity.Collections;

namespace Game.Tools;

public interface IZoningInfoSystem
{
	NativeList<ZoneEvaluationUtils.ZoningEvaluationResult> evaluationResults { get; }
}
