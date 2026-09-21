@echo off
title ZoneMart Server & AI Face Service
echo ==========================================================
echo   ZoneMart ASP.NET Core Web API (Port 5000)
echo   + UniFace Python Microservice (Port 8000 Auto-Start)
echo ==========================================================
cd /d "%~dp0server"
dotnet run
pause

