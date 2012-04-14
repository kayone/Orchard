TASKKILL /T /F /IM WaHostBootstrapper.exe
TASKKILL /T /F /IM DFService.exe
TASKKILL /T /F /IM csMonitor.exe
TASKKILL /T /F /IM w3wp.exe
TASKKILL /T /F /IM dfMonitor.exe
TASKKILL /T /F /IM WaIISHost.exe

SET TARGET=_rawPackage
SET ORCHARD=_orchard

rd %TARGET% /S /Q
rd %ORCHARD% /S /Q

xcopy src\Orchard.Azure\Orchard.Azure.Web\*.*  %TARGET%\ /E /V /I /Y /F /EXCLUDE:exclude.txt

xcopy src\Orchard.Web\Modules\*.*  %TARGET%\Modules\ /E /V /I /Y /F  /EXCLUDE:modules_exclude.txt
xcopy src\Orchard.Web\Core\*.*  %TARGET%\Core\ /E /V /I /Y /F /EXCLUDE:modules_exclude.txt
xcopy src\Orchard.Web\Themes\*.*  %TARGET%\Themes\ /E /V /I /Y /F /EXCLUDE:modules_exclude.txt

del %TARGET%\*.cs /S /F /Q
del %TARGET%\*.csproj /S /F /Q
del %TARGET%\bin\*.xml /S /F /Q
rd  %TARGET%\obj /S /Q

xcopy %TARGET%\*.* %ORCHARD%\*.* /s
rd %TARGET% /S /Q


pause