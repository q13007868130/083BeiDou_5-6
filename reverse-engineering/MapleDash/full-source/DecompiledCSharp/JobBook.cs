public static class JobBook
{
	public const int BookCount = 10;

	public static int Of(int jobId)
	{
		if (jobId >= 2210 && jobId <= 2218)
		{
			return jobId - 2209;
		}
		return 0;
	}
}
