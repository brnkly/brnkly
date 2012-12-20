Properties {
	$base_dir  				= resolve-path .
	$lib_dir				= "$base_dir\lib"
	$build_dir 				= "$base_dir\build"
	$buildartifacts_dir		= "$build_dir\"
	$sln_file 				= "$base_dir\Brnkly.sln"
	$release_dir 			= "$base_dir\Release"
	#$build_number_default	= if ("$assembly_version".length -gt 0) { "$assembly_version" } else { "1.0.7" }
	#$build_number			= if ("$env:BUILD_NUMBER".length -gt 0) { "$env:BUILD_NUMBER" } else { "$build_number_default" } 
	#$build_vcs_number		= if ("$env:BUILD_VCS_NUMBER".length -gt 0) { "$env:BUILD_VCS_NUMBER" } else { "0" } 
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

include .\psake_ext.ps1

task default -depends CreateNuGetPackages

task Verify45 {
	if (Test-Net45Full) { Write-Host ".NET Framework 4.5 Full is installed" } 
	               else { throw ".NET Framework 4.5 Full is required, but is not installed on this machine." }
}

task Clean {
  remove-item -force -recurse $buildartifacts_dir -ErrorAction SilentlyContinue
  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue
}

task Init -depends Verify45, Clean {

	if($env:buildlabel -eq $null) {
		$env:buildlabel = "7"
	}

	exec { git update-index --assume-unchanged "$base_dir\CommonAssemblyInfo.cs" }
	$commit = Get-Git-Commit;
	(Get-Content "$base_dir\CommonAssemblyInfo.cs") | 
		Foreach-Object { $_ -replace ".07", ".$($env:buildlabel)" } |
		Foreach-Object { $_ -replace "{commit}", $commit } |
		Set-Content "$base_dir\CommonAssemblyInfo.cs" -Encoding UTF8;
	
	New-Item $release_dir -Type directory -ErrorAction SilentlyContinue | Out-Null;
	New-Item $buildartifacts_dir -Type directory -ErrorAction SilentlyContinue | Out-Null;
}

task Compile -depends Init {
	exec { & msbuild "$sln_file" /p:OutDir="$buildartifacts_dir\" /p:Configuration=Release }
}

task CreateNuGetPackages -depends Compile {
	$nuget_dir = "$build_dir\NuGet";
	if (Test-Path $nuget_dir) { Remove-Item -Force -Recurse $nuget_dir; }
	New-Item $nuget_dir -Type Directory | Out-Null;

	New-Item "$nuget_dir\Brnkly\lib\net45" -Type Directory | Out-Null;
	Copy-Item "$buildartifacts_dir\Brnkly.???" "$nuget_dir\Brnkly\lib\net45";

	Copy-Item "$base_dir\Brnkly\Brnkly.nuspec" "$nuget_dir\Brnkly";
	(Get-Content "$nuget_dir\Brnkly\Brnkly.nuspec") |
		Foreach-Object { $_ -replace ".07", ".$($env:buildlabel)" } |
		Set-Content "$nuget_dir\Brnkly\Brnkly.nuspec" -Encoding UTF8;

	exec { & "$base_dir\.nuget\nuget.exe" pack "$nuget_dir\Brnkly\Brnkly.nuspec" -OutputDirectory $nuget_dir }
}

task Release { 
}
