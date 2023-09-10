$TagName = $Args[0]

$AssemblyInfo = cat .\Properties\AssemblyInfo.cs
$NewVersion = $TagName.Substring(1)
$AssemblyInfo = $AssemblyInfo -replace '^\[assembly\: AssemblyVersion\(\"\d+\.\d+\.\d+\.\d+\"\)\]$', ('[assembly: AssemblyVersion("' + $NewVersion + '.0")]')
$AssemblyInfo = $AssemblyInfo -replace '^\[assembly\: AssemblyFileVersion\(\"\d+\.\d+\.\d+\.\d+\"\)\]$', ('[assembly: AssemblyFileVersion("' + $NewVersion + '.0")]')
Remove-Item .\Properties\AssemblyInfo.cs
Out-File -FilePath .\Properties\AssemblyInfo.cs -InputObject $AssemblyInfo