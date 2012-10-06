
$ErrorActionPreference = "Stop";

[String]$script:BuildNumberPattern = "\d{4}\.\d{2}\.\d{2}\.\d{1,3}";
[String]$script:BuildPath = $null;
[String]$script:BuildName = $null;
[System.Collections.Hashtable]$script:Environment = $null;
[String]$script:EnvironmentName = $null;

[String]$script:ThisMachineName = [Environment]::MachineName.ToLowerInvariant();
[Boolean]$script:IsDev = $true; # ($ThisMachineName -match "^dev-");
[Boolean]$script:IsTest = $false; # ($ThisMachineName -match "^test-");
[Boolean]$script:IsProd = (-not ($IsDev -or $IsTest));

[String]$script:DriveLetter = if ($IsDev) { "c"; } else { "e"; }
[String]$script:WebrootPath = "$DriveLetter`:\webroot";
[String]$script:LocalBrnklyDeployRoot = "$DriveLetter`:\BrnklyDeploy";
[String]$script:RemoteBrnklyDeployRoot = "$DriveLetter`$\BrnklyDeploy";
[String]$script:LocalScriptsPath = $null;
[String]$script:RemoteScriptsPath = $null;


. .\BrnklyDeploy_Apps.ps1
. .\BrnklyDeploy_Config.ps1
. .\BrnklyDeploy_Iis.ps1
. .\BrnklyDeploy_Msmq.ps1
. .\BrnklyDeploy_PerfCounters.ps1
. .\BrnklyDeploy_Sql.ps1
. .\BrnklyDeploy_Utility.ps1

function Set-Build
{
    param([Parameter(Mandatory=$true)][String]$buildName)

    if ($buildName -notmatch "$BuildNumberPattern`$") { Write-Error "Build value must end in a build number like '2012.03.04.5'."; }
    $buildPath = "\\MYBUILDDROPSERVER\drops\$buildName\Release\Deploy";
    if (-not (Test-Path $buildPath)) { Write-Error "The path '$buildPath' does not exist."; }

    $script:BuildName = $buildName -replace "\\", "_";
    $script:BuildPath = $buildPath;
    $script:LocalScriptsPath = "$LocalBrnklyDeployRoot\$script:BuildName\Brnkly.Operations.Deploy";
    $script:RemoteScriptsPath = "$RemoteBrnklyDeployRoot\$script:BuildName\Brnkly.Operations.Deploy";
}

function Clear-Build
{
    $script:BuildPath = $null;
    $script:BuildName = $null;
    $script:LocalScriptsPath = $null;
    $script:RemoteScriptsPath = $null;
}

function Set-Environment
{
    param([Parameter(Mandatory=$true)][Alias("Env")]$Environment)

    $script:Environment = (Get-EnvironmentObject $Environment);
    if ($Environment.GetType().Name -eq "Hashtable") 
    { 
        $script:EnvironmentName = $Environment.Name; 
        return; 
    }
    
    if ($Environment.EndsWith(".ps1")) 
    {
        $script:EnvironmentName = Split-Path -Leaf $Environment | % { $_.Substring(0, $_.IndexOf(".ps1")) };
    }
    else
    {
        $script:EnvironmentName = $Environment;
    }
    
    $script:Environment.Name = $script:EnvironmentName;
}

function Clear-Environment
{
    $script:Environment = $null;
    $script:EnvironmentName = $null;
}

function Receive-BuildFiles
{
    if ([String]::IsNullOrEmpty($script:BuildPath)) { Write-Error "No build has been set."; }
    
    $copyTo = "$LocalBrnklyDeployRoot\$BuildName";
    Write-Host "Receiving build files from $BuildPath to $copyTo ...";
    Clear-Directory $copyTo;
    Copy-Item "$BuildPath\*.7z" $copyTo;
    Copy-Item "$BuildPath\Brnkly.Operations.Deploy" "$copyTo\Brnkly.Operations.Deploy" -Recurse;    
}

function Send-BuildFiles
{
    if ([String]::IsNullOrEmpty($script:BuildPath)) { Write-Error "No build has been set."; }
    
    # TODO: Use jobs here to do copies in parallel.
    
    $appsByMachine = Get-ItemsByMachine $script:Environment.Applications;
    $appServers = $appsByMachine.Keys | % { $_.ToLowerInvariant() };
    if (-not ($appServers -eq $null))
    {
        $appServers | where { -not ($ThisMachineName -eq $_) } | % {
            $copyTo = "\\$_\$RemoteBrnklyDeployRoot\$BuildName";
            Write-Host "Sending application files to $copyTo ...";
            Clear-Directory $copyTo;
            Copy-Item "$LocalBrnklyDeployRoot\$BuildName\*.7z" $copyTo -Recurse;        
            Copy-Item $LocalScriptsPath $copyTo -Recurse;
        };
    }
    
    $script:Environment.RavenServers | % { $_.ToLowerInvariant() } | `
        where { -not ($appServers -contains $_) } | `
        where { -not ($ThisMachineName -eq $_) } | % {
            $copyTo = "\\$_\$RemoteBrnklyDeployRoot\$BuildName";
            Write-Host "Sending RavenDB files to $copyTo ...";
            Clear-Directory $copyTo;
            Copy-Item "$LocalBrnklyDeployRoot\$BuildName\RavenDB*.7z" $copyTo -Recurse;        
            Copy-Item $LocalScriptsPath $copyTo -Recurse;
    };

    $sqlDatabasesByMachine =  Get-ItemsByMachine $script:Environment.SqlDatabases;
    $sqlDatabasesByMachine.Keys | where { -not ($ThisMachineName -eq $_.ToLowerInvariant()) } | % {
        $copyTo = "\\$_\$RemoteBrnklyDeployRoot\$BuildName";
        Write-Host "Sending SQL database files to $copyTo ...";
        Clear-Directory $copyTo;
        Copy-Item "$LocalBrnklyDeployRoot\$BuildName\Databases.7z" $copyTo -Recurse;        
        Copy-Item $LocalScriptsPath $copyTo -Recurse;        
    };   
}

function Remove-BuildFiles
{
    param([Switch]$All)
    
    if ((-not $All) -and ([String]::IsNullOrEmpty($script:BuildPath))) { Write-Error "No build has been set."; }

    $appsByMachine = Get-ItemsByMachine $script:Environment.Applications;
    $sqlDatabasesByMachine = Get-ItemsByMachine $script:Environment.SqlDatabases;
    
    $allMachines = $appsByMachine.Keys + $sqlDatabasesByMachine.Keys;
    $allMachines | % {
        $toDelete = if ($All) { "\\$_\$RemoteBrnklyDeployRoot" } else { "\\$_\$RemoteBrnklyDeployRoot\$BuildName"; };
        Write-Host "Deleting folder $toDelete ...";
        if (Test-Path $toDelete) { Remove-Item $toDelete -Recurse; };
    }

    $toDelete = if ($All) { "$LocalBrnklyDeployRoot"; } else { "$LocalBrnklyDeployRoot\$BuildName"; };
    Write-Host "Deleting folder $toDelete ...";
    if (Test-Path $toDelete) { Remove-Item $toDelete -Recurse; };

    Clear-Build;    
}

function Start-Deployment
{
    param([Switch]$AppsOnly, [Switch]$SqlOnly, [Switch]$Clean)

    if ([String]::IsNullOrEmpty($script:BuildPath)) { Write-Error "No build has been set."; }

    Write-Banner "Main" "Starting deployment of $script:BuildName to $script:EnvironmentName";    
    if (-not $SqlOnly) { Invoke-InstallApplications -Clean:$Clean; }
    if (-not $AppsOnly) { Invoke-InstallSqlDatabases; }
}

function Start-RavenDeployment
{
    param([Parameter(Mandatory=$true)][String]$ServerName, [Switch]$Clean)

    if ([String]::IsNullOrEmpty($script:BuildPath)) { Write-Error "No build has been set."; }

    Write-Banner "Main" "Starting RavenDB deployment of $script:BuildName to $ServerName";    
    Invoke-InstallApplications -Clean:$Clean -RavenDB -RavenServerName $ServerName;
}

function Start-FullDeployment
{
    param([Parameter(Mandatory=$true)][String]$Environment)
    if ($Environment -notmatch "^sys") { Write-Error "The Start-FullDeployment function can only be used with sys environments"; }

    Receive-BuildFiles;
    Set-Environment $Environment;
    Send-BuildFiles;
    Start-RavenDeployment -Clean;
    Start-Deployment;
    Remove-BuildFiles;
}

$publicFunctions = @(
    "prompt",
    "Set-Build", 
    "Clear-Build",
    "Set-Environment", 
    "Clear-Environment",
    "Receive-BuildFiles", 
    "Send-BuildFiles", 
    "Remove-BuildFiles",
    "Start-Deployment",
    "Start-RavenDeployment",
    "Install-Applications", 
    "Install-SqlDatabases",
    "Install-CurrentBranch",
    "Enable-Pod",
    "Disable-Pod",
    "Show-PodState",
    "Start-FullDeployment",
    "Get-RavenServerPrefix",
    "Set-RavenServerPrefix"
);

Export-ModuleMember -Function $publicFunctions


if ((Get-Location).Path -match "PowerShell") # UNC path - typically when launched from batch file in drop folder.
{
    if ((Get-Location) -match "\\drops\\(.*\d{4}\.\d{2}\.\d{2}\.\d{1,3})")
    {
        $buildName = $matches[1];
        Set-Build $buildName;
    }

    if (-not (Test-Path $LocalBrnklyDeployRoot)) { New-Item -ItemType Directory $LocalBrnklyDeployRoot; }
    Set-Location $LocalBrnklyDeployRoot;    
}
else
{
    if ((Get-Location).Path.EndsWith("Brnkly.Operations.Deploy"))
    {
        Set-Location ..;
    }
}
