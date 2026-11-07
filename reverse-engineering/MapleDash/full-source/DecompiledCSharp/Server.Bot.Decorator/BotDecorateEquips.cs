using System;

namespace Server.Bot.Decorator;

public sealed class BotDecorateEquips
{
	private readonly EquipSelection selection;

	private readonly BotCustomization customization;

	private readonly Random random;

	private readonly JobPathLogic paths;

	public BotDecorateEquips(EquipSelection selection, BotCustomization customization, Random random = null)
	{
		this.selection = selection;
		this.customization = customization;
		this.random = random ?? new Random();
		paths = new JobPathLogic(this.random);
	}

	private void Equip(BotCharacter bot, string type)
	{
		customization.Equip(bot, selection.ForWearing(type, bot));
	}

	public void Apply(BotCharacter bot)
	{
		int? id = selection.ForWearing("COAT", bot);
		int? id2 = selection.ForWearing("PANTS", bot);
		int? id3 = selection.ForWearing("LONGCOAT", bot);
		if (id.HasValue && id2.HasValue)
		{
			customization.Equip(bot, id);
			customization.Equip(bot, id2);
		}
		else if (id3.HasValue)
		{
			customization.Equip(bot, id3);
		}
		else if (id.HasValue)
		{
			customization.Equip(bot, id);
		}
		else
		{
			customization.Equip(bot, id2);
		}
		Weapon(bot);
		string[] array = new string[4] { "CAP", "GLOVES", "SHOES", "CAPE" };
		foreach (string type in array)
		{
			Equip(bot, type);
		}
		EquipDecoratorRules equipDecoratorRules = new EquipDecoratorRules(selection, customization, random);
		equipDecoratorRules.Add("CAPE", 0.5);
		equipDecoratorRules.Add("EARRING", 0.5);
		equipDecoratorRules.Add("ACCESSORY", 0.98);
		equipDecoratorRules.Apply(bot);
	}

	private bool Coin()
	{
		return random.Next(2) == 0;
	}

	public void Weapon(BotCharacter bot)
	{
		int num = EquipSelection.JobStyle(bot);
		bool flag = false;
		string text;
		switch (num)
		{
		case 100:
		{
			if (bot.job % 100 == 0)
			{
				bool flag3 = Coin();
				if (flag3)
				{
					text = (Coin() ? "SWORD_2H" : "AXE_2H");
				}
				else
				{
					text = (Coin() ? "SWORD" : "AXE");
				}
				flag = !flag3;
				break;
			}
			int num2 = paths.DetermineWarriorPath(bot.job);
			if (num2 == 130)
			{
				text = (Coin() ? "SPEAR" : "POLEARM");
				break;
			}
			bool flag4 = Coin();
			bool flag5 = Coin();
			flag = !flag4;
			if (num2 != 110)
			{
				text = (flag5 ? "SWORD" : "AXE");
			}
			else
			{
				text = (flag5 ? "AXE" : "SWORD");
			}
			if (flag4)
			{
				text += "_2H";
			}
			break;
		}
		case 200:
			text = (Coin() ? "WAND" : "STAFF");
			flag = Coin();
			break;
		case 300:
			if (bot.job % 100 != 0)
			{
				text = ((paths.DetermineBowmanPath(bot.job) == 310) ? "BOW" : "CROSSBOW");
			}
			else
			{
				text = (Coin() ? "BOW" : "CROSSBOW");
			}
			break;
		case 320:
			text = "CROSSBOW";
			break;
		case 400:
		{
			bool flag2 = ((bot.job % 100 == 0) ? Coin() : (paths.DetermineThiefPath(bot.job) == 410));
			text = (flag2 ? "CLAW" : "DAGGER");
			flag = !flag2;
			break;
		}
		case 500:
		case 510:
			if (bot.job != 500)
			{
				text = ((bot.job / 10 == 52) ? "PISTOL" : "KNUCKLER");
			}
			else
			{
				text = (Coin() ? "KNUCKLER" : "PISTOL");
			}
			break;
		case 520:
			text = "PISTOL";
			break;
		default:
			text = "SWORD";
			break;
		}
		Equip(bot, text);
		if (flag)
		{
			Equip(bot, "SHIELD");
		}
	}
}
