function Test-Net45Full
{
	$net4RegKey = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
	$net45Release = 378389;
	if (Test-Path $net4RegKey)
	{
		$release = (Get-Item $net4RegKey | Get-ItemProperty -Name Release -ErrorAction SilentlyContinue)
		if ($release -ne $null -and
			$release.Release -ne $null -and
			$release.Release.CompareTo($net45Release) -ge 0)
		{
			return $true;
		}
	}

	return $false;
}

function Get-Git-Commit
{
	$gitLog = git log --oneline -1
	return $gitLog.Split(' ')[0]
}

function New-NuGetPackager
{
	param($projectName)

	# Bring these into local scope so the closures get them.
	$base_dir = $base_dir;
	$nuget_dir = $nuget_dir;
	$buildartifacts_dir = $buildartifacts_dir;

	return @{

		Name = { return $projectName; }.GetNewClosure();

		CopyDll = {
			New-Item "$nuget_dir\$projectName\lib\net45" -Type Directory | Out-Null;
			Copy-Item "$buildartifacts_dir\$($projectName).???" "$nuget_dir\$projectName\lib\net45";
		}.GetNewClosure();

		CopyNuspec = {
			New-Item "$nuget_dir\$projectName" -Type Directory -ErrorAction SilentlyContinue | Out-Null;
			Copy-Item "$base_dir\$projectName\$($projectName).nuspec" "$nuget_dir\$projectName";
			(Get-Content "$nuget_dir\$projectName\$($projectName).nuspec") |
				Foreach-Object { $_ -replace ".07", ".$($env:buildlabel)" } |
				Set-Content "$nuget_dir\$projectName\$($projectName).nuspec" -Encoding UTF8;
		}.GetNewClosure();

		CopyContent = {
			param($fromPath, $contentRelativePath, [Switch]$Recurse)
			Copy-Item "$fromPath" "$nuget_dir\$projectName\Content\$contentRelativePath" -Recurse:$Recurse;
		}.GetNewClosure();

		Pack = {
			exec { & "$base_dir\.nuget\nuget.exe" pack "$nuget_dir\$projectName\$($projectName).nuspec" -OutputDirectory $nuget_dir }
		}.GetNewClosure();

	};
}