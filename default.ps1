Properties {
	$base_dir  				= resolve-path .
	$lib_dir				= "$base_dir\lib"
	$build_dir 				= "$base_dir\build"
	$buildartifacts_dir		= "$build_dir\"
	$sln_file 				= "$base_dir\Brnkly.sln"
	$release_dir 			= "$base_dir\Release"
	$build_number_default	= if ("$assembly_version".length -gt 0) { "$assembly_version" } else { "1.0.0.7" }
	$build_number			= if ("$env:BUILD_NUMBER".length -gt 0) { "$env:BUILD_NUMBER" } else { "$build_number_default" } 
	$build_vcs_number		= if ("$env:BUILD_VCS_NUMBER".length -gt 0) { "$env:BUILD_VCS_NUMBER" } else { "0" } 
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

include .\psake_ext.ps1

task default -depends Compile

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
		$env:buildlabel = "777"
	}

	exec { git update-index --assume-unchanged "$base_dir\CommonAssemblyInfo.cs" }
	$commit = Get-Git-Commit
	(Get-Content "$base_dir\CommonAssemblyInfo.cs") | 
		Foreach-Object { $_ -replace ".777.", ".$($env:buildlabel)." } |
		Foreach-Object { $_ -replace "{commit}", $commit } |
		Set-Content "$base_dir\CommonAssemblyInfo.cs" -Encoding UTF8
	
	New-Item $release_dir -itemType directory -ErrorAction SilentlyContinue | Out-Null
	New-Item $buildartifacts_dir -itemType directory -ErrorAction SilentlyContinue | Out-Null
}

task Compile -depends Init {
	exec { msbuild "$sln_file" /p:OutDir="$buildartifacts_dir\" /p:Configuration=Release }
}

task Publish -depends Compile {
}

task Release -depends Publish { 
}
