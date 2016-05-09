using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFileTransfer
{
	public class Server
	{
		private const string BROADCAST_ADDRESS = "224.0.0.0";

		#region Fields

		/// <summary>
		/// Является текущая ОС Windows.
		/// </summary>
		private static bool? is_os_windows;

		static public int ReceviceFileCount = 0;

		static public long? FileSize = null;

		/// <summary>
		/// Порт.
		/// </summary>
		private int _localPort = 1234;

		private int _remotePort = 1234;

		private string _localAddress;

		/// <summary>
		/// Максимальный размер очереди.
		/// </summary>
		private const int MAX_QUEUE = 99999;

		/// <summary>
		/// Максимальнй размер одного сообщения.
		/// </summary>
		public const int MAX_PART_SIZE = 65536;

		/// <summary>
		/// Запущено ли?
		/// </summary>
		public bool Running = false;

		/// <summary>
		/// Лиммт времени на приём данных.
		/// </summary>
		private const int TIMEOUT = 5800;

		/// <summary>
		/// Cокет
		/// </summary>
		private Socket _serverSocket;

		private static int _fileCount = 0;

		public static bool IsDeleteFiles = true;

		#endregion

		#region Methods

		/// <summary>
		/// Статический конструктор. Определяет является ли используемая ОС Windows.
		/// </summary>
		static Server()
		{
			is_os_windows =
				Environment.OSVersion.Platform == PlatformID.Win32NT ||
				Environment.OSVersion.Platform == PlatformID.Win32S ||
				Environment.OSVersion.Platform == PlatformID.Win32Windows ||
				Environment.OSVersion.Platform == PlatformID.WinCE;
		}

		public static ManualResetEvent allDone = new ManualResetEvent(false);

		private void Broadcast(string ip_address)
		{
			UdpClient receiver = new UdpClient(6666); // UdpClient для получения данных
			receiver.JoinMulticastGroup(IPAddress.Parse(BROADCAST_ADDRESS), 50);

			IPEndPoint remoteIp = null;
			new Task(() =>
			{
				while (true)
				{
					try
					{
						while (true)
						{
							byte[] data = receiver.Receive(ref remoteIp); // получаем данные
							if (remoteIp.Address.ToString().Equals(ip_address))
								continue;
							string message = Encoding.Unicode.GetString(data);
							Console.WriteLine("ReceiveMessage: {0}",message);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
					finally
					{
						receiver.Close();
					}
				}
			}).Start();
		}

		private void SendMessage()
		{
			new Task(() =>
			{

				UdpClient sender = new UdpClient(); // создаем UdpClient для отправки
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(BROADCAST_ADDRESS), 6666);
				try
				{
					while (true)
					{
						string message = String.Format("{0} {1} {2}", _localAddress, _localPort, _remotePort);
						byte[] data = Encoding.Unicode.GetBytes(message);
						sender.Send(data, data.Length, endPoint); // отправка
						Thread.Sleep(5000);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					sender.Close();
				}
			}).Start();
		}

		/// <summary>
		/// Запуск сервера.
		/// </summary>
		/// <param name="ip_address">IP-адресс.</param>
		/// <returns></returns>
		public bool Start(string ip_address, int port, int? remote_port = null)
		{
			_localPort = port;
			_remotePort = remote_port ?? port;

			_localAddress = ip_address;
			
			if (Running) return false; // Если уже запущено, то выходим

			try
			{
				// tcp/ip сокет (ipv4)
				_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ip_address), _localPort));
				_serverSocket.Listen(MAX_QUEUE);
				_serverSocket.ReceiveTimeout = TIMEOUT;
				_serverSocket.SendTimeout = TIMEOUT;
				Running = true;
			}
			catch
			{
				return false;
			}
			Broadcast(ip_address);
			SendMessage();

			// Наш поток ждет новые подключения и создает новые потоки.
			new Task(() =>
			{
				while (Running)
				{
					try
					{
						allDone.Reset();

						Console.WriteLine("Waiting for a connection...");
						_serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), _serverSocket);

						allDone.WaitOne();
					}
					catch(Exception e)
					{
						Console.WriteLine(e.Message);
					}
				}
			}).Start();

			return true;
		}

		public void AcceptCallback(IAsyncResult ar)
		{
			try
			{
				// Signal the main thread to continue.
				allDone.Set();

				// Get the socket that handles the client request.
				Socket listener = (Socket)ar.AsyncState;
				Socket handler = listener.EndAccept(ar);
				Receive(handler);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		/// <summary>
		/// Остановка сервера.
		/// </summary>
		public void Stop()
		{
			if (Running)
			{
				Running = false;
				try { _serverSocket.Close(); }
				catch { }
				_serverSocket = null;
			}
		}

		/// <summary>
		/// Обработка полученного сообщения.
		/// </summary>
		/// <param name="socket"></param>
		private void Receive(Socket socket)
		{
			ReceiveFile(socket);
		}

		#endregion

		public enum MesasageType
		{
			/// <summary>
			/// Принять файл.
			/// </summary>
			ReceiveFile = 1,

			/// <summary>
			/// Принять файл и вызвать подпрограмму.
			/// </summary>
			ReveiveFileAndExecProc = 2,

			Text = 3
		}

		/// <summary>
		/// Отправляет файл по указанному адресу.
		/// </summary>
		/// <param name="ip">Удаленный IP-адрес.</param>
		/// <param name="port"></param>
		/// <param name="file_name">Удаленный порт.</param>
		/// <param name="exec_proc">Будет ли вызвано выполнение подпрограммы на принимающей стороне.</param>
		/// <param name="is_delete_file"></param>
		static public void SendFile(IPAddress ip, int port, string file_name, bool exec_proc = false, bool is_delete_file = false)
		{
			FileInfo file = new FileInfo(file_name);

			if (FileSize.HasValue)
			{
				FileSize = file.Length;
			}

			// Устанавливаем соединение через сокет.
			Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.Connect(new IPEndPoint(ip, port));
			var ns = new NetworkStream(socket);
			var fs = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.Read);

			ns.WriteByte((byte)(exec_proc ? MesasageType.ReveiveFileAndExecProc : MesasageType.ReceiveFile));
			fs.CopyToAsync(ns).ContinueWith((ar) =>
			{
				fs.Close();
				fs.Dispose();
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
				ns.Close();
				ns.Dispose();

				if (is_delete_file)
				{
					File.Delete(file_name);
				}
			});
		}

		/// <summary>
		/// Отправляет файл по указанному адресу.
		/// </summary>
		/// <param name="ip">Удаленный IP-адрес.</param>
		/// <param name="port">Порт.</param>
		/// <param name="file_name">Удаленный порт.</param>
		/// <param name="exec_proc">Будет ли вызвано выполнение подпрограммы на принимающей стороне.</param>
		/// <param name="is_delete_file"></param>
		static public void SendFile(string ip, int port, string file_name, bool exec_proc = false, bool is_delete_file = false)
		{
			SendFile(IPAddress.Parse(ip), port, file_name, exec_proc, is_delete_file);
		}

		/// <summary>
		/// Отправляет файл по указанному адресу.
		/// </summary>
		/// <param name="ip_address">Удаленный IP-адрес.</param>
		/// <param name="port">Порт.</param>
		static public void SendText(string ip_address, int port, string text)
		{
			var ip = IPAddress.Parse(ip_address);
			using (var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
			{
				socket.Connect(new IPEndPoint(ip, port));
				socket.Send(new[] { (byte)MesasageType.Text });
				socket.Send(Encoding.Unicode.GetBytes(text));
			}
		}

		/// <summary>
		/// Приём файла.
		/// </summary>
		/// <param name="socket">Сокет.</param>
		private void ReceiveFile(Socket socket)
		{
			// Генерируем уникальное имя для файла.
			var tmp_file_name = string.Format("{0}_{1}", DateTime.Now.Ticks, _fileCount);
			_fileCount++;

			var ns = new NetworkStream(socket);
			
			var remote_address = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
			var message_type = (MesasageType)ns.ReadByte();

			if (message_type == MesasageType.Text)
			{
				byte[] buffer = new byte[256];
				socket.Receive(buffer);
				Console.WriteLine("Принято сообщение: {0}", Encoding.Unicode.GetString(buffer));
				return;
			}

			var fs = new FileStream(tmp_file_name, FileMode.CreateNew);

			ns.CopyToAsync(fs).ContinueWith((ar) =>
			{
				fs.Close();
				fs.Dispose();
				ns.Close();
				ns.Dispose();
				FileInfo file = new FileInfo(tmp_file_name);
				if (FileSize.HasValue)
				{
					if (FileSize == file.Length)
					{
						ReceviceFileCount++;
					}
					else
					{
						Console.WriteLine("Размер принятого файла не соответствует отправленному ({0} != {1}).", FileSize, file.Length);
					}
				}

				Console.WriteLine("Получен файл");

				if (message_type == MesasageType.ReveiveFileAndExecProc)
				{
					ExecProcAndSendFile(tmp_file_name, remote_address, _remotePort);
				}
				else
				{
					if (IsDeleteFiles)
					{
						File.Delete(tmp_file_name);
					}
				}
			});
		}

		/// <summary>
		/// Вызов подпрограммы и отправка результата по адресу.
		/// </summary>
		/// <param name="file_name">Имя файла с фходными данными для подпрограммы.</param>
		/// <param name="ip_address">IP-адрес для возврата результата выполнения.</param>
		/// <param name="port">Порт.</param>
		private void ExecProcAndSendFile(string file_name, string ip_address, int port)
		{
			// Создаем новую нить для выполнения подпрограммы.
			new Task(() =>
			{
				try
				{
					var copy_file_name = string.Format("{0}_copy", file_name);
					// Устанавливаем параметры запуска подпрограммы.
					var proc = new Process
					{
						StartInfo =
						{
							FileName = "CopyPaster.exe",
							Arguments = string.Format("{0} {1}", file_name, copy_file_name),
							CreateNoWindow = is_os_windows.Value,
							UseShellExecute = !is_os_windows.Value
						}
					};

					// Запускаем и ожидаем окончания выполнения.
					proc.Start();
					proc.WaitForExit();
					/*
					string[] input = new string[2];
					input[0] = file_name;
					input[1] = copy_file_name;
					ExecBin("CopyPaster.exe", input);*/

					// Возвращаем результат выполнения по указанному IP-адресу.
					SendFile(ip_address, port, copy_file_name, false, IsDeleteFiles);

					if (IsDeleteFiles)
					{
						File.Delete(file_name);
					}
					Console.WriteLine("Выполнена подпрограмма и результат отправлен по адресу {0}", ip_address);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			}).Start();
		}

		private static Dictionary<string, byte[]> ProgrammCash = new Dictionary<string, byte[]>(); 

		private static void ExecBin(string name, string[] arguments)
		{
			if (!ProgrammCash.Keys.Contains(name))
			{
				FileStream fs = new FileStream(name, FileMode.Open);
				BinaryReader br = new BinaryReader(fs);
				ProgrammCash.Add(name, br.ReadBytes(Convert.ToInt32(fs.Length)));
				fs.Close();
				br.Close();
			}
		
			Assembly a = Assembly.Load(ProgrammCash[name]);
			MethodInfo method = a.EntryPoint;

			if (method != null)
			{
				
				method.Invoke(null, new object[] { arguments });
			}
		}
	}
}
