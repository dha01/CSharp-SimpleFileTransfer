using System;
using System.Linq;

namespace SimpleFileTransfer
{
	class Program
	{
		private static Server _server;
		
		/// <summary>
		/// Сервер.
		/// </summary>
		private static Server Server
		{
			get
			{
				if (_server == null)
				{
					_server = new Server();
				}
				if (_server.Running)
				{
					_server.Stop();
				}
				return _server;
			}
		}
		
		static void Main(string[] args)
		{
			while (true)
			{
				try
				{
					string[] command = Console.ReadLine().Split(' ');

					Console.WriteLine("");

					switch (command.First())
					{
						case "ss":
						case "StartServer":
							Server.Start(command[1], int.Parse(command[2]));
							Console.WriteLine("Сервер запущен. Локальный адрес {0}:{1}", command[1], int.Parse(command[2]));
							break;
						case "sf":
						case "SendFile":
							Server.SendFile(command[1], int.Parse(command[2]), command[2]);
							Console.WriteLine("Файл {0} отправлен по адресу {1}.", command[2], command[1]);
							break;

						case "sfaep":
						case "SendFileAndExecProc":
							Server.SendFile(command[1], int.Parse(command[2]), command[3], true);
							Console.WriteLine("Файл {0} отправлен по адресу {1}:{2}. Ожидается файл с результатом.", command[3], command[1], command[2]);
							break;
						case "Test":
							new Test().StartTest(int.Parse(command[1]));
							break;
						case "sst":
							Server.Start("192.168.1.64", 1234);
							Console.WriteLine("Сервер запущен. Локальный адрес {0}", "192.168.1.64:1234");
							break;
						case "ssd":
							Server.Start("192.168.1.64", 1235);
							Console.WriteLine("Сервер запущен. Локальный адрес {0}", "192.168.1.64:1235");
							break;
						default:
							Console.WriteLine("Команда не найдена.");
							break;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}
	}
}
