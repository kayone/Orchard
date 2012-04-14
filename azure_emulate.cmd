"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devstore:shutdown

CALL package.cmd

SET ROLENAME=Orchard.Azure.Web
SET APPROOT=_rawPackage
SET APPNAME=OrchardRaw


sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE %APPNAME%_Dev"
sqlcmd -S localhost\SQLExpress -Q "CREATE DATABASE %APPNAME%_Dev"

sqlcmd -S localhost\SQLExpress -Q "DROP DATABASE DevelopmentStorageDb20110816"

"C:\Program Files\Windows Azure Emulator\emulator\csrun" /devstore:start

"C:\Program Files\Windows Azure SDK\v1.6\bin\cspack" ServiceDefinition.csdef /role:%ROLENAME%;"%APPROOT%" /sites:%ROLENAME%;Web;"%APPROOT%" /rolePropertiesFile:%ROLENAME%;"Properties.txt" /out:"%ROLENAME%.csx" /copyOnly
"C:\Program Files\Windows Azure Emulator\emulator\csrun" /run:%ROLENAME%.csx;ServiceConfiguration.cscfg

pause