function Test-Net45Full
{
	if (Test-Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full") 
	{
		$release = Get-Item -Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" | `
						Select-Object -ExpandProperty Property | ? { $_ -eq "Release" }
		if ($release -ne $null) 
		{
			$value = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release).Release
			if($value.CompareTo(378389) -ge 0) 
			{
				return $true;
			}
		}
	}

	return $false;
}

function Get-Git-Commit
{
	$gitLog = git log --oneline -1
	return $gitLog.Split(' ')[0]
}
