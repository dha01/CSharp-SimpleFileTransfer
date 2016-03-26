using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFileTransfer
{
	public class Test
	{
		private const int TIMEOUT_SEC = 10;
		
		private const string FILE_LOG_NAME = "Log";

		private const int TEST_COUNT = 25;

		private static Object thisLock = new Object();

		public static void Log(string message)
		{
			lock (thisLock)
			{
				StreamWriter writer = new System.IO.StreamWriter(string.Format("{0}.txt", FILE_LOG_NAME), true);
				writer.WriteLine(string.Format("{0}", message));
				writer.Close();
			}
		}

		public void StartTest(int count, IPAddress ip, int port, string file_name)
		{
			TimeSpan total = new TimeSpan(0);
			Server.FileSize = -1;
			Stopwatch stopWatch = new Stopwatch();
			for (int j = 0; j < TEST_COUNT; j++)
			{
				var task_list = new List<Task>();
			
				for (int i = 0; i < count; i++)
				{
					task_list.Add( new Task(() =>
					{
						try
						{
							Server.SendFile(ip, port, file_name, true);
							//Log("Отправлен файл");
						}
						catch (Exception e)
						{
							throw new Exception(e.Message);
						}

					}));
				}

				stopWatch.Restart();
				foreach (var task in task_list)
				{
					task.Start();
				}

				while (Server.ReceviceFileCount < count && stopWatch.Elapsed.TotalSeconds < TIMEOUT_SEC)
				{
					Thread.Sleep(0);
				}

				stopWatch.Stop();

				if (stopWatch.Elapsed.TotalSeconds >= TIMEOUT_SEC)
				{
					j--;
					Console.WriteLine("Привышено время ожидания результата.");
					continue;
				}

				Server.ReceviceFileCount = 0;
				TimeSpan ts = stopWatch.Elapsed;
				total += ts;
				string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
			ts.Hours, ts.Minutes, ts.Seconds,
			ts.Milliseconds);
				Log(string.Format("{0}) RunTime: {1}", j, elapsedTime));
			}


			string total_time = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
			total.Hours, total.Minutes, total.Seconds,
			total.Milliseconds / 10);

			TimeSpan average = new TimeSpan(total.Ticks / TEST_COUNT);

			string average_time = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
			average.Hours, average.Minutes, average.Seconds,
			average.Milliseconds);

			Log(string.Format("Try count: {0} Total Time: {1} {3} Average Time one try: {2} {4}", TEST_COUNT, total_time, average_time, total.TotalMilliseconds, average.TotalMilliseconds));
			Console.WriteLine("Complite");
		}
	}
}
