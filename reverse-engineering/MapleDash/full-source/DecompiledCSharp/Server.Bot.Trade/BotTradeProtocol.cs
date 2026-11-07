using System;

namespace Server.Bot.Trade;

public sealed class BotTradeProtocol
{
	private readonly BotTradeService service;

	private readonly Func<int, BotActor> players;

	public BotTradeProtocol(BotTradeService service, Func<int, BotActor> players)
	{
		this.service = service;
		this.players = players;
	}

	public BotTradeResponse Handle(BotTradeRequest request)
	{
		BotActor botActor = players(request.characterId);
		if (botActor == null || !botActor.online || botActor.IsBot())
		{
			return new BotTradeResponse
			{
				ok = false,
				error = "角色不在线"
			};
		}
		string text = null;
		if (request.operation == BotTradeOperation.Invite)
		{
			BotCharacter botCharacter = BotRegistry.Get(request.botId);
			if (botCharacter == null || !botCharacter.online || botCharacter.mapId != botActor.mapId || botCharacter.world != botActor.world || botCharacter.channel != botActor.channel)
			{
				text = "交易对象不在当前地图";
			}
			else if (!service.Invite(botActor, botCharacter))
			{
				text = "交易对象正忙";
			}
		}
		else if (request.operation != BotTradeOperation.Read)
		{
			if (!(botActor.trade is BotTradeService.Session session))
			{
				text = "交易已结束";
			}
			else if (request.operation == BotTradeOperation.Cancel)
			{
				session.Decline();
			}
			else if (request.operation == BotTradeOperation.Accept)
			{
				if (!session.IsIncoming || session.partner?.actor.id != request.botId)
				{
					text = "交易邀请已变更";
				}
				else
				{
					session.Accept();
					if (!session.FullTrade)
					{
						text = "交易邀请已过期";
					}
				}
			}
			else if (!session.FullTrade)
			{
				text = "对方尚未接受交易";
			}
			else
			{
				switch (request.operation)
				{
				case BotTradeOperation.OfferItem:
					if (!session.OfferFromInventory(request.inventoryType, request.inventoryPosition, request.quantity, request.slot))
					{
						text = "不能放入该物品";
					}
					break;
				case BotTradeOperation.OfferMeso:
					if (request.meso < 0 || request.meso > botActor.character.record.meso || session.locked)
					{
						text = "金币不足或交易已锁定";
					}
					else
					{
						session.SetMeso(request.meso);
					}
					break;
				case BotTradeOperation.Confirm:
					if (session.partner?.actor == null)
					{
						text = "交易对象已离开";
					}
					else
					{
						session.Confirm();
					}
					break;
				case BotTradeOperation.Chat:
					if (string.IsNullOrWhiteSpace(request.text) || request.text.Length > 127)
					{
						text = "聊天内容无效";
					}
					else
					{
						session.Chat(request.text);
					}
					break;
				}
			}
		}
		BotTradeService.Session session2 = botActor.trade as BotTradeService.Session;
		BotTradeResponse botTradeResponse = new BotTradeResponse
		{
			ok = (text == null),
			error = text,
			character = botActor.character,
			active = (session2 != null)
		};
		if (session2 != null)
		{
			botTradeResponse.accepted = session2.FullTrade;
			botTradeResponse.locked = session2.locked;
			botTradeResponse.incomingInvitation = session2.IsIncoming && session2.InvitationPending;
			botTradeResponse.meso = session2.meso;
			botTradeResponse.items.AddRange(session2.Items);
			if (session2.partner != null)
			{
				botTradeResponse.partnerAppearance = ((session2.partner.actor is BotCharacter actor) ? BotAppearance.CharacterIds(actor) : null);
				botTradeResponse.partnerId = session2.partner.actor.id;
				botTradeResponse.partnerName = session2.partner.actor.name;
				botTradeResponse.partnerMeso = session2.partner.meso;
				botTradeResponse.partnerLocked = session2.partner.locked;
				botTradeResponse.partnerItems.AddRange(session2.partner.Items);
			}
		}
		return botTradeResponse;
	}
}
