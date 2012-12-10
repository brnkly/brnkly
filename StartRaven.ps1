param($serverNames = '', $port = 8081)

$serverExe = ".\packages\RavenDB.Server.2.0.2170-Unstable\tools\Raven.Server.exe";
#$serverExe = "C:\dev\ravendb\Raven.Server\bin\Debug\Raven.Server.exe";

@($serverNames) | %{

	$setHost = if ($_ -eq '') { '' } else { "--set=Raven/HostName==$_ " };
	$dataDir = if ($_ -eq '') { "$([Environment]::MachineName)-$port" } else { "$_-$port" };
	$serverSettings = $setHost + `
		              "--set=Raven/Port==$port " + `
					  "--set=Raven/DataDir==c:\RavenData\$dataDir\Data " + `
					  "--set=Raven/AnonymousAccess==None";

	Write-Host "`"$serverExe $serverSettings`"";
	Start-Process cmd -ArgumentList /C , "`"$serverExe /K $serverSettings`"";
}
