CALL package.cmd

SET ROLENAME=Orchard.Azure.Web
SET APPROOT=_rawPackage

SET APPNAME=Exceptrack


"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devfabric:shutdown
"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devstore:shutdown

%systemroot%\system32\inetsrv\appcmd stop apppool %APPNAME%



sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE %APPNAME%_Dev"
sqlcmd -S localhost\SQLExpress -Q "CREATE DATABASE %APPNAME%_Dev"

sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE DevelopmentStorageDb20110816"

"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devstore:start


"C:\Program Files\Windows Azure SDK\v1.6\bin\cspack" ServiceDefinition.csdef /role:%ROLENAME%;"%APPROOT%" /sites:%ROLENAME%;Web;"%APPROOT%" /rolePropertiesFile:%ROLENAME%;"Properties.txt" /out:"%ROLENAME%.csx" /copyOnly
"C:\Program Files\Windows Azure Emulator\emulator\csrun" /run:%ROLENAME%.csx;ServiceConfiguration.cscfg


pause

pause