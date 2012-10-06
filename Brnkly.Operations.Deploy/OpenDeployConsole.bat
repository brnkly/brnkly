@ECHO OFF
SET CurrentDir=%~dp0
powershell.exe -NoExit -Command "Set-Location '%CurrentDir%'; Import-Module .\BrnklyDeploy.psm1;"
