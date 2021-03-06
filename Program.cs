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

		static bool ExecCommand(string[] command)
		{
			switch (command.First())
			{
				case "pid":
					if (Environment.GetEnvironmentVariables()["SLURM_PROCID"].Equals(command[1]))
					{
						return ExecCommand(command.Skip(2).ToArray());
					}
					return false;
					break;
				case "go":
					var list = Environment.GetEnvironmentVariables();

					var n = (string)list["SLURM_PROCID"];
					var ip_addr = (string)list["SLURM_LAUNCH_NODE_IPADDR"];

					var srv_port = int.Parse(command[3]);
					var test_port = int.Parse(command[4]);

					// Сервер
					if (int.Parse(n) == 0)
					{
						Console.WriteLine("PROCID = 0");
						Server.Start("0.0.0.0", srv_port, test_port);
						Console.WriteLine("PROCID = 0: запущен сервер: {0} {1} {2}", "0.0.0.0", srv_port, test_port);
					}

					// Тестер
					if (int.Parse(n) == 1)
					{
						Console.WriteLine("PROCID = 1");
						Server.Start("0.0.0.0", test_port, test_port);
						Console.WriteLine("PROCID = 1: запущен сервер тестирования: {0} {1} {2}", "0.0.0.0", test_port, test_port);

						int co = 0;

						if (!int.TryParse(command[1], out co))
						{
							throw new Exception("Неправильно введено количество запросов.");
						}

						new Test().StartTest(co, IPAddress.Parse(ip_addr), srv_port, command[2]);
					}

					break;
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
					int? local_port = null;
					string ipa = null;


					if (command.Length == 4)
					{
						remote_port = int.Parse(command[3]);
					}

					local_port = command.Length >= 3 ? int.Parse(command[2]) : 1234;

					if (command.Length >= 2)
					{
						ipa = command[1];
					}
					else
					{
						string hn = Dns.GetHostName();
						IPHostEntry ipe = Dns.GetHostEntry(hn);
						IPAddress[] addrss = ipe.AddressList;
						ipa = addrss.First().ToString();
					}

					Server.Start(ipa, local_port.Value, remote_port);
					Console.WriteLine("Сервер запущен. Локальный адрес {0}:{1}", ipa, local_port);
					break;
				case "sf":
				case "SendFile":
					Server.SendFile(command[1], int.Parse(command[2]), command[3]);
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
				case "env":
					var env_list = Environment.GetEnvironmentVariables();
					int i = 0;
					foreach (var key in env_list.Keys)
					{
						i++;
						Console.WriteLine("{0}) {1} = {2}", i, key, env_list[key]);
						Console.WriteLine("");
					}
					break;
				case "ip":
					string host_name = Dns.GetHostName();
					IPHostEntry ip_entry = Dns.GetHostEntry(host_name);
					IPAddress[] addrs = ip_entry.AddressList;

					Console.WriteLine("HostName: {0}", host_name);
					foreach (var addr in addrs)
					{
						Console.WriteLine("IP-address: {0}", addr);
					}
					break;
				case "st":
					Server.SendText(command[1], int.Parse(command[2]), command[3]);
					Console.WriteLine("Отправлено сообщение: {0}", command[3]);
					break;
				default:
					Console.WriteLine("Команда не найдена.");
					break;
			}

			return true;
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
					while (!ExecCommand(Console.ReadLine().Split(' ')))
					{
					}
					Console.WriteLine("");
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}
		}
	}
}
