function Create-SkyPadCounters
{
    Create-ServiceBusCounters;
    Create-AutoDisablerCounters;
}

function Create-ServiceBusCounters
{
    Write-Host;

    $categoryName = "Brnkly Service Bus";

    if ($Clean -and [System.Diagnostics.PerformanceCounterCategory]::Exists($categoryName)) 
    { 
        Write-Host "Deleting service bus performance counters...";
        [System.Diagnostics.PerformanceCounterCategory]::Delete($categoryName); 
    }

    if ([System.Diagnostics.PerformanceCounterCategory]::Exists($categoryName)) { return; }
    
    Write-Host "Creating service bus performance counters...";
    $ccd = 'System.Diagnostics.CounterCreationData'
    $counters = New-Object System.Diagnostics.CounterCreationDataCollection
    $junk = $counters.Add((New-Object $ccd("Total Handled", "Number of messages successfully handled.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Handled/sec", "Number of messages successfully handled in the past second.", "RateOfCountsPerSecond32")));
    $junk = $counters.Add((New-Object $ccd("Total Failed", "Number of messages for which handling failed.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Failed/sec", "Number of messages for which handling failed in the past second.", "RateOfCountsPerSecond32")));
    $junk = $counters.Add((New-Object $ccd("Total Dead Letters", "Number of dead letters received.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Dead Letters/sec", "Number of dead letters received in the past second.", "RateOfCountsPerSecond32")));
    $junk = $counters.Add((New-Object $ccd("Total Sent", "Number of messages sent.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Sent/sec", "Number of messages sent in the past second.", "RateOfCountsPerSecond32")));
    $junk = $counters.Add((New-Object $ccd("Milliseconds since message sent", "Number of milliseconds from when the message was sent to when it was received (prior to handling). This indicates the total amount of time a message spent in transit and in the destination queue.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Milliseconds since activity started", "Number of milliseconds from when the first message in the activity was sent to when the current message was received (prior to handling). This indicates the total amount of time taken for all activity resulting from the original message.", "NumberOfItems32")));

    [System.Diagnostics.PerformanceCounterCategory]::Create(
        $categoryName, 
        "Performance counters for the service bus. This is a multi-instance category, with one instance per message type.",
        [Diagnostics.PerformanceCounterCategoryType]::MultiInstance, 
        $counters) | Out-Null;
}

function Create-AutoDisablerCounters
{
    Write-Host;

    $categoryName = "Brnkly Auto Disabler";

    if ($Clean -and [System.Diagnostics.PerformanceCounterCategory]::Exists($categoryName)) 
    { 
        Write-Host "Deleting auto disabler performance counters...";
        [System.Diagnostics.PerformanceCounterCategory]::Delete($categoryName); 
    }

    if ([System.Diagnostics.PerformanceCounterCategory]::Exists($categoryName)) { return; }
    
    Write-Host "Creating auto disabler performance counters...";
    $ccd = 'System.Diagnostics.CounterCreationData'
    $counters = New-Object System.Diagnostics.CounterCreationDataCollection
    $junk = $counters.Add((New-Object $ccd("Total Enables", "Number of times the auto disabler re-enabled its action.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Enables/sec", "Number of times the auto disabler re-enabled its action in the past second.", "RateOfCountsPerSecond32")));
    $junk = $counters.Add((New-Object $ccd("Total Disables", "Number of times the auto disabler disabled its action.", "NumberOfItems32")));
    $junk = $counters.Add((New-Object $ccd("Disables/sec", "Number of times the auto disabler disabled its action in the past second.", "RateOfCountsPerSecond32")));

    [System.Diagnostics.PerformanceCounterCategory]::Create(
        $categoryName, 
        "Performance counters for the auto disabler. This is a multi-instance category, with one instance per named auto disabler instance.",
        [Diagnostics.PerformanceCounterCategoryType]::MultiInstance, 
        $counters) | Out-Null;
}
