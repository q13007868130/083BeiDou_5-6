using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public static class AdminMainThread
{
	private static readonly object gate = new object();

	private static readonly Queue<Action> queue = new Queue<Action>();

	public static volatile bool Pumping;

	public static volatile bool Foreground = true;

	public static int MainThreadId;

	public static Action<Exception> OnError;

	private static int inflight;

	public static bool OnMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

	public static void Post(Action action)
	{
		if (action == null)
		{
			return;
		}
		lock (gate)
		{
			queue.Enqueue(action);
		}
	}

	public static int Pump()
	{
		int num = 0;
		while (true)
		{
			Action action;
			lock (gate)
			{
				if (queue.Count == 0)
				{
					return num;
				}
				action = queue.Dequeue();
			}
			Interlocked.Increment(ref inflight);
			try
			{
				action();
			}
			catch (Exception obj)
			{
				OnError?.Invoke(obj);
			}
			finally
			{
				Interlocked.Decrement(ref inflight);
			}
			num++;
		}
	}

	public static void AwaitIdle(int timeoutMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (Volatile.Read(in inflight) > 0 && stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			Thread.Sleep(5);
		}
	}

	public static T Run<T>(Func<T> work, int timeoutMs = 8000)
	{
		if (!Pumping || OnMainThread)
		{
			return work();
		}
		T result = default;
		Exception error = null;
		ManualResetEventSlim done = new ManualResetEventSlim(initialState: false);
		try
		{
			Post(() =>
			{
				try
				{
					result = work();
				}
				catch (Exception ex)
				{
					error = ex;
				}
				finally
				{
					done.Set();
				}
			});
			if (!done.Wait(timeoutMs))
			{
				throw new AdminApiException("游戏主线程繁忙，请求超时（切回游戏后再试）");
			}
		}
		finally
		{
			if (done != null)
			{
				((IDisposable)done).Dispose();
			}
		}
		if (error != null)
		{
			throw error;
		}
		return result;
	}

	public static void Run(Action work, int timeoutMs = 8000)
	{
		Run(() =>
		{
			work();
			return (object)null;
		}, timeoutMs);
	}
}
