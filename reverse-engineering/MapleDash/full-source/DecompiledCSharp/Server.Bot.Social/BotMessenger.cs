using System.Collections.Generic;

namespace Server.Bot.Social;

public sealed class BotMessenger
{
	public sealed class Entry
	{
		public long sequence;

		public int senderId;

		public string sender;

		public string text;

		public bool typing;
	}

	private sealed class Room
	{
		public int id;

		public readonly List<BotActor> members = new List<BotActor>();

		public readonly Dictionary<int, int> positions = new Dictionary<int, int>();

		public readonly List<Entry> messages = new List<Entry>();
	}

	private sealed class InvitationData
	{
		public Room room;

		public BotActor sender;

		public BotActor target;

		public int timeout;
	}

	private readonly Dictionary<int, Room> membership = new Dictionary<int, Room>();

	private readonly Dictionary<int, InvitationData> invites = new Dictionary<int, InvitationData>();

	private int nextRoom;

	private long nextMessage;

	public void Open(BotActor actor)
	{
		if (actor != null && !membership.ContainsKey(actor.id))
		{
			invites.Remove(actor.id);
			Room room = new Room
			{
				id = ++nextRoom
			};
			room.members.Add(actor);
			room.positions[actor.id] = 0;
			membership[actor.id] = room;
		}
	}

	public bool InviteMember(BotActor sender, BotActor target)
	{
		if (sender == null || target == null || !target.online || sender.world != target.world || sender.id == target.id || membership.ContainsKey(target.id) || invites.ContainsKey(target.id) || !membership.TryGetValue(sender.id, out var value) || value.members.Count >= 3)
		{
			return false;
		}
		invites[target.id] = new InvitationData
		{
			room = value,
			sender = sender,
			target = target
		};
		return true;
	}

	public bool Invite(BotActor sender, BotActor target)
	{
		if (sender == null || target == null || !target.online || sender.world != target.world || sender.id == target.id)
		{
			return false;
		}
		Leave(sender.id);
		Open(sender);
		if (invites.ContainsKey(target.id))
		{
			return false;
		}
		invites[target.id] = new InvitationData
		{
			room = membership[sender.id],
			sender = sender,
			target = target
		};
		return true;
	}

	public void TickInvitationTimeouts()
	{
		foreach (int item in new List<int>(invites.Keys))
		{
			if (invites[item].timeout > 5)
			{
				invites.Remove(item);
			}
			else
			{
				invites[item].timeout++;
			}
		}
	}

	public int Invitation(int id)
	{
		if (!invites.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value.room.id;
	}

	public string Inviter(int id)
	{
		if (!invites.TryGetValue(id, out var value))
		{
			return null;
		}
		return value.sender.name;
	}

	public bool Answer(int id, int roomId, bool accepted)
	{
		if (!invites.TryGetValue(id, out var value) || value.room.id != roomId)
		{
			return false;
		}
		invites.Remove(id);
		if (!accepted)
		{
			return true;
		}
		if (membership.ContainsKey(id) || value.room.members.Count >= 3 || !membership.TryGetValue(value.sender.id, out var value2) || value2 != value.room)
		{
			return false;
		}
		int i;
		for (i = 0; value.room.positions.ContainsValue(i); i++)
		{
		}
		value.room.positions[id] = i;
		value.room.members.Add(value.target);
		membership[id] = value.room;
		return true;
	}

	public bool Accepted(BotActor sender, BotActor target)
	{
		if (sender != null && target != null && membership.TryGetValue(sender.id, out var value))
		{
			return value.members.Exists((BotActor m) => m.name == target.name);
		}
		return false;
	}

	public bool Contains(int id)
	{
		return membership.ContainsKey(id);
	}

	public void Send(BotActor sender, string text, bool typing = false)
	{
		if (sender != null && membership.TryGetValue(sender.id, out var value))
		{
			value.messages.Add(new Entry
			{
				sequence = ++nextMessage,
				senderId = sender.id,
				sender = sender.name,
				text = text,
				typing = typing
			});
		}
	}

	public void Send(int id, string text)
	{
		if (membership.TryGetValue(id, out var value))
		{
			Send(value.members.Find((BotActor m) => m.id == id), text);
		}
	}

	public List<Entry> Messages(int id, long after)
	{
		if (!membership.TryGetValue(id, out var value))
		{
			return new List<Entry>();
		}
		return value.messages.FindAll((Entry m) => m.sequence > after && m.senderId != id);
	}

	public List<SocialMember> Members(int id)
	{
		List<SocialMember> list = new List<SocialMember>();
		if (membership.TryGetValue(id, out var value))
		{
			foreach (BotActor member in value.members)
			{
				list.Add(new SocialMember
				{
					id = member.id,
					name = member.name,
					level = member.level,
					messengerPosition = value.positions[member.id]
				});
			}
		}
		return list;
	}

	public void Leave(int id)
	{
		invites.Remove(id);
		if (!membership.TryGetValue(id, out var value))
		{
			return;
		}
		membership.Remove(id);
		value.members.RemoveAll((BotActor m) => m.id == id);
		value.positions.Remove(id);
		foreach (int item in new List<int>(invites.Keys))
		{
			if (invites[item].sender.id == id)
			{
				invites.Remove(item);
			}
		}
	}

	public void Clear()
	{
		membership.Clear();
		invites.Clear();
	}
}
