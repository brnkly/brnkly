
function prompt
{
    $envType = "PROD";
    if ($IsTest) { $envType = "TEST"; }
    if ($IsDev) { $envType = "DEV"; }
    $buildName = if ([String]::IsNullOrEmpty($script:BuildName)) { "NoBuild"; } else { $script:BuildName; };
    $envName = if ([String]::IsNullOrEmpty($script:EnvironmentName)) { "NoEnv"; } else { $script:EnvironmentName; };
    
    "$EnvType [$buildName > $envName] $(Get-Location)>";
}

function Get-ItemsByMachine($itemsToDeploy)
{
    # Input is a hashtable where the keys are either app names or db names, and each value is an array of machines.
    # Output is a hashtable where the keys are the machines, and each value is an array of either app or db names.
    $machines = @();
    $itemsToDeploy.Keys | % { $machines += $itemsToDeploy[$_] };
    
    $machinesWithItems = @{};
    foreach($machine in ($machines | sort -Unique))
    {
        $itemNames = $ItemsToDeploy.Keys | where { $ItemsToDeploy[$_] -Contains $machine };
        $machinesWithItems.Add($machine, $itemNames);
    }
    
    return $machinesWithItems;
}

function Clear-Directory
{
    param($dir)
    
    if (Test-Path $dir)
    {
        Write-Verbose "Deleting all items from directory $dir...";
        Remove-Item "$dir\*" -Recurse -Force;
    }
    else
    {
        Write-Verbose "Creating directory $dir...";
        New-Item -ItemType Directory $dir | Out-Null;
    }
}

function Expand-Zip($zipFile, $extractTo)
{
    Write-Verbose "  Extracting $zipFile to $extractTo...";
    $outParam = "-o$extractTo";
    & "$LocalScriptsPath\7z" x $zipFile $outParam -y | Out-Null;
    if (-not ($LastExitCode -eq 0))
    {
        Write-Error "7zip failed to extract '$zipFile' to '$extractTo'.";
    }
}

function Write-Banner($style, $message)
{
    switch($style)
    {
        "Main" { 
            Write-Host "";
            Write-Host "======================================================================";
            Write-Host $message;
            Write-Host "======================================================================";
            Write-Host "";
        }
        "SectionStart" { 
            Write-Host "";
            Write-Host "---------- $message ----------";
        }
        "SectionEnd" { 
            Write-Host "---------- $message ----------";
            Write-Host "";
        }
        default { 
            Write-Host $message; 
        }
    }
}

function Get-RavenServerPrefix
{
    param(
        [Parameter(Mandatory=$true)][string]$ServerName,
        [Parameter(Mandatory=$true)][string]$Database
    )

    $raven = New-WebClient -BaseAddress "http://$($ServerName):8080" -Header @{"User-Agent"="BrnklyDeploy"};
    $raven.DownloadString("ravendb/databases/$Database/docs/Raven/ServerPrefixForHilo");
}

function Set-RavenServerPrefix
{
    param(
        [Parameter(Mandatory=$true)][string]$ServerName,
        [Parameter(Mandatory=$true)][string]$Database,
        [Parameter(Mandatory=$true)][string]$ServerPrefix,
        [Switch]$Force
    )

    $raven = New-WebClient -BaseAddress "http://$($ServerName):8080" -Header @{"User-Agent"="BrnklyDeploy"};
    $prefixDocPath = "ravendb/databases/$Database/docs/Raven/ServerPrefixForHilo";
    try
    {
        $existing = $raven.DownloadString($prefixDocPath);
    }
    catch
    {
		$existing = $null;
    }

    if ($existing -and (-not $Force))
    {
        Write-Host "The document Raven/ServerPrefixForHilo already exists: $existing".
        Write-Error "In non-production environments, use the -Force switch to overwrite an existing prefix. You cannot overwrite an existing prefix in production using this script.";
        return;
    }

    $raven.UploadString($prefixDocPath, "PUT", "{ `"ServerPrefix`": `"$ServerPrefix`" }");
    
    Write-Host "Prefix saved: $($raven.DownloadString($prefixDocPath))";
}

# New-WebClient taken from:
# http://blogs.technet.com/b/heyscriptingguy/archive/2010/10/21/packaging-net-framework-classes-into-windows-powershell-functions.aspx
function New-WebClient
{
    [CmdletBinding(DefaultParameterSetName="Easy")]
    param(
        [string]$BaseAddress,

        [Parameter(ParameterSetName='Hard')][Net.ICredentials]$Credentials,
        [Parameter(ParameterSetName='Easy')][Management.Automation.PSCredential]$Credential,
        
        [Parameter(ParameterSetName='Hard')][Net.WebHeaderCollection]$Headers,
        [Parameter(ParameterSetName='Easy')][Hashtable]$Header,
        
        [Parameter(ParameterSetName='Hard')][Collections.Specialized.NameValueCollection]$QueryString,
        [Parameter(ParameterSetName='Easy')][Hashtable]$Query,
        
        [Parameter(ParameterSetName='Hard')][bool]$UseDefaultCredentials,
        [Parameter(ParameterSetName='Easy')][Switch]$Anonymous    
    )
    
    process {
        if ($psCmdlet.ParameterSetName -eq "Hard") {
            New-Object Net.Webclient -Property $psBoundParameters
        } else {
            $newWebClientParameters = @{} + $psBoundParameters

            $newWebClientParameters.UseDefaultCredentials = -not $Anonymous
            $null = $newWebClientParameters.Remove("Anonymous")

            if ($newWebClientParameters.Credential) {
                $newWebClientParameters.Credentials = $newWebClientParameters.Credential.GetNetworkCredential()
                $null = $newWebClientParameters.Remove("Credential")
            }

            if ($newWebClientParameters.Header) {
                $newWebClientParameters.Headers = New-Object Net.WebHeadercollection
                foreach ($headerPair in $newWebClientParameters.Header.GetEnumerator()) {
                    $null = $newWebClientParameters.Headers.Add($headerPair.Key, $headerPair.Value)
                }
                $null = $newWebClientParameters.Remove("Header")
            }

            if ($newWebClientParameters.Query) {
                $newWebClientParameters.QueryString = New-Object Collections.Specialized.NameValueCollection
                foreach ($QueryPair in $newWebClientParameters.Query.GetEnumerator()) {
                    $null = $newWebClientParameters.QueryString.Add($QueryPair.Key, $QueryPair.Value)
                }
                $null = $newWebClientParameters.Remove("Query")
            }
            New-WebClient @newWebclientParameters
        }
    }    
}
