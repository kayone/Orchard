TASKKILL /T /F /IM WaHostBootstrapper.exe
TASKKILL /T /F /IM DFService.exe
TASKKILL /T /F /IM csMonitor.exe
TASKKILL /T /F /IM w3wp.exe
TASKKILL /T /F /IM dfMonitor.exe
TASKKILL /T /F /IM WaIISHost.exe

SET TARGET=_rawPackage

rd %TARGET% /S /Q

xcopy src\Orchard.Azure\Orchard.Azure.Web\*.*  %TARGET%\ /E /V /I /Y /F /EXCLUDE:exclude.txt

xcopy src\Orchard.Web\Modules\*.*  %TARGET%\Modules\ /E /V /I /Y /F 
xcopy src\Orchard.Web\Core\*.*  %TARGET%\Core\ /E /V /I /Y /F
xcopy src\Orchard.Web\Themes\*.*  %TARGET%\Themes\ /E /V /I /Y /F


del %TARGET%\Modules\*.cs /S /F /Q
del %TARGET%\Modules\*.csproj /S /F /Q


pause