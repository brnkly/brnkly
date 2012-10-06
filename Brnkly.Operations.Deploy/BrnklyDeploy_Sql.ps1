
function Invoke-InstallSqlDatabases
{
    #$jobs = @();
    $sqlDatabasesByMachine = Get-ItemsByMachine $script:Environment.SqlDatabases;
    $sqlDatabasesByMachine.Keys | % {
        Write-Banner "SectionStart" "Invoking Install-SqlDatabases on $_ ...";
        Invoke-Command -ComputerName $_ `
            -Args $LocalScriptsPath, $BuildName, $script:Environment -ScriptBlock {
            param($LocalScriptsPath, $passthruBuildName, $passthruEnvironment)
            Set-Location $LocalScriptsPath;           
            Import-Module .\BrnklyDeploy.psm1;
            Set-Environment $passthruEnvironment;
            Install-SqlDatabases -BuildName $passthruBuildName;
        };
        Write-Banner "SectionEnd" "Install-SqlDatabases on $_ completed.";
        #$jobs += job;
    };
    #Wait-Job $jobs | Receive-Job;
}

function Install-SqlDatabases
{
    param([Parameter(Mandatory=$true)][String]$BuildName)

    $script:BuildName = $BuildName;
    $script:LocalScriptsPath = "$LocalBrnklyDeployRoot\$BuildName\Brnkly.Operations.Deploy";
    
    if (-not ($BuildName -eq "dev")) { Expand-Zip "$LocalBrnklyDeployRoot\$BuildName\Databases.7z" "$LocalBrnklyDeployRoot\$BuildName"; }
    
    Write-Host "Installing SQL databases from $BuildName on $ThisMachineName ..."; Write-Host;
    $sqlScriptEnvName = Get-EnvironmentNameForSqlCmd;
    (Get-ItemsByMachine $script:Environment.SqlDatabases)[$ThisMachineName] | % {
        Write-Host "Executing SQL scripts for $_ ...";
        $scriptsFolder = Get-SqlScriptsFolderName $_;
        $scripts = Get-Content "$LocalBrnklyDeployRoot\$BuildName\Databases\$scriptsFolder\sqlscripts.txt" | `
            where { -not ($_.StartsWith(";") -or [String]::IsNullOrEmpty($_)) }
        foreach($script in $scripts)
        {
            $filePath = "$LocalBrnklyDeployRoot\$BuildName\Databases\$scriptsFolder\$script";
            $sqlCmd = "sqlcmd -i`"$filePath`" -s$ThisMachineName -dMaster -E -I -v DatabaseName=`"$_`" -v EnvironmentName=`"$sqlScriptEnvName`" >> `"$LocalScriptsPath\sqldeploy.log`""; 
            cmd /c $sqlCmd;
        }
    }
}

function Get-EnvironmentNameForSqlCmd
{
    if ($IsProd) { return "PROD"; }
    if ($IsTest) { return "TEST"; }
    if ($IsDev) { return "DEV"; }
}

function Get-SqlScriptsFolderName($dbName)
{
    switch ($dbName)
    {
        default { return $dbName; }
    }
}
