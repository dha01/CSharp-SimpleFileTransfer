using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleFileTransfer
{
	public class Server
	{
		#region Fields

		/// <summary>
		/// Порт.
		/// </summary>
		private const int PORT = 1234;

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

		#endregion

		#region Methods

		/// <summary>
		/// Запуск сервера.
		/// </summary>
		/// <param name="ip_address">IP-адресс.</param>
		/// <returns></returns>
		public bool Start(string ip_address)
		{
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
						Thread request_handler = new Thread(() =>
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
		/// <param name="file_name">Удаленный порт.</param>
		/// <param name="exec_proc">Будет ли вызвано выполнение подпрограммы на принимающей стороне.</param>
		static public void SendFile(string ip, string file_name, bool exec_proc = false)
		{
			// Открываем файл для чтения.
			using (var fs = new FileStream(file_name, FileMode.Open))
			{
				// Устанавливаем соединение через сокет.
				var ip_address = IPAddress.Parse(ip);
				Socket socket = new Socket(ip_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				socket.Connect(new IPEndPoint(ip_address, PORT));

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

			// Если требуется, то вызываем подпрорамму.
			if (message_type == MesasageType.ReveiveFileAndExecProc)
			{
				ExecProcAndSendFile(tmp_file_name, remote_address);
			}
		}

		/// <summary>
		/// Вызов подпрограммы и отправка результата по адресу.
		/// </summary>
		/// <param name="file_name">Имя файла с фходными данными для подпрограммы.</param>
		/// <param name="ip_address">IP-адрес для возврата результата выполнения.</param>
		private void ExecProcAndSendFile(string file_name, string ip_address)
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
							Arguments = string.Format("{0} {1}", file_name, copy_file_name)
						}
					};
					// Запускаем и ожидаем окончания выполнения.
					proc.Start();
					proc.WaitForExit();

					// Возвращаем результат выполнения по указанному IP-адресу.
					SendFile(ip_address, copy_file_name);
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
