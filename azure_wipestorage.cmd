SET APPNAME=Exceptrack

pause

"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devfabric:shutdown
"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devstore:shutdown

%systemroot%\system32\inetsrv\appcmd stop apppool %APPNAME%

sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE %APPNAME%_Dev"
sqlcmd -S localhost\SQLExpress -Q "CREATE DATABASE %APPNAME%_Dev"

%systemroot%\system32\inetsrv\appcmd start apppool %APPNAME%

sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE DevelopmentStorageDb20110816"


pause