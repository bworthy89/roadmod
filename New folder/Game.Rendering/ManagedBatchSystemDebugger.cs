using System.Collections.Generic;
using UnityEngine;

namespace Game.Rendering;

public class ManagedBatchSystemDebugger : MonoBehaviour
{
	public const string kSectionName = "======System======";

	public ManagedBatchSystem managedBatchSystem { get; set; }

	public IReadOnlyDictionary<ManagedBatchSystem.MaterialKey, Material> materials => managedBatchSystem?.materials;
}
