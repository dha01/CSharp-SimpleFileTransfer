﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

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
			Console.WriteLine("OS : {0}", Environment.OSVersion);
			
			/*	FileStream fs = new FileStream("CopyPaster.exe", FileMode.Open);
			BinaryReader br = new BinaryReader(fs);
			byte[] bin = br.ReadBytes(Convert.ToInt32(fs.Length));
			fs.Close();
			br.Close();
			/*
			Assembly a = Assembly.Load(bin);
			MethodInfo method = a.EntryPoint;

			if (method != null) {
				string[] input = new string[2];
				input[0] = "1.jpg";
				input[1] = "25.jpg";
				method.Invoke(null, new object[] { input });
			}*/
			
			
			while (true)
			{
				try
				{
					Console.Write("Input command: ");
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

						case "help":
							Console.WriteLine(@"
Command list:
n
1 Format	: ss <local_ip> <local_port> [<remote_port>]
  Sample	: ss 192.168.1.64 1234 1235
  Description	: Start server

2 Format	: Test <file_count> <remote_ip> <remote_port> <file_name>
  Sample	: Test 1 192.168.1.64 1235 1.jpg
  Description	: Send and receive 25 files and write statistic in Log.txt

3 Format	: sf <remote_ip> <remote_port> <file_name>
  Sample	: sf 192.168.1.64 1235 1.jpg
  Description	: Send file to remote server

4 Format	: sfaep <remote_ip> <remote_port> <file_name>
  Sample	: sfaep 192.168.1.64 1235 1.jpg
  Description	: Send file to remote server with flag exec proc and return value
");
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
