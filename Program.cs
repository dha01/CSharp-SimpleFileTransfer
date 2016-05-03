using System;
using System.Linq;

namespace SimpleFileTransfer
{
	class Program
	{
		/// <summary>
		/// Сервер.
		/// </summary>
		private static Server _server;
		
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
							if (_server == null)
							{
								_server = new Server();
							}
							if (_server.Running)
							{
								_server.Stop();
							}
							_server.Start(command[1]);
							Console.WriteLine("Сервер запущен. Локальный адрес {0}", command[1]);
							break;
						case "sf":
						case "SendFile":
							Server.SendFile(command[1], command[2]);
							Console.WriteLine("Файл {0} отправлен по адресу {1}.", command[2], command[1]);
							break;

						case "sfaep":
						case "SendFileAndExecProc":
							Server.SendFile(command[1], command[2], true);
							Console.WriteLine("Файл {0} отправлен по адресу {1}. Ожидается файл с результатом.", command[2], command[1]);
							break;
						case "env":
							var env_list = Environment.GetEnvironmentVariables();
							foreach (var key in env_list.Keys)
							{
								Console.WriteLine("{0} = {1}", key, env_list[key]);
								Console.WriteLine("");
							}
							
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
