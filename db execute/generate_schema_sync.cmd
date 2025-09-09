@echo off
setlocal enabledelayedexpansion

set ROOT=%~dp0..
pushd "%ROOT%"

set "YASGMP_MYSQL_CS=Server=localhost;Database=yasgmp;User=root;Password=Jasenka1;Charset=utf8mb4;Allow User Variables=true;AllowPublicKeyRetrieval=True;SslMode=None;"

echo [SchemaSync] Building tool
dotnet build .\tools\SchemaSync\SchemaSync.csproj -c Release || goto :err

set OUTFILE=%~dp0\03_schema_sync.sql
echo [SchemaSync] Generating %OUTFILE%
dotnet run --project .\tools\SchemaSync\SchemaSync.csproj > "%OUTFILE%" || goto :err

echo [SchemaSync] Done. Review and run:
echo   mysql --protocol=tcp -h localhost -u root -p*** --default-character-set=utf8mb4 -D yasgmp ^< "%OUTFILE%"
goto :eof

:err
echo Failed. See errors above.
exit /b 1
