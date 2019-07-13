<# 
   .SYNOPSIS - backup an AKS PVC
   .DESCRIPTION - 
   .PARAMETER - 
   .EXAMPLE - 

   Backup-Disk 
    

   .NOTES 
   
   Name :   Backup-Disk
   Author : golive@microsoft.com 
   Version: V1.0 Initial Version 
#>

function Backup-Disk
{
    [CmdletBinding()]
    param
    (
    )
    BEGIN
    {
        Write-Verbose "$((Get-Date).ToLongTimeString()) : Started running $($MyInvocation.MyCommand)"
    }
    PROCESS
    {
    }
    END
    {
        Write-Verbose "$((Get-Date).ToLongTimeString()) : Ended running $($MyInvocation.MyCommand)"
    }

}

