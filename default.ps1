Properties {
	$base_dir  				= resolve-path .
	$lib_dir				= "$base_dir\lib"
	$build_dir 				= "$base_dir\build"
	$buildartifacts_dir		= "$build_dir\"
	$nuget_dir				= "$build_dir\NuGet";
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
  Remove-Item -Force -Recurse $buildartifacts_dir -ErrorAction SilentlyContinue
  Remove-Item -Force -Recurse $release_dir -ErrorAction SilentlyContinue
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
	if (Test-Path $nuget_dir) { Remove-Item -Force -Recurse $nuget_dir; Start-Sleep -Milliseconds 100; }
	New-Item $nuget_dir -Type Directory | Out-Null;

	$brnkly = New-NuGetPackager "Brnkly";
	& $brnkly.CopyDll;
	& $brnkly.CopyNuspec;
	& $brnkly.Pack;

	$admin = New-NuGetPackager "Brnkly.Admin";
	& $admin.CopyDll;
	& $admin.CopyNuspec;
	& $admin.CopyContent "$base_dir\Brnkly.Admin.Views\Areas\Brnkly" "Areas\Brnkly" -Recurse
	& $admin.Pack;
}

task Release { 
}
