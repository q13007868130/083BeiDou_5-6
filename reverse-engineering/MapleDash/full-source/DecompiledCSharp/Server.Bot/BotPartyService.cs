using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class BotPartyService
{
	public sealed class Party
	{
		public int id;

		public int leader;

		public readonly List<int> members = new List<int>();
	}

	public sealed class Invitation
	{
		public int party;

		public int timeout;

		public BotActor inviter;
	}

	private readonly DbStore db;

	private readonly Dictionary<int, Party> parties = new Dictionary<int, Party>();

	private readonly Dictionary<int, int> membership = new Dictionary<int, int>();

	private readonly Dictionary<int, Invitation> invitations = new Dictionary<int, Invitation>();

	private readonly Dictionary<int, BotActor> players = new Dictionary<int, BotActor>();

	private int nextId = 1000000001;

	public event Action<int> Left;

	public BotPartyService(DbStore db)
	{
		this.db = db;
	}

	public void Bind(BotActor actor)
	{
		if (!actor.IsBot())
		{
			players[actor.id] = actor;
		}
	}

	public BotActor Actor(CharRecord record)
	{
		if (players.TryGetValue((int)record.id, out var value))
		{
			return value;
		}
		BotActor botActor = new BotActor
		{
			id = (int)record.id,
			name = record.name,
			world = record.world,
			level = record.level,
			job = record.job,
			mapId = record.map
		};
		players[botActor.id] = botActor;
		return botActor;
	}

	public BotActor Find(string name, int world)
	{
		foreach (BotActor value in players.Values)
		{
			if (value.online && value.world == world && value.name == name)
			{
				return value;
			}
		}
		foreach (BotCharacter value2 in BotRegistry.All().Values)
		{
			if (value2.online && value2.world == world && value2.name == name)
			{
				return value2;
			}
		}
		return null;
	}

	public BotActor FindIgnoreCase(string name, int world)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		foreach (BotActor value in players.Values)
		{
			if (value.online && value.world == world && string.Equals(value.name, name, StringComparison.OrdinalIgnoreCase))
			{
				return value;
			}
		}
		foreach (BotCharacter value2 in BotRegistry.All().Values)
		{
			if (value2.online && value2.world == world && string.Equals(value2.name, name, StringComparison.OrdinalIgnoreCase))
			{
				return value2;
			}
		}
		return null;
	}

	public BotActor Get(int id)
	{
		if (!players.TryGetValue(id, out var value))
		{
			return BotRegistry.Get(id);
		}
		return value;
	}

	public Party For(int id)
	{
		if (!membership.TryGetValue(id, out var value) || !parties.TryGetValue(value, out var value2))
		{
			return null;
		}
		return value2;
	}

	public Party ById(int partyId)
	{
		if (!parties.TryGetValue(partyId, out var value))
		{
			return null;
		}
		return value;
	}

	private void Persist(int id, int party)
	{
		BotActor botActor = Get(id);
		if (botActor != null && !botActor.IsBot())
		{
			db.Table("characters").Update((DbRow r) => DbStore.Int(r, "id") == id, new DbRow { ["party"] = (long)party });
			if (Get(id).character != null)
			{
				Get(id).character.record.party = party;
			}
		}
	}

	public bool Create(BotActor actor)
	{
		if (actor == null || For(actor.id) != null || (actor.level < 10 && !GameConfigDao.GetBool(db, "use_party_for_starters")))
		{
			return false;
		}
		Bind(actor);
		Party party = new Party
		{
			id = nextId++,
			leader = actor.id
		};
		party.members.Add(actor.id);
		parties[party.id] = party;
		membership[actor.id] = party.id;
		Persist(actor.id, party.id);
		db.Save();
		return true;
	}

	public bool Join(BotActor actor, int partyId)
	{
		if (actor == null || For(actor.id) != null || !parties.TryGetValue(partyId, out var value) || value.members.Count >= 6)
		{
			return false;
		}
		BotActor botActor = Get(value.leader);
		if (botActor == null || botActor.world != actor.world)
		{
			return false;
		}
		Bind(actor);
		value.members.Add(actor.id);
		membership[actor.id] = partyId;
		Persist(actor.id, partyId);
		db.Save();
		return true;
	}

	public bool Invite(BotActor sender, BotActor target)
	{
		if (sender == null || target == null || sender.id == target.id || !target.online || sender.world != target.world || For(target.id) != null || invitations.ContainsKey(target.id))
		{
			return false;
		}
		bool flag = GameConfigDao.GetBool(db, "use_party_for_starters");
		if ((target.level < 10 && (!flag || sender.level >= 10)) || (flag && target.level >= 10 && sender.level < 10))
		{
			return false;
		}
		Party party = For(sender.id);
		if (party == null)
		{
			if (!Create(sender))
			{
				return false;
			}
			party = For(sender.id);
		}
		if (party.members.Count >= 6 || (sender.IsBot() && party.leader != sender.id))
		{
			return false;
		}
		invitations[target.id] = new Invitation
		{
			party = party.id,
			inviter = sender
		};
		return true;
	}

	public void TickInvitationTimeouts()
	{
		foreach (int item in new List<int>(invitations.Keys))
		{
			if (invitations[item].timeout > 5)
			{
				invitations.Remove(item);
			}
			else
			{
				invitations[item].timeout++;
			}
		}
	}

	public Invitation Pending(int id)
	{
		if (!invitations.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public bool Answer(BotActor actor, int partyId, bool accepted)
	{
		if (!invitations.TryGetValue(actor.id, out var value) || value.party != partyId)
		{
			return false;
		}
		invitations.Remove(actor.id);
		if (accepted)
		{
			return Join(actor, partyId);
		}
		return true;
	}

	public void Leave(int id)
	{
		invitations.Remove(id);
		Party party = For(id);
		if (party == null)
		{
			return;
		}
		if (party.leader == id)
		{
			parties.Remove(party.id);
			foreach (int item in new List<int>(party.members))
			{
				membership.Remove(item);
				Persist(item, 0);
				Left?.Invoke(item);
			}
			foreach (int item2 in new List<int>(invitations.Keys))
			{
				if (invitations[item2].party == party.id)
				{
					invitations.Remove(item2);
				}
			}
		}
		else
		{
			membership.Remove(id);
			party.members.Remove(id);
			Persist(id, 0);
			Left?.Invoke(id);
		}
		db.Save();
	}

	public bool Expel(int sender, int member)
	{
		Party party = For(sender);
		if (party == null || party.leader != sender || !party.members.Contains(member))
		{
			return false;
		}
		membership.Remove(member);
		party.members.Remove(member);
		Persist(member, 0);
		Left?.Invoke(member);
		db.Save();
		return true;
	}

	public bool ChangeLeader(int sender, int member)
	{
		Party party = For(sender);
		if (party == null || !party.members.Contains(member))
		{
			return false;
		}
		party.leader = member;
		return true;
	}

	public void EndSession(int id)
	{
		Leave(id);
		players.Remove(id);
	}

	public IEnumerable<BotActor> Members(int id)
	{
		Party party = For(id);
		if (party == null)
		{
			yield break;
		}
		foreach (int member in party.members)
		{
			BotActor botActor = Get(member);
			if (botActor != null)
			{
				yield return botActor;
			}
		}
	}

	public void Clear()
	{
		foreach (int item in new List<int>(membership.Keys))
		{
			Leave(item);
		}
		parties.Clear();
		invitations.Clear();
		players.Clear();
	}
}
