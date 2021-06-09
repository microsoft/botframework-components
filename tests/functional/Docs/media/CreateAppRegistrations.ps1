Write-Host "You will be prompted to login into your Azure account to create App Registrations"
az login

$defaultPrefix = "generic-app-registration"
$defaultAmount = 14
$appRegistrations = @()
do {
    $prefix = Read-Host -Prompt "App Registrations prefix ('$defaultPrefix' by default)"
    if($prefix -eq ""){
        $prefix = $defaultPrefix
    }
    $amount = Read-Host -Prompt "Amount to create ($defaultAmount by default)"
    if($amount -eq ""){
        $amount = $defaultAmount
    }

    For ($index = 1; $index -le $amount; $index++) {
        $name = "$prefix-$index"
        Write-Host "Creating $name..."
        $id = (az ad app create --display-name $name --available-to-other-tenants | ConvertFrom-Json).appId
        $password = (az ad app credential reset --id $id | ConvertFrom-Json).password
        $appRegistration = @{
            name = $name
            id = $id
            password = $password
        }
        Write-Host ($appRegistration | ConvertTo-Json) "`n"
        $appRegistrations += $appRegistration
    }
} while ((Read-Host -Prompt 'Batch completed. Create another batch? y/n').ToUpper() -eq "Y")

if((Read-Host -Prompt "Output App Registrations to a file? y/n").ToUpper() -eq "Y"){
    $defaultPath = ".\appRegistrations.json"
    $path = Read-Host -Prompt "Where? ('$defaultPath' by default)"
    if($path -eq ""){
        $path = $defaultPath
    }
    $appRegistrations | ConvertTo-Json | Out-File $path
}
