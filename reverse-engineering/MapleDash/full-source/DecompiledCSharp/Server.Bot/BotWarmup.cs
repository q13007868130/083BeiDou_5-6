using System;
using System.Collections.Generic;
using System.Threading;

namespace Server.Bot;

public static class BotWarmup
{
	private sealed class Job
	{
		public Thread thread;

		public Dictionary<string, object> root;

		public Exception error;

		public volatile bool done;
	}

	private static readonly Dictionary<string, Job> jobs = new Dictionary<string, Job>();

	private static readonly object gate = new object();

	public static void Start(string path)
	{
		lock (gate)
		{
			if (jobs.ContainsKey(path))
			{
				return;
			}
		}
		string text = PlatformRes.LoadText(path);
		if (text == null)
		{
			lock (gate)
			{
				jobs[path] = new Job
				{
					done = true
				};
				return;
			}
		}
		lock (gate)
		{
			if (jobs.ContainsKey(path))
			{
				return;
			}
			Job job = new Job();
			jobs[path] = job;
			job.thread = new Thread(() =>
			{
				try
				{
					job.root = MiniJsonDb.Parse(text) as Dictionary<string, object>;
				}
				catch (Exception error)
				{
					job.error = error;
				}
				job.done = true;
			})
			{
				IsBackground = true
			};
			job.thread.Start();
		}
	}

	public static bool Ready(string path)
	{
		Job value;
		lock (gate)
		{
			if (!jobs.TryGetValue(path, out value))
			{
				return false;
			}
		}
		return value.done;
	}

	public static bool TryRoot(string path, out Dictionary<string, object> root)
	{
		root = null;
		Job value;
		lock (gate)
		{
			jobs.TryGetValue(path, out value);
		}
		if (value == null)
		{
			Start(path);
			lock (gate)
			{
				jobs.TryGetValue(path, out value);
			}
		}
		if (value == null || !value.done)
		{
			return false;
		}
		root = value.root;
		return root != null;
	}

	public static void Release(string path)
	{
		lock (gate)
		{
			if (jobs.TryGetValue(path, out var value) && value.done && value.root != null)
			{
				jobs.Remove(path);
			}
		}
	}

	public static Dictionary<string, object> Root(string path)
	{
		Job value;
		lock (gate)
		{
			jobs.TryGetValue(path, out value);
		}
		if (value == null)
		{
			Start(path);
			lock (gate)
			{
				jobs.TryGetValue(path, out value);
			}
		}
		if (value == null)
		{
			return (MiniJsonDb.Parse(PlatformRes.LoadText(path)) as Dictionary<string, object>) ?? throw new InvalidOperationException("Bot resource missing: " + path);
		}
		value.thread?.Join();
		if (value.root == null)
		{
			throw new InvalidOperationException("Bot resource invalid: " + path, value.error);
		}
		return value.root;
	}
}
