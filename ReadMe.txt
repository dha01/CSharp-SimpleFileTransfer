��������� mono
�����: https://habrahabr.ru/post/193156/
������� ���������: wget https://bitbucket.org/mindbar/install-mono/raw/master/install-nginx-mono.sh && sudo chmod +x install-nginx-mono.sh && ./install-nginx-mono.sh

������ ���������: mono <����>\CSharp-SimpleFileTransfer\bin\Debug\SimpleFileTransfer.exe

����� � "SimpleFileTransfer.exe" ��������� ����������� ��� ����������� ����� "CopyPaster.exe".
��� ��������� ������� "SendFileAndExecProc" ���� "CopyPaster.exe" ������ ���������� ����� � "SimpleFileTransfer.exe" ��� ���� ����� ��������� �������� �����������.  


�� ��������� ������������ ���� 1234.

�������, ������� ����� ��������� ����� ������� ���������:

"StartServer" ��� "ss" ��������� ������ �� ���������� ���������� ip
������:
StartServer 192.168.1.64

"SendFile" ��� "sf" ���������� ���� �� ���������� ip (��� �������� ����� ������ ��������� �� �����������)
������:
SendFile 192.168.1.64 1.txt

"SendFileAndExecProc" ��� "sfaep" ���������� ���� �� ���������� ip, ����������� ������� �������� ������������ ��� ���������� � ��������� ���������� �������
����������, ����� ��� ������� ������ �� ������� ����������� ��� ��������� ����� � ������������ ����������.
������:
SendFileAndExecProc 192.168.1.64 1.txt
