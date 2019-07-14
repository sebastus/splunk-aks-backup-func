<# 
   .SYNOPSIS - backup Splunk indexer data & config disks
   .DESCRIPTION - 
   .PARAMETER - 
   .EXAMPLE - 

   Backup-SplunkData
    

   .NOTES 
   
   Name :   Backup-SplunkData
   Author : golive@microsoft.com 
   Version: V1.0 Initial Version 
#>

function Connect-AksEnvironment
{
    param
    (
        [Parameter(Mandatory=$true)]
        [String]$tenant_id,

        [Parameter(Mandatory=$true)]
        [String]$app_id,

        [Parameter(Mandatory=$true)]
        [String]$app_key,

        [Parameter(Mandatory=$true)]
        [String]$subscription_id,

        [Parameter(Mandatory=$true)]
        [String]$aks_rg,

        [Parameter(Mandatory=$true)]
        [String]$aks_name
    )    
    $passwd = ConvertTo-SecureString $app_key -AsPlainText -Force
    $pscredential = New-Object System.Management.Automation.PSCredential($app_id, $passwd)
    Connect-AzAccount -ServicePrincipal -Credential $pscredential -TenantId $tenant_id

    Get-AzSubscription -SubscriptionId $subscription_id -TenantId $tenant_id | Set-AzContext

    Import-AzAksCredential -ResourceGroupName $aks_rg -Name $aks_name -Force
}

function Get-Env
{
    param
    (
        [Parameter(Mandatory=$true)]
        [String]$env
    )
    $var = Get-ChildItem env:$env
    $var.value
}

function Backup-SplunkData
{
    [CmdletBinding()]
    param
    (
    )
    BEGIN
    {
        Write-Verbose "$((Get-Date).ToLongTimeString()) : Started running $($MyInvocation.MyCommand)"

        # get environment vars
        $tenant_id = Get-Env "AZURE_TENANT_ID"
        $app_id = Get-Env "AZURE_APP_ID"
        $app_key = Get-Env "AZURE_APP_KEY"
        $subscription_id = Get-Env "AZURE_SUBSCRIPTION_ID"

        $aks_rg = Get-Env "AKS_RG"
        $aks_asset_rg = Get-Env "AKS_ASSET_RG"
        $aks_name = Get-Env "AKS_NAME"

        Connect-AksEnvironment -tenant_id $tenant_id -app_id $app_id -app_key $app_key `
            -subscription_id $subscription_id -aks_rg $aks_rg -aks_name $aks_name
    }
    PROCESS
    {
        kubectl -n splunk get pod -l role=splunk_indexer -o json > pods_as.json
        kubectl -n splunk get pvc -o json > pvc_as.json
        
        $pods = (get-content ./pods_as.json | convertfrom-json)
                
        foreach ($item in $pods.items)
        {
            $podName = $item.metadata.name

            Write-Verbose "Looking at $podName disks..."

            foreach ($volumeMount in $item.spec.containers.volumeMounts)
            {

                if ($volumeMount.name -like "splunk-idxcluster-*")
                {
                    $volumeName = $volumeMount.name
        
                    $diskName = "$volumeName-$podName"

                    Write-Verbose "Backing up $diskName..."

                    Backup-Disk -aks_asset_rg $aks_asset_rg -diskname $diskName
                }
            }
        }
    }
    END
    {
        Write-Verbose "$((Get-Date).ToLongTimeString()) : Ended running $($MyInvocation.MyCommand)"
    }

}

