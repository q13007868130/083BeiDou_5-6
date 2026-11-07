using System;
using System.Collections.Generic;
using Server.Bot;
using Server.Bot.Social;

public sealed class NativeSocialService
{
	private readonly DbStore db;

	public readonly NativeGmCommands Commands;

	public Action<SocialRequest> ChatAccepted;

	public readonly BotMessenger BotMessenger = new BotMessenger();

	public readonly BotPartyService Parties;

	private int messengerOwner;

	private int searchOwner;

	private readonly Dictionary<int, long> lastChat = new Dictionary<int, long>();

	public NativeSocialService(DbStore db)
	{
		this.db = db;
		Commands = new NativeGmCommands(db);
		Parties = new BotPartyService(db);
	}

	public void EndSession(int characterId)
	{
		Parties.EndSession(characterId);
		if (searchOwner == characterId)
		{
			searchOwner = 0;
		}
		if (messengerOwner == characterId)
		{
			messengerOwner = 0;
		}
		BotMessenger.Leave(characterId);
		lastChat.Remove(characterId);
	}

	public SocialResponse Handle(SocialRequest request)
	{
		CharRecord self = CharactersDao.FindById(db, request.characterId);
		if (self == null)
		{
			return new SocialResponse
			{
				ok = false,
				error = "角色不存在"
			};
		}
		if (request.operation == SocialOperation.CharacterInfo)
		{
			BotActor botActor = ((request.memberId == 0 || request.memberId == self.id) ? null : Parties.Get(request.memberId));
			BotActor botActor2 = Parties.Get((int)self.id);
			if (request.memberId != 0 && request.memberId != self.id && (botActor == null || !botActor.online || botActor.world != self.world || botActor.mapId != self.map))
			{
				return new SocialResponse
				{
					ok = false,
					error = "该角色不在当前地图"
				};
			}
			if (botActor != null && botActor2 != null && botActor.channel != botActor2.channel)
			{
				return new SocialResponse
				{
					ok = false,
					error = "该角色不在当前频道"
				};
			}
			return new SocialResponse
			{
				characterInfo = NativeCharacterInfoData.Read(db, self, botActor)
			};
		}
		string text = null;
		NativeCommandResult nativeCommandResult = null;
		switch (request.operation)
		{
		case SocialOperation.SetBuddyCapacity:
			self.buddyCapacity = request.buddyCapacity;
			CharactersDao.Save(db, self);
			db.Save();
			break;
		case SocialOperation.AddBuddy:
			text = AddBuddy(self, request);
			break;
		case SocialOperation.AcceptBuddy:
			text = AcceptBuddy(self, request.target);
			break;
		case SocialOperation.DeleteBuddy:
		{
			CharRecord other = Find(request.target, self.world);
			if (other != null)
			{
				db.Table("buddies").Delete((DbRow r) => DbStore.Int(r, "characterid") == self.id && DbStore.Int(r, "buddyid") == other.id);
			}
			db.Save();
			break;
		}
		case SocialOperation.CreateParty:
			if (Parties.For((int)self.id) != null)
			{
				text = "已经加入组队";
			}
			else if (self.level < 10 && !GameConfigDao.GetBool(db, "use_party_for_starters"))
			{
				text = "达到10级才能创建组队";
			}
			else if (!Parties.Create(Parties.Actor(self)))
			{
				text = "无法创建组队";
			}
			break;
		case SocialOperation.LeaveParty:
			Parties.Leave((int)self.id);
			if (searchOwner == self.id)
			{
				searchOwner = 0;
			}
			break;
		case SocialOperation.ExpelParty:
			if (!Parties.Expel((int)self.id, request.memberId))
			{
				text = "无法逐出该队员";
			}
			break;
		case SocialOperation.ChangePartyLeader:
			if (!Parties.ChangeLeader((int)self.id, request.memberId))
			{
				text = "该角色不在队伍中";
			}
			break;
		case SocialOperation.InviteParty:
		{
			if (request.target == self.name)
			{
				text = "不能邀请自己";
				break;
			}
			BotActor botActor3 = Parties.Find(request.target, self.world);
			if (botActor3 == null)
			{
				text = "该角色不在线";
			}
			else if (!Parties.Invite(Parties.Actor(self), botActor3))
			{
				text = "无法发出组队邀请";
			}
			break;
		}
		case SocialOperation.AcceptParty:
		case SocialOperation.DeclineParty:
			if (!Parties.Answer(Parties.Actor(self), request.partyId, request.operation == SocialOperation.AcceptParty))
			{
				text = "组队邀请已失效";
			}
			break;
		case SocialOperation.SearchParty:
			if (request.minLevel > request.maxLevel)
			{
				text = "最低等级不能高于最高等级";
			}
			else if (request.maxLevel - request.minLevel > 30)
			{
				text = "搜索等级范围不能超过30级";
			}
			else if (self.level < request.minLevel || self.level > request.maxLevel)
			{
				text = "搜索范围必须包含自己的等级";
			}
			else if (Parties.For((int)self.id)?.leader != self.id)
			{
				text = "只有队长可以发起组队搜索";
			}
			else
			{
				searchOwner = (int)self.id;
			}
			break;
		case SocialOperation.StopSearch:
			if (searchOwner == self.id)
			{
				searchOwner = 0;
			}
			break;
		case SocialOperation.OpenMessenger:
			messengerOwner = (int)self.id;
			BotMessenger.Open(Parties.Actor(self));
			break;
		case SocialOperation.CloseMessenger:
			if (messengerOwner == self.id)
			{
				messengerOwner = 0;
			}
			BotMessenger.Leave((int)self.id);
			break;
		case SocialOperation.AcceptMessenger:
		case SocialOperation.DeclineMessenger:
			if (!BotMessenger.Answer((int)self.id, request.messengerRoom, request.operation == SocialOperation.AcceptMessenger))
			{
				text = "聊天邀请已失效";
			}
			break;
		case SocialOperation.InviteMessenger:
		{
			if (request.target == self.name)
			{
				text = "不能邀请自己";
				break;
			}
			BotActor botActor4 = Parties.Find(request.target, self.world);
			if (botActor4 == null)
			{
				text = "该角色不在线";
			}
			else if (!BotMessenger.InviteMember(Parties.Actor(self), botActor4))
			{
				text = "无法发出聊天邀请";
			}
			break;
		}
		case SocialOperation.SendChat:
			if (NativeGmCommands.IsCommand(self, request.text))
			{
				nativeCommandResult = ((request.text.Length > 127) ? NativeCommandResult.Fail("聊天内容不能超过127个字符") : Commands.Execute(self, request.text));
				if (!nativeCommandResult.ok)
				{
					text = nativeCommandResult.error;
				}
			}
			else
			{
				text = ValidateChat(self, request);
			}
			break;
		case SocialOperation.AddMonsterCard:
		{
			if (request.cardId / 10000 != 238)
			{
				text = "无效怪物卡片";
				break;
			}
			DbRow dbRow = db.Table("monsterbook").Find((DbRow r) => DbStore.Int(r, "charid") == self.id && DbStore.Int(r, "cardid") == request.cardId);
			if (dbRow == null)
			{
				db.Table("monsterbook").Insert(new DbRow
				{
					["charid"] = self.id,
					["cardid"] = (long)request.cardId,
					["level"] = 1L
				});
			}
			else
			{
				dbRow["level"] = (long)Math.Min(5, DbStore.Int(dbRow, "level") + 1);
			}
			db.Save();
			break;
		}
		case SocialOperation.SetBookCover:
			if (request.cardId != 0 && request.cardId / 10000 != 238)
			{
				text = "无效怪物卡片封面";
				break;
			}
			self.monsterbookcover = request.cardId;
			CharactersDao.Save(db, self);
			db.Save();
			break;
		}
		SocialResponse socialResponse = new SocialResponse
		{
			command = nativeCommandResult,
			ok = (text == null),
			error = (text ?? ""),
			capacity = self.buddyCapacity,
			party = (Parties.For((int)self.id) != null),
			searching = (searchOwner == self.id),
			messenger = (messengerOwner == self.id || BotMessenger.Contains((int)self.id)),
			messengerInvite = BotMessenger.Invitation((int)self.id),
			messengerInviter = BotMessenger.Inviter((int)self.id),
			messengerMembers = BotMessenger.Members((int)self.id),
			messengerMessages = BotMessenger.Messages((int)self.id, request.afterMessengerMessage)
		};
		foreach (SocialMember messengerMember in socialResponse.messengerMembers)
		{
			if (Parties.Get(messengerMember.id) is BotCharacter actor)
			{
				messengerMember.appearance = BotAppearance.CharacterIds(actor);
			}
		}
		BotPartyService.Party party = Parties.For((int)self.id);
		socialResponse.partyId = party?.id ?? 0;
		socialResponse.partyLeader = party?.leader ?? 0;
		socialResponse.partyInvite = Parties.Pending((int)self.id)?.party ?? 0;
		foreach (BotActor item in Parties.Members((int)self.id))
		{
			socialResponse.partyMembers.Add(new SocialMember
			{
				id = item.id,
				name = item.name,
				level = item.level,
				job = item.job,
				mapId = item.mapId,
				channel = item.channel
			});
		}
		foreach (DbRow item2 in from r in db.Table("buddies")
			select DbStore.Int(r, "characterid") == self.id)
		{
			CharRecord charRecord = CharactersDao.FindById(db, DbStore.Int(item2, "buddyid"));
			if (charRecord != null)
			{
				socialResponse.buddies.Add(new SocialMember
				{
					id = (int)charRecord.id,
					name = charRecord.name,
					level = charRecord.level,
					group = DbStore.Str(item2, "group"),
					pending = (DbStore.Int(item2, "pending") != 0)
				});
			}
		}
		foreach (DbRow item3 in from r in db.Table("monsterbook")
			select DbStore.Int(r, "charid") == self.id)
		{
			socialResponse.cards[DbStore.Int(item3, "cardid")] = DbStore.Int(item3, "level");
		}
		int num = 1;
		while (true)
		{
			num += socialResponse.bookLevel * 10;
			if (socialResponse.cards.Count < num)
			{
				break;
			}
			socialResponse.bookLevel++;
		}
		socialResponse.bookCover = self.monsterbookcover;
		return socialResponse;
	}

	private CharRecord Find(string name, int world)
	{
		DbRow dbRow = db.Table("characters").Find((DbRow r) => DbStore.Int(r, "world") == world && string.Equals(DbStore.Str(r, "name"), name, StringComparison.OrdinalIgnoreCase));
		if (dbRow != null)
		{
			return CharactersDao.FindById(db, DbStore.Int(dbRow, "id"));
		}
		return null;
	}

	private bool IsOnline(string name, int world, CharRecord self)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (string.Equals(name, self.name, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return Parties.FindIgnoreCase(name, world) != null;
	}

	private int BuddyCount(long id)
	{
		return (from r in db.Table("buddies")
			select DbStore.Int(r, "characterid") == id && DbStore.Int(r, "pending") == 0).Count;
	}

	private DbRow Buddy(long id, long other)
	{
		return db.Table("buddies").Find((DbRow r) => DbStore.Int(r, "characterid") == id && DbStore.Int(r, "buddyid") == other);
	}

	private string AddBuddy(CharRecord self, SocialRequest request)
	{
		if (request.target == null || request.target.Length < 2 || request.target.Length > 13 || request.group == null || request.group.Length > 16)
		{
			return "角色名或分组名称长度无效";
		}
		CharRecord charRecord = Find(request.target, self.world);
		if (charRecord == null)
		{
			return "角色不存在";
		}
		if (charRecord.id == self.id)
		{
			return "不能添加自己";
		}
		DbRow dbRow = Buddy(self.id, charRecord.id);
		if (dbRow != null && DbStore.Int(dbRow, "pending") == 0)
		{
			dbRow["group"] = request.group;
			db.Save();
			return null;
		}
		if (BuddyCount(self.id) >= self.buddyCapacity)
		{
			return "好友列表已满";
		}
		if (BuddyCount(charRecord.id) >= charRecord.buddyCapacity)
		{
			return "对方好友列表已满";
		}
		DbRow dbRow2 = Buddy(charRecord.id, self.id);
		PutBuddy(self.id, charRecord.id, request.group, 0);
		if (dbRow2 == null)
		{
			PutBuddy(charRecord.id, self.id, "Default Group", 1);
		}
		db.Save();
		return null;
	}

	private void PutBuddy(long id, long other, string group, int pending)
	{
		DbRow dbRow = Buddy(id, other);
		if (dbRow != null)
		{
			dbRow["pending"] = (long)pending;
			dbRow["group"] = group;
			return;
		}
		db.Table("buddies").Insert(new DbRow
		{
			["characterid"] = id,
			["buddyid"] = other,
			["group"] = group,
			["pending"] = (long)pending
		});
	}

	private string AcceptBuddy(CharRecord self, string name)
	{
		CharRecord charRecord = Find(name, self.world);
		DbRow dbRow = ((charRecord == null) ? null : Buddy(self.id, charRecord.id));
		if (dbRow == null || DbStore.Int(dbRow, "pending") == 0)
		{
			return "好友申请已失效";
		}
		if (BuddyCount(self.id) >= self.buddyCapacity)
		{
			return "好友列表已满";
		}
		dbRow["pending"] = 0L;
		db.Save();
		return null;
	}

	private string ValidateChat(CharRecord self, SocialRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.text))
		{
			return "请输入聊天内容";
		}
		if (request.text.Length > 127)
		{
			return "聊天内容不能超过127个字符";
		}
		long num = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		if (lastChat.TryGetValue((int)self.id, out var value) && num - value < 200)
		{
			return "发送过于频繁";
		}
		switch (request.channel)
		{
		case NativeChatChannel.Whisper:
			if (!IsOnline(request.target, self.world, self))
			{
				if (Find(request.target, self.world) != null)
				{
					return "对方不在线";
				}
				return "角色不存在";
			}
			break;
		case NativeChatChannel.Party:
			if (Parties.For((int)self.id) == null)
			{
				return "尚未加入组队";
			}
			break;
		case NativeChatChannel.Buddy:
			if (BuddyCount(self.id) == 0)
			{
				return "好友列表为空";
			}
			break;
		case NativeChatChannel.Guild:
			if (self.guildid <= 0)
			{
				return "尚未加入家族";
			}
			break;
		case NativeChatChannel.Alliance:
			return "尚未加入联盟";
		case NativeChatChannel.Spouse:
			if (self.partnerId > 0)
			{
				return "配偶不在线";
			}
			return "尚未结婚";
		case NativeChatChannel.Messenger:
			if (messengerOwner != self.id && !BotMessenger.Contains((int)self.id))
			{
				return "讯息会话已关闭";
			}
			break;
		}
		lastChat[(int)self.id] = num;
		if (request.channel == NativeChatChannel.Messenger)
		{
			BotMessenger.Send((int)self.id, request.text);
		}
		ChatAccepted?.Invoke(request);
		return null;
	}
}
