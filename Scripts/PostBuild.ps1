#If Args are not provided, set them to defaults
$NeosPath = if ($args[0] -ne $null) { $args[0] } else {"R:\SteamLibrary\steamapps\common\NeosVRBetaBuilds\NeosStandalone\app"}
$ConfigurationName = if ($args[1] -ne $null) { $args[1] } else {"AutoPostX"}
#If run from the Scripts folder directly, go up one folder to the project directory
if ($PWD.Path.EndsWith("Scripts")){
    $ProjectPath = Split-Path $PWD.Path -Parent
} else {
    $ProjectPath = $PWD.Path
}

$OutputLocation = "$($ProjectPath)\bin\$($ConfigurationName)\"
if (-not (Test-Path "$OutputLocation"))
{
    Write-Error "Output Location path '$OutputLocation' does not exist"
    Exit 2
}

Add-Type -Path "$($NeosPath)\Neos_Data\Managed\PostX.dll"

#Copy Required dlls from Neos to OutputLocation 
Copy-Item -Path "$($NeosPath)\Neos_Data\Managed\FrooxEngine.dll" -Destination $OutputLocation -ErrorAction Continue
Copy-Item -Path "$($NeosPath)\BaseX.dll" -Destination $OutputLocation -ErrorAction Continue
Copy-Item -Path "$($NeosPath)\Neos_Data\Managed\System.Threading.Tasks.Dataflow.dll" -Destination $OutputLocation -ErrorAction Continue

[PostX.NeosAssemblyPostProcessor].GetMethod("Process").Invoke($null, @("$OutputLocation\NeosFileStreamWriter.dll", "$OutputLocation"))