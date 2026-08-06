@echo off
cd /d "%~dp0"
chcp 65001 > nul
echo Generazione struttura progetto in corso...

powershell -NoProfile -ExecutionPolicy Bypass -Command "$PWD = Get-Location; Get-ChildItem -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj|\.vs|\.git|\.vscode|node_modules|TestResults|packages|Documentazione)\\' } | Select-Object @{ Name='Struttura'; Expression={ $depth = ($_.FullName.Replace($PWD.Path, '').Split('\\').Count - 1); if ($_.PSIsContainer) { '{0}/ {1}' -f ('  ' * $depth), $_.Name } else { '{0}{1}' -f ('  ' * $depth), $_.Name } } } | Format-Table -HideTableHeaders | Out-File -FilePath 'struttura_progetto.txt' -Encoding utf8"

echo.
echo Completato! Il file 'struttura_progetto.txt' e' stato creato.
pause