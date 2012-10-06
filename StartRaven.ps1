param($serverName)

$serverExe = "C:\dev\brnkly-private\packages\RavenDB.Server.1.0.960\Raven.Server.exe";

@($serverName) | %{
	$serverSettings = "--set=Raven/HostName==$_ " + `
					  "--set=Raven/DataDir==c:\RavenData\$_\Data " + `
		              "--set=Raven/Port==8081 " + `
					  "--set=Raven/VirtualDirectory==/RavenDB " + `
					  "--set=Raven/AnonymousAccess==All";

	Write-Host "`"$serverExe $serverSettings`"";
	Start-Process cmd -ArgumentList /C, "`"$serverExe $serverSettings`"";
}
