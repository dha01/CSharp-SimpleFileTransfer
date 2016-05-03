Установка mono
Сатья: https://habrahabr.ru/post/193156/
Команда установки: wget https://bitbucket.org/mindbar/install-mono/raw/master/install-nginx-mono.sh && sudo chmod +x install-nginx-mono.sh && ./install-nginx-mono.sh

Запуск программы: mono <путь>\CSharp-SimpleFileTransfer\bin\Debug\SimpleFileTransfer.exe

Рядом с "SimpleFileTransfer.exe" находится подрограмма для копирования файла "CopyPaster.exe".
При получении команды "SendFileAndExecProc" файл "CopyPaster.exe" должен находиться рядом с "SimpleFileTransfer.exe" для дого чтобы выполнить операцию копирования.  


По умолчанию используется порт 1234.

Команды, которые можно выполнять после запуска программы:

"StartServer" или "ss" запускает сервер по указанному локальному ip
Пример:
StartServer 192.168.1.64

"SendFile" или "sf" отправляет файл по указанному ip (для отправки файла сервер запускать не обязательно)
Пример:
SendFile 192.168.1.64 1.txt

"SendFileAndExecProc" или "sfaep" отправляет файл по указанному ip, принимающая сторона вызывает подпрограмму для копировани и результат отправляет обратно
Необходимо, чтобы был запущен сервер со стороны отправителя для получения файла с результатами выполнения.
Пример:
SendFileAndExecProc 192.168.1.64 1.txt

Запуск на сервере:
cd ..
cd ..
cd ..
rm -r -f CSharp-SimpleFileTransfer
git clone https://github.com/dha01/CSharp-SimpleFileTransfer.git
cd CSharp-SimpleFileTransfer/bin/Debug
srun -t1 -n2 mono SimpleFileTransfer.exe
go 1 CopyPaster.exe 12516 12517
