using System;
using System.Collections.Generic;

namespace Game.UI;

internal class FingerprintState
{
	public LinkedListNode<ErrorEntry> node;

	public DateTime? cooldownUntilUtc;

	public int count;

	public List<long> timestampsMs;

	public bool isSpam;

	public DateTime lastSeenUtc;
}
