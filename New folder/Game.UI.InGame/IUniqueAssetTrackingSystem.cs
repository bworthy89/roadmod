using System;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.InGame;

public interface IUniqueAssetTrackingSystem
{
	NativeParallelHashSet<Entity> placedUniqueAssets { get; }

	Action<Entity, bool> EventUniqueAssetStatusChanged { get; set; }
}
