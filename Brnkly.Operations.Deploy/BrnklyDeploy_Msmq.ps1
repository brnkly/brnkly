function Create-SkyPadAppQueues([String]$appName)
{
    Add-Type -AssemblyName System.Messaging;

    $queueTypes = @{""=$true; "/tx"=$true; "/nontx"=$false; "/txdeadletter"=$true; };
    foreach ($suffix in $queueTypes.Keys)
    {
        $queueName = ".\private`$\$appName/Bus.svc$suffix";
        if (![System.Messaging.MessageQueue]::Exists($queueName))
        {
            Write-Host "  Creating queue $queueName ...";
            $queue = [System.Messaging.MessageQueue]::Create($queueName, $queueTypes[$suffix]);
            Set-SkyPadQueuePermissions $queue;
        }
    }
}

function Set-SkyPadQueuePermissions
{
    param([System.Messaging.MessageQueue]$queue)
    
    $administrators = New-Object System.Messaging.Trustee("Administrators");
    $networkService = New-Object System.Messaging.Trustee("NETWORK SERVICE");
    $everyone = New-Object System.Messaging.Trustee("Everyone");
    $anonymous = New-Object System.Messaging.Trustee("ANONYMOUS LOGON");

    $fullControl = [System.Messaging.MessageQueueAccessRights]::FullControl;
    $write = [System.Messaging.MessageQueueAccessRights]::WriteMessage;
    $peekReceiveWrite = [System.Messaging.MessageQueueAccessRights]::PeekMessage -bOr `
                        [System.Messaging.MessageQueueAccessRights]::ReceiveMessage -bOr `
                        [System.Messaging.MessageQueueAccessRights]::WriteMessage;
    
    $queue.SetPermissions((New-Object System.Messaging.MessageQueueAccessControlEntry($administrators, $fullControl)));
    $queue.SetPermissions((New-Object System.Messaging.MessageQueueAccessControlEntry($networkService, $peekReceiveWrite)));
    $queue.SetPermissions((New-Object System.Messaging.MessageQueueAccessControlEntry($everyone, $write)));
    $queue.SetPermissions((New-Object System.Messaging.MessageQueueAccessControlEntry($anonymous, $write)));
}
