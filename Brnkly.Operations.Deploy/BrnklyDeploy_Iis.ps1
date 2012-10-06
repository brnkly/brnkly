
function Remove-AllIisObjects
{
    Write-Host "Deleting all IIS sites...";
    $sites = Get-ChildItem -Path "iis:\sites" -Name; 
    $sites | % { Remove-Item "iis:\sites\$_" -Recurse; }
    
    Write-Host "Deleting all IIS app pools...";
    $appPools = Get-ChildItem -Path "iis:\apppools" -Name;
    $appPools | % { Remove-Item "iis:\apppools\$_" -Recurse; }
    
    Write-Host "Deleting all IIS SSL bindings...";
    $sslBindings = Get-ChildItem -Path "iis:\sslbindings" -Name;
    $sslBindings | % { Remove-Item "iis:\sslbindings\$_" -Recurse; }
    
    Write-Host;
}

function Stop-AllSitesAndAppPools
{
    param([String[]]$machines)

    Write-Banner "SectionStart" "Stopping application pools...";
    $machines | % {
        Invoke-Command -ComputerName $_ -ScriptBlock { 
            Write-Host "Ensuring IIS is started on $([Environment]::MachineName.ToLowerInvariant())...";
            Start-Service W3SVC;

            Import-Module WebAdministration;

            Write-Host "Stopping sites on $([Environment]::MachineName.ToLowerInvariant())...";
            Get-ChildItem "iis:\sites" | Stop-Website;

            Write-Host "Stopping application pools on $([Environment]::MachineName.ToLowerInvariant())...";
            Get-ChildItem "iis:\apppools" | where { (Get-WebAppPoolState $_.Name).Value -eq "Started" } | Stop-WebAppPool;
        };
    };
    Write-Banner "SectionEnd" "Application pools stopped.";
}

function Start-AllSitesAndAppPools
{
    param([String[]]$machines)

    Write-Banner "SectionStart" "Starting application pools...";
    $machines | % {
        Invoke-Command -ComputerName $_ -ScriptBlock { 
            Write-Host "Restarting IIS on $([Environment]::MachineName.ToLowerInvariant())...";
            Restart-Service W3SVC,NetMsmqActivator -Force -WarningAction SilentlyContinue
            Get-Service SMTPSVC -ErrorAction SilentlyContinue | Restart-Service

            Import-Module WebAdministration;

            Write-Host "Starting application pools on $([Environment]::MachineName.ToLowerInvariant())...";
            Get-ChildItem "iis:\apppools" | Start-WebAppPool;

            Write-Host "Starting sites on $([Environment]::MachineName.ToLowerInvariant())...";
            Get-ChildItem "iis:\sites" | Start-Website;
        };
    };
    Write-Banner "SectionEnd" "Application pools started.";
}

function Add-HttpCompressionMimeTypes
{
    Write-Host "Adding static and dynamic mime types to HttpCompression...";

    $staticTypes = Get-WebConfiguration -filter "/system.webServer/httpCompression/staticTypes/add" | % { $_.mimeType };
    $staticAtIndex =  if ($staticTypes.Length -gt 0) { ($staticTypes.Length - 1); } else { 0; };

    $dynamicTypes = Get-WebConfiguration -filter "/system.webServer/httpCompression/dynamicTypes/add" | % { $_.mimeType };
    $dynamicAtIndex =  if ($dynamicTypes.Length -gt 0) { ($dynamicTypes.Length - 1); } else { 0; };

    @("application/rss+xml", "application/atom+xml", "application/json") | % {
        if (-not ($staticTypes -contains $_)) 
        { 
            Add-WebConfiguration -filter "/system.webServer/httpCompression/staticTypes" -value @{mimeType="$_";enabled="true";} -atindex $staticAtIndex 
        }
        if (-not ($dynamicTypes -contains $_)) 
        { 
            Add-WebConfiguration -filter "/system.webServer/httpCompression/dynamicTypes" -value @{mimeType="$_";enabled="true";} -atindex $dynamicAtIndex 
        }
    }
}

function Create-IisApp
{
    param($appDef)

    $iisSitePath = "iis:\sites\$($appDef.Site)";
    $iisAppPath = "iis:\sites\$($appDef.Site)\$($appDef.Name)";
    
    if (Test-Path $iisAppPath) { return; }
    
    $appPoolName = if ([String]::IsNullOrEmpty($appDef.AppPool)) { "$($appDef.Name)AppPool"; } else { $appDef.AppPool; }
    $pathLeaf = if ([String]::IsNullOrEmpty($appDef.Path)) { $appDef.Name; } else { $appDef.Path; }
    $path = "$WebrootPath\$pathLeaf";
    
    Create-IisSite $appDef.Site;
    $disableOverlap = ($appDef.DisableAppPoolOverlap -or $false);
    Create-IisAppPool -Name $appPoolName -DisableOverlap $disableOverlap;

    Write-Host "Creating IIS app $iisAppPath...";
    Write-Verbose "  Path: $path";
    Write-Verbose "  AppPool: $appPoolName";
    
    $enabledProtocols = (Get-Item $iisSitePath).enabledProtocols + ",net.msmq";
    $app = New-Item $iisAppPath -Type Application -PhysicalPath $path;
    Set-ItemProperty $iisAppPath -name applicationPool -value $appPoolName;
    Set-ItemProperty $iisAppPath -name enabledProtocols -value $enabledProtocols;
}

function Create-IisSite
{
    param([String]$name)
    
    $iisSitePath = "iis:\sites\$name";
    if (Test-Path $iisSitePath) { return; }
    
    $siteDef = (Get-SiteDefinitions)[$name];
    $appPoolName = $name + "AppPool";
    $path = "$WebrootPath\$($siteDef.Path)";
    
    $disableOverlap = ($siteDef.DisableAppPoolOverlap -or $false);
    Create-IisAppPool -Name $appPoolName -DisableOverlap $disableOverlap;
    
    Write-Host "Creating IIS site $iisSitePath...";
    Write-Verbose "    Path: $path";
    Write-Verbose "    AppPool: $appPoolName";

    $id = if ($siteDef.Port -gt 0) { $siteDef.Port; } else { $siteDef.SslPort; }
    $site = New-Item $iisSitePath -Id $id -PhysicalPath $path `
        -Bindings @{ protocol="net.msmq"; bindingInformation="localhost" };
    Set-ItemProperty $iisSitePath -name applicationPool -value ($name + "AppPool");
    
    if ($siteDef.EnableWindowsAuth)
    {
        Write-Verbose "  Enabling Windows Authentication...";
        Set-WebConfigurationProperty -filter /system.webServer/security/authentication/windowsAuthentication `
            -name enabled -Value true -PSPath IIS:\ -Location $name;
        Write-Verbose "  Disabling Anonymous Authentication...";
        Set-WebConfigurationProperty -filter /system.webServer/security/authentication/anonymousAuthentication `
            -name enabled -Value false -PSPath IIS:\ -Location $name;
    }

    if ($siteDef.Port -gt 0)
    {
        Write-Verbose "  Creating http binding on port $($siteDef.Port)...";
        New-WebBinding -Name $name -Protocol http -Port $siteDef.Port -IPAddress "*";
    }
    
    if ($siteDef.SslPort -gt 0)
    {
        Write-Verbose "  Creating https binding on port $($siteDef.SslPort)...";
        New-WebBinding -Name $name -Protocol https -Port $siteDef.sslPort -IPAddress "*";
        $certThumbprint = "";
        if ($IsDev) 
        { 
            $certThumbprint = "CC192F5D62D0DFE8A6FE0A5D2E5A389AB9E530D2"; # self-signed in pfx file.
            Create-SelfSignedSslCert;
        } 
        else
        {
            $certThumbprint = "F5FE91D236D54847833B523CF087160BD035BAA8"; # prod cert.
        };

        Get-Item "cert:\LocalMachine\My\$certThumbprint" | `
            New-Item "iis:\SslBindings\0.0.0.0!$($siteDef.SslPort)" | Out-Null;
    }
    
    $enabledProtocols = 'http';	
    if ($siteDef.Port -gt 0 -and $siteDef.SslPort -gt 0) { $enabledProtocols = 'http,https'; }
    if ($siteDef.Port -eq 0 -and $siteDef.SslPort -gt 0) { $enabledProtocols = 'https'; }
    Set-ItemProperty $iisSitePath -Name enabledProtocols -Value $enabledProtocols;
}

function Create-IisAppPool
{
    param([String]$name, [Boolean]$disableOverlap = $false)
    
    $iisAppPoolPath = "iis:\apppools\$name";
    
    if (Test-Path $iisAppPoolPath) { return; }

    Write-Host "Creating IIS app pool $iisAppPoolPath...";
    $appPool = New-Item $iisAppPoolPath;
    $appPool | Stop-WebAppPool;
    $appPool.managedPipelineMode = "Integrated";
    $appPool.managedRuntimeVersion = "v4.0";
    $appPool.processModel.identityType = 2; #NetworkService
    $appPool.processModel.pingingEnabled = $false;
    $appPool.processModel.idleTimeout = "00:00:00";
    $appPool.recycling.disallowOverlappingRotation = $disableOverlap;
    $appPool | Set-Item
}

function Create-SelfSignedSslCert
{
    $cert = new-object System.Security.Cryptography.X509Certificates.X509Certificate2;
    $cert.Import("$LocalScriptsPath\SelfSignedSslCert.pfx", "ss", [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::MachineKeySet);
  
    $myStore = new-object System.Security.Cryptography.X509Certificates.X509Store("My", "LocalMachine");
    $authRootStore = new-object System.Security.Cryptography.X509Certificates.X509Store("AuthRoot", "LocalMachine");
    try
    {
        # Intentionally re-adding each time, even if it exists already.
        # Creating the IIS binding fails if we don't.
        $myStore.Open("MaxAllowed");
        $authRootStore.Open("MaxAllowed");
        $myStore.Add($cert);
        $authRootStore.Add($cert);
    }
    finally
    {
        $myStore.Close();
        $authRootStore.Close();
    }
}
