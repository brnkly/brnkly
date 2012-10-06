
function Invoke-InstallApplications
{
    param([Switch]$Clean, [Switch]$RavenDB, [String]$RavenServerName)


    $machines = (Get-ItemsByMachine $script:Environment.Applications).Keys;
    if ($RavenDB) 
    { 
        if ([String]::IsNullOrEmpty($RavenServerName)) { Write-Error "The -RavenDB switch was used, but no value was given for -RavenServerName."; }
        $machines = @($RavenServerName);
    }

    Stop-AllSitesAndAppPools $machines;

    #$jobs = @();
    $machines | % {
        Write-Banner "SectionStart" "Invoking Install-Applications on $_ ...";
        Invoke-Command -ComputerName $_ `
            -Args $LocalScriptsPath, $BuildName, $script:Environment, $Clean, $RavenDB -ScriptBlock {
            param($LocalScriptsPath, $passthruBuildName, $passthruEnvironment, $passthruClean, $passthruRavenDB) 
            Set-Location $LocalScriptsPath;
            Import-Module .\BrnklyDeploy.psm1;
            Set-Environment $passthruEnvironment;
            Install-Applications -BuildName $passthruBuildName -Clean:$passthruClean -RavenDB:$passthruRavenDB;
        };
        Write-Banner "SectionEnd" "Install-Applications on $_ completed.";
        #$jobs += $job;
    };
    #Wait-Job $jobs | Receive-Job;

    Start-AllSitesAndAppPools $machines;
}

function Install-Applications
{
    param([Parameter(Mandatory=$true)][String]$BuildName, [Switch]$Clean, [Switch]$RavenDB)

    $script:BuildName = $BuildName;
    $script:LocalScriptsPath = "$LocalBrnklyDeployRoot\$BuildName\Brnkly.Operations.Deploy";

    $apps = Get-ApplicationsForThisMachine -RavenDB:$RavenDB;
        
    Write-Host "Installing applications from $BuildName on $ThisMachineName ..."; Write-Host;
    Import-Module WebAdministration;
    if ($Clean) { Remove-AllIisObjects; }
    Expand-BuildFiles -AppNames $apps;
    Add-HttpCompressionMimeTypes;
    Create-SkyPadApps -AppNames $apps;
    if (-not $RavenDB) 
    { 
        Create-SkyPadCounters; 
    };
}

function Get-ApplicationsForThisMachine
{
    param([Switch]$RavenDB)

    if ($RavenDB) 
    { 
        if (-not ($script:Environment.RavenServers -contains $script:ThisMachineName))
        { 
            Write-Error "The environment file's RavenServers property does not contain '$script:ThisMachineName'."; 
        }

        return @("RavenDB"); 
    } 
    else 
    { 
        return (Get-ItemsByMachine $script:Environment.Applications)[$ThisMachineName];
    }
}

function Install-CurrentBranch
{
    [CmdletBinding()]
    param([string[]]$Apps, [Switch]$Clean, [Switch]$AppsOnly, [Switch]$SqlOnly)

    if (-not $IsDev) { Write-Error "This cmdlet is only supported in dev environments."; }
    
    $path = (Get-Location).Path;
    
    Clear-Build;
    Clear-Environment;
    $script:WebrootPath = $path;
    $script:LocalScriptsPath = "$path\Brnkly.Operations.Deploy";
    Set-Environment -Environment "Dev";

    if (-not $SqlOnly)
    {
        if (($Apps -eq $null) -or ($Apps.Count -eq 0)) { $Apps = (Get-AppDefinitions).Keys; }
    
        Write-Host "Installing dev workspace from $path ..."; Write-Host;
        Import-Module WebAdministration;
        if ($Clean) { Remove-AllIisObjects; }
        Stop-AllSitesAndAppPools @($ThisMachineName);
        Add-HttpCompressionMimeTypes;
        Create-SkyPadApps -AppNames $Apps;
        Create-SkyPadCounters;
        Start-AllSitesAndAppPools @($ThisMachineName);
    }

    if (-not $AppsOnly)
    {
        $script:BuildName = "dev";
        $sqlDatabasesByMachine =  Get-ItemsByMachine $script:Environment.SqlDatabases;
        $sqlDatabasesByMachine.Keys | where { -not ($ThisMachineName -eq $_.ToLowerInvariant()) } | % {
            $copyTo = "\\$_\$RemoteBrnklyDeployRoot\$BuildName";
            Write-Host "Sending SQL database files to $copyTo ...";
            Clear-Directory $copyTo;
            Copy-Item "$path\Databases" $copyTo -Recurse;        
            Copy-Item $LocalScriptsPath $copyTo -Recurse;        
        };   
 
        $script:LocalScriptsPath = "$LocalBrnklyDeployRoot\$script:BuildName\Brnkly.Operations.Deploy";
        Invoke-InstallSqlDatabases; 
    }

    Clear-Build;
    Clear-Environment;
}

function Expand-BuildFiles
{
    param([String[]]$AppNames)

    # Raven takes some time to release our server bundle DLL.    
    Start-Sleep -Seconds 2;

    Write-Host "Expanding build files into $WebrootPath ...";
    $appDefinitions = Get-AppDefinitions;
    $siteDefinitions = Get-SiteDefinitions;
    $AppNames | % {
        $appDef = $appDefinitions[$_];
        $appFolderName = Get-AppFolderName $appDef;
        Clear-Directory "$WebrootPath\$appFolderName";
        Expand-Zip $LocalBrnklyDeployRoot\$BuildName\$appFolderName.7z $WebrootPath;

        $siteDef = $siteDefinitions[$appDef.Site];
        Clear-Directory "$WebrootPath\$($siteDef.Path)";
        Expand-Zip "$LocalBrnklyDeployRoot\$BuildName\$($siteDef.Path).7z" $WebrootPath;
    }
    
    Write-Host;
}

function Create-SkyPadApps
{
    param([String[]]$AppNames)
    
        $appDefinitions = Get-AppDefinitions;
        $AppNames | % {
        $appDef = $appDefinitions[$_];
        Write-Host; Write-Host "Creating application $($appDef.Name) ...";
        $appFolderName = Get-AppFolderName $appDef;
        Expand-ConfigTemplates "$WebrootPath\$appFolderName";
        Create-SkyPadAppQueues $appDef.Name;
        Create-IisApp $appDef;
    }
}

function Get-AppFolderName($appDef)
{
    if ([String]::IsNullOrEmpty($appDef.Path)) { $appDef.Name; } else { $appDef.Path; };
}
