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

function Set-AssemblyInfo
{
	param(
		[string]$file = $(throw "file is a required parameter."),
		[string]$version = $(throw "version is a required parameter."),
		[string]$title = $(throw "title is a required parameter."),
		[string]$description = "",
		[string]$company = "Brnkly",
		[string]$product = "Brnkly",
		[string]$copyright = "Copyright © 2012 NBC News Digital",
		[string]$clsCompliant = "true"
	)

  $asmInfo = "using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: CLSCompliantAttribute($clsCompliant)]
[assembly: ComVisibleAttribute(false)]
[assembly: AssemblyTitleAttribute(""$title"")]
[assembly: AssemblyDescriptionAttribute(""$description"")]
[assembly: AssemblyCompanyAttribute(""$company"")]
[assembly: AssemblyProductAttribute(""$product"")]
[assembly: AssemblyCopyrightAttribute(""$copyright"")]
[assembly: AssemblyVersionAttribute(""$version"")]
[assembly: AssemblyInformationalVersionAttribute(""$version"")]
[assembly: AssemblyFileVersionAttribute(""$version"")]
[assembly: AssemblyDelaySignAttribute(false)]
";

	$dir = [System.IO.Path]::GetDirectoryName($file)
	if ([System.IO.Directory]::Exists($dir) -eq $false)
	{
		Write-Host "Creating directory $dir"
		[System.IO.Directory]::CreateDirectory($dir)
	}
	Write-Host "Generating assembly info file: $file"
	Set-Content -Path $file -Value $asmInfo
}
