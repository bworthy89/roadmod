using System;

namespace Game.Simulation;

[Flags]
public enum MailTransferRequestFlags : ushort
{
	Deliver = 1,
	Receive = 2,
	RequireTransport = 4,
	UnsortedMail = 0x10,
	LocalMail = 0x20,
	OutgoingMail = 0x40,
	ReturnUnsortedMail = 0x100,
	ReturnLocalMail = 0x200,
	ReturnOutgoingMail = 0x400
}
