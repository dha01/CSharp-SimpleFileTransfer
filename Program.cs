using System;
using System.Linq;
using System.Net;

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
						case "d":
							Server.IsDeleteFiles = int.Parse(command[1]) != 0;
							if (Server.IsDeleteFiles)
							{
								Console.WriteLine("Удаление принятых файлов включено.");
							}
							else
							{
								Console.WriteLine("Удаление принятых файлов отключено.");
							}
							break;
						case "ss":
						case "StartServer":
							int? remote_port = null;

							if (command.Length == 4)
							{
								remote_port = int.Parse(command[3]);
							}

							Server.Start(command[1], int.Parse(command[2]), remote_port);
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

							int count;
							IPAddress ip;
							int port;

							if (!int.TryParse(command[1], out count))
							{
								throw new Exception("Неправильно введено количество запросов.");
							}

							if (!IPAddress.TryParse(command[2], out ip))
							{
								throw new Exception("Неправильно введен IP.");
							}

							if (!int.TryParse(command[3], out port))
							{
								throw new Exception("Неправильно введен порт.");
							}

							new Test().StartTest(count, ip, port, command[4]);
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
