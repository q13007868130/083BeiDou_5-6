using System;
using System.Collections.Generic;

namespace Server.Bot;

public sealed class MarketShopProtocol
{
	private readonly MarketShopService service;

	private readonly Func<int, BotActor> players;

	public MarketShopProtocol(MarketShopService service, Func<int, BotActor> players)
	{
		this.service = service;
		this.players = players;
	}

	public MarketResponse Handle(MarketRequest request)
	{
		BotActor botActor = players(request.characterId);
		if (botActor == null || !botActor.online || botActor.IsBot())
		{
			return new MarketResponse
			{
				ok = false,
				error = "角色不在线"
			};
		}
		MarketResponse marketResponse = new MarketResponse();
		switch (request.operation)
		{
		case MarketOperation.Visit:
			if (service.Visit(botActor, request.ownerId) == null)
			{
				marketResponse.ok = false;
				marketResponse.error = "无法进入该商店";
			}
			break;
		case MarketOperation.Buy:
			marketResponse.purchase = service.Buy(botActor, request.ownerId, request.itemIndex, request.quantity);
			marketResponse.ok = marketResponse.purchase == MarketPurchaseResult.Success;
			if (!marketResponse.ok)
			{
				ref string error = ref marketResponse.error;
				string text;
				if (marketResponse.purchase == MarketPurchaseResult.InsufficientMeso)
				{
					text = "金币不足";
				}
				else if (marketResponse.purchase == MarketPurchaseResult.InventoryFull)
				{
					text = "背包空间不足";
				}
				else
				{
					text = ((marketResponse.purchase == MarketPurchaseResult.OwnerMesoFull) ? "店主无法持有更多金币" : "商品或商店状态已变更");
				}
				error = text;
			}
			break;
		case MarketOperation.Chat:
			marketResponse.ok = service.Chat(botActor, request.text);
			if (!marketResponse.ok)
			{
				marketResponse.error = "无法发送店铺聊天";
			}
			break;
		case MarketOperation.Leave:
			service.Leave(botActor.id);
			break;
		}
		foreach (MarketShop item in service.OnMap(botActor.world, botActor.channel, botActor.mapId))
		{
			marketResponse.shops.Add(MarketShopSnapshot.Of(item));
		}
		marketResponse.visiting = MarketShopSnapshot.Of(service.Visiting(botActor));
		if (marketResponse.visiting != null)
		{
			foreach (KeyValuePair<int, int> item2 in service.Visitors(marketResponse.visiting.ownerId))
			{
				BotActor botActor2 = players(item2.Key);
				if (botActor2 != null && service.Visiting(botActor2)?.ownerId == marketResponse.visiting.ownerId)
				{
					marketResponse.visiting.visitors.Add(new MarketVisitorSnapshot
					{
						id = botActor2.id,
						slot = item2.Value,
						name = botActor2.name,
						character = botActor2.character
					});
				}
			}
		}
		return marketResponse;
	}
}
