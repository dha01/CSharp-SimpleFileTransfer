using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFileTransfer
{
	public class Server
	{
		#region Fields

		static public int ReceviceFileCount = 0;

		static public long? FileSize = null;

		/// <summary>
		/// Порт.
		/// </summary>
		private int PORT = 1234;

		private int _remotePort = 1234;

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
		private const int TIMEOUT = 800;

		/// <summary>
		/// Cокет
		/// </summary>
		private Socket _serverSocket;

		private static int _fileCount = 0;

		public static bool IsDeleteFiles = true;

		#endregion

		#region Methods

		/// <summary>
		/// Запуск сервера.
		/// </summary>
		/// <param name="ip_address">IP-адресс.</param>
		/// <returns></returns>
		public bool Start(string ip_address, int port, int? remote_port = null)
		{
			PORT = port;
			if (!remote_port.HasValue)
			{
				_remotePort = port;
			}
			else
			{
				_remotePort = remote_port.Value;
			}
			
			if (Running) return false; // Если уже запущено, то выходим

			try
			{
				// tcp/ip сокет (ipv4)
				_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ip_address), PORT));
				_serverSocket.Listen(MAX_QUEUE);
				_serverSocket.ReceiveTimeout = TIMEOUT;
				_serverSocket.SendTimeout = TIMEOUT;
				Running = true;
			}
			catch
			{
				return false;
			}

			// Наш поток ждет новые подключения и создает новые потоки.
			Thread request_listener = new Thread(() =>
			{
				while (Running)
				{
					Socket clientSocket;
					try
					{
						clientSocket = _serverSocket.Accept();
						// Создаем новый поток для нового клиента и продолжаем слушать сокет.
						Task request_handler = new Task(() =>
						{
							clientSocket.ReceiveTimeout = TIMEOUT;
							clientSocket.SendTimeout = TIMEOUT;
							try
							{
								Receive(clientSocket);
							}
							catch
							{
								try
								{
									clientSocket.Close();
								}
								catch { }
							}
						});
						request_handler.Start();
					}
					catch { }
				}
			});
			request_listener.Start();

			return true;
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
			ReveiveFileAndExecProc = 2
		}

		/// <summary>
		/// Отправляет файл по указанному адресу.
		/// </summary>
		/// <param name="ip">Удаленный IP-адрес.</param>
		/// <param name="port"></param>
		/// <param name="file_name">Удаленный порт.</param>
		/// <param name="exec_proc">Будет ли вызвано выполнение подпрограммы на принимающей стороне.</param>
		static public void SendFile(IPAddress ip, int port, string file_name, bool exec_proc = false)
		{
			FileInfo file = new FileInfo(file_name);

			if (FileSize.HasValue)
			{
				FileSize = file.Length;
			}

			// Открываем файл для чтения.
			using (var fs = new FileStream(file_name, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				// Устанавливаем соединение через сокет.
				Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				socket.Connect(new IPEndPoint(ip, port));

				// Отправляем тип сообщения.
				socket.Send(new[] { (byte)(exec_proc ? MesasageType.ReveiveFileAndExecProc : MesasageType.ReceiveFile) }, 1, SocketFlags.None);

				// Отправляем файл блоками размером MAX_PART_SIZE байт, до тех пор пока не будет считан весь файл.
				byte[] part = new byte[MAX_PART_SIZE];
				while (true)
				{
					var size = fs.Read(part, 0, MAX_PART_SIZE);
					if (size == 0) break;
					socket.Send(part, size, SocketFlags.None);
				}

				// Закрываем соединения.
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
		}

		/// <summary>
		/// Отправляет файл по указанному адресу.
		/// </summary>
		/// <param name="ip">Удаленный IP-адрес.</param>
		/// <param name="port"></param>
		/// <param name="file_name">Удаленный порт.</param>
		/// <param name="exec_proc">Будет ли вызвано выполнение подпрограммы на принимающей стороне.</param>
		static public void SendFile(string ip, int port, string file_name, bool exec_proc = false)
		{
			SendFile(IPAddress.Parse(ip), port, file_name, exec_proc);
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

			byte[] buffer = new byte[MAX_PART_SIZE];
			// Получаем тип сообщения.
			socket.Receive(buffer, 1, SocketFlags.None);
			var message_type = (MesasageType) buffer[0];

			var remote_address = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();

			// Получаем файл.
			using (var fs = new FileStream(tmp_file_name, FileMode.CreateNew))
			{
				while (true)
				{
					var received_count = socket.Receive(buffer, MAX_PART_SIZE, SocketFlags.None);
					if (received_count == 0) break;
					fs.Write(buffer, 0, received_count);
				}
				
				Console.WriteLine("Получен файл от {0}.", remote_address);
			}

			FileInfo file = new FileInfo(tmp_file_name);

			//Test.Log(string.Format("Принят файл"));

			if(FileSize.HasValue)
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

			// Если требуется, то вызываем подпрорамму.
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
		}

		/// <summary>
		/// Вызов подпрограммы и отправка результата по адресу.
		/// </summary>
		/// <param name="file_name">Имя файла с фходными данными для подпрограммы.</param>
		/// <param name="ip_address">IP-адрес для возврата результата выполнения.</param>
		/// <param name="port"></param>
		private void ExecProcAndSendFile(string file_name, string ip_address, int port)
		{
			// Создаем новую нить для выполнения подпрограммы.
			var proccess = new Thread(() =>
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
							CreateNoWindow = true
						}
					};

					// Запускаем и ожидаем окончания выполнения.
					proc.Start();
					proc.WaitForExit();
					// Возвращаем результат выполнения по указанному IP-адресу.
					SendFile(ip_address, port, copy_file_name);

					if (IsDeleteFiles)
					{
						File.Delete(file_name);
						File.Delete(copy_file_name);
					}
					Console.WriteLine("Выполнена подпрограмма и результат отправлен по адресу {0}", ip_address);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			});

			// Запускаем нить.
			proccess.Start();
		}
	}
}
