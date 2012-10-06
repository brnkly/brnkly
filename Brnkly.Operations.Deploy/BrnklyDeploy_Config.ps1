
function Get-SiteDefinitions
{
    return @{
        Public =            @{ Name="Public";      Path="Public_SiteRoot";      Port=80; };
        PublicAuthn =       @{ Name="PublicAuthn"; Path="PublicAuthn_SiteRoot"; Port=8000; };
        RavenDB =           @{ Name="RavenDB";     Path="RavenDB_SiteRoot";     Port=8080; EnableWindowsAuth=$true; DisableAppPoolOverlap=$true; };
    }
}

function Get-AppDefinitions
{
    return @{
		Demo =              @{ Site="Public";      Name="Demo"; };        
        Administration =    @{ Site="PublicAuthn"; Name="Administration"; };
        RavenDB =           @{ Site="RavenDB";     Name="RavenDB"; DisableAppPoolOverlap=$true; };
    }
}

function Get-EnvironmentData
{
    param(
        [String[]]$WebServers = @(),
        [String[]]$RavenServers = @(), 
        [String[]]$SqlServers = @(),
        [System.Collections.Hashtable]$ConfigOverrides = @{})

    $env = @{
        ConfigValues = @{
            "AppSettings.DefaultRavenServer" = $RavenServers[0];
            "DriveLetter" = $DriveLetter;
            "System.Web.Compilation.Debug" = if ($IsDev) { "true"; } else { "false"; };
            "Authentication.Forms.Domain" = if ($IsDev) { "" } else { "MYDOMAIN.COM" };
        };

        RavenServers = $RavenServers;
            
        Applications = @{
			Demo = $WebServers;
            Administration = $WebServers;
        };
    
        SqlDatabases = @{ }
    }

    $ConfigOverrides.Keys | where { -not [String]::IsNullOrEmpty($_) } | `
        % { $env.ConfigValues[$_] = $ConfigOverrides[$_] };

    return $env;
}

function Get-EnvironmentObject
{
    param([Parameter(Mandatory=$true)][Alias("Env")]$Environment)
    
    if ($Environment.GetType().Name -eq "Hashtable") { return $Environment; }
    
    $envFile = $null;
    if ($Environment.EndsWith(".ps1")) { $envFile = $Environment; }
                                  else { $envFile = Get-EnvironmentFilePath $Environment; }
    if (-not (Test-Path $envFile)) { Write-Error "Could not locate the environment file '$envFile' or it is does not have the extension '.ps1'."; }

    $env = (& $envFile);    
    if ($env -eq $null) { Write-Error "The environment file '$envFile' did not return an object."; }
    
    return $env;
}

function Get-EnvironmentFilePath
{
    param([String]$envName)
 
    $paths = @(Get-ChildItem "$LocalScriptsPath\Environments" -Recurse -filter "$envName.ps1" |  % { $_.FullName });
    if ($paths.Count -gt 1) { Write-Error "Found multiple environment files named '$envName.ps1' under '$LocalScriptsPath\Environments'."; }
    if ($paths.Count -eq 1) { return $paths[0]; }
    return $null;
}

function Expand-ConfigTemplates($path)
{
    Write-Host "Processing config templates in $path...";
    @(Get-ChildItem -Path $path -Name -Include "*.template.config") | % {
        Expand-Template "$path\$_" ("$path\$_" -replace ".template.config", ".config");
    }
}

function Expand-Template($templateFile, $outFile)
{
    Write-Verbose "  Expanding template $templateFile ...";
    $tokens = $script:Environment.ConfigValues;
    (Get-Content $templateFile) | % {
        $line = $_;
        $tokens.Keys | where { $line.IndexOf("{$_}") -gt -1 } | % { 
            $line = $line -replace "{$_}", $tokens[$_] };
        if ($line.IndexOf("{Environment.Name}") -gt -1) 
        { 
            $line = $line -replace "{Environment.Name}", $script:Environment.Name; 
        }
        $line;
    } | Out-File -Encoding UTF8 $outFile -Force;
}
