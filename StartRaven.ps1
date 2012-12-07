param($serverNames = 'localhost', $port = 8081)

$serverExe = ".\packages\RavenDB.Server.2.0.2170-Unstable\tools\Raven.Server.exe";
#$serverExe = "C:\dev\ravendb\Raven.Server\bin\Debug\Raven.Server.exe";

@($serverNames) | %{
	$serverSettings = "--set=Raven/HostName==$_ " + `
					  "--set=Raven/DataDir==c:\RavenData\$_\Data " + `
		              "--set=Raven/Port==$port " + `
					  "--set=Raven/AnonymousAccess==None";

	Write-Host "`"$serverExe $serverSettings`"";
	Start-Process cmd -ArgumentList /C , "`"$serverExe /K $serverSettings`"";
}
