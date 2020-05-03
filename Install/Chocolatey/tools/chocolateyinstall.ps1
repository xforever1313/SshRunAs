$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64      = "https://files.shendrick.net/projects/sshrunas/releases/$($env:ChocolateyPackageVersion)/SshRunAs.msi"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'msi'
  url64bit      = $url64 # 64-bit only software.
  softwareName  = 'SshRunAs*'
  checksum64    = 'fc52d033c1aba173b96faad232a4e10b6a8cffc34ad66d4db62ff5008989fc99'
  checksumType64= 'sha256'

  # MSI
  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs
