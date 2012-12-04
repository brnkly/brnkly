param($serverName)

$serverExe = ".\packages\RavenDB.Server.2.0.2161\Raven.Server.exe";

@($serverName) | %{
	$serverSettings = "--set=Raven/HostName==$_ " + `
					  "--set=Raven/DataDir==c:\RavenData\$_\Data " + `
		              "--set=Raven/Port==8081 " + `
					  "--set=Raven/VirtualDirectory==/RavenDB " + `
					  "--set=Raven/AnonymousAccess==All";

	Write-Host "`"$serverExe $serverSettings`"";
	Start-Process cmd -ArgumentList /C, "`"$serverExe $serverSettings`"";
}
