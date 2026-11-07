using System;
using System.Globalization;
using MapleDash.Js;

public sealed class NativeScriptValues
{
	private readonly DbStore db;

	private readonly Func<DateTimeOffset> clock;

	public NativeScriptValues(DbStore db, Func<DateTimeOffset> clock = null)
	{
		this.db = db;
		this.clock = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.Now));
	}

	public ScriptValueResponse Handle(ScriptValueRequest request)
	{
		CharRecord charRecord = CharactersDao.FindById(db, request.characterId);
		if (charRecord == null)
		{
			throw new JsRuntimeException("Script character does not exist");
		}
		if (request.period < 0 || request.period > 2)
		{
			throw new JsRuntimeException("Invalid script value period");
		}
		if (request.name == null)
		{
			throw new JsRuntimeException("Script value key cannot be null");
		}
		long num = (request.account ? charRecord.accountid : charRecord.id);
		int type = (request.account ? 11 : 21) + request.period;
		DbTable dbTable = db.Table("extend_value");
		DateTimeOffset dateTimeOffset = clock();
		long boundary = 0L;
		if (request.period > 0)
		{
			DateTime dateTime = dateTimeOffset.Date;
			if (request.period == 2)
			{
				dateTime = dateTime.AddDays((double)(1 - dateTime.DayOfWeek));
			}
			boundary = new DateTimeOffset(dateTime, dateTimeOffset.Offset).ToUnixTimeMilliseconds();
			dbTable.Delete((DbRow r) => DbStore.Int(r, "extend_type") == type && DbStore.Long(r, "create_time", 0L) < boundary);
		}
		string id = num.ToString(CultureInfo.InvariantCulture);
		DbRow dbRow = dbTable.Find((DbRow r) => DbStore.Str(r, "extend_id") == id && DbStore.Int(r, "extend_type") == type && DbStore.Str(r, "extend_name") == request.name);
		if (request.write)
		{
			long num2 = dateTimeOffset.ToUnixTimeMilliseconds();
			if (dbRow == null)
			{
				dbTable.Insert(new DbRow
				{
					["extend_id"] = id,
					["extend_type"] = (long)type,
					["extend_name"] = request.name,
					["extend_value"] = request.value,
					["create_time"] = num2,
					["update_time"] = 0L
				});
				dbRow = dbTable.Find((DbRow r) => DbStore.Str(r, "extend_id") == id && DbStore.Int(r, "extend_type") == type && DbStore.Str(r, "extend_name") == request.name);
			}
			else
			{
				dbRow["extend_value"] = request.value;
				dbRow["update_time"] = num2;
			}
		}
		if (request.write || request.period > 0)
		{
			db.Save();
		}
		return new ScriptValueResponse
		{
			value = ((dbRow == null) ? null : DbStore.Str(dbRow, "extend_value"))
		};
	}
}
