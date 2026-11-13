@echo off
rem Lanca o build-and-test.ps1 (a logica vive no PowerShell).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-and-test.ps1"
