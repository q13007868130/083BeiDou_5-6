public readonly struct DamageResult(long amount, bool critical)
{
	public readonly long Amount = amount;

	public readonly bool Critical = critical;
}
