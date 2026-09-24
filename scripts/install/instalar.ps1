<#
  RobloxServerLauncher - instalador.

  Coloca o launcher compilado (pasta "launcher" deste zip) dentro de uma instalacao do Novetus e abre o jogo:
    1. procura o Novetus (parametro -Novetus, pasta de destino ou pasta Downloads); se nao achar, abre
       https://bitl.itch.io/novetus e espera o download terminar;
    2. extrai o Novetus em -Destino;
    3. faz backup de data\bin, do NovetusBootstrapper.exe e do AddonLoader.lua;
    4. copia o launcher e o addon RobloxServerAuth;
    5. cria um atalho na Area de Trabalho e abre o NovetusBootstrapper.exe.

  Uso: instalar.bat
       instalar.bat -Novetus "C:\caminho\Novetus.zip"      (ou uma pasta com o Novetus ja extraido)
       instalar.bat -Destino "D:\Jogos\Novetus"
#>
param(
    [string]$Novetus = "",
    [string]$Destino = (Join-Path $env:USERPROFILE "RobloxServerNovetus"),
    [switch]$NaoAbrir,
    [switch]$SemPausa
)

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$launcher = Join-Path $here "launcher"
$addonsSource = Join-Path $here "addons"

function Write-Step([string]$text) {
    Write-Host ""
    Write-Host "==> $text" -ForegroundColor Cyan
}

function Stop-Install([string]$text) {
    Write-Host ""
    Write-Host "ERRO: $text" -ForegroundColor Red
    if (-not $SemPausa) { Read-Host "Aperte Enter para sair" | Out-Null }
    exit 1
}

function Test-NovetusRoot([string]$dir) {
    return (Test-Path (Join-Path $dir "NovetusBootstrapper.exe")) -and (Test-Path (Join-Path $dir "data\config\info.json"))
}

function Find-NovetusRoot([string]$dir) {
    if (-not (Test-Path $dir)) { return $null }
    if (Test-NovetusRoot $dir) { return (Resolve-Path $dir).Path }
    $found = Get-ChildItem -Path $dir -Filter "NovetusBootstrapper.exe" -Recurse -ErrorAction SilentlyContinue |
        Where-Object { Test-NovetusRoot $_.DirectoryName } |
        Select-Object -First 1
    if ($found) { return $found.DirectoryName }
    return $null
}

function Expand-Novetus([string]$archive, [string]$target) {
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    if ($archive -match '\.zip$') {
        Expand-Archive -Path $archive -DestinationPath $target -Force
        return
    }
    $sevenZip = @("$env:ProgramFiles\7-Zip\7z.exe", "${env:ProgramFiles(x86)}\7-Zip\7z.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $sevenZip) {
        Stop-Install "Para extrair $archive instale o 7-Zip (https://www.7-zip.org/), ou extraia voce mesmo e rode: instalar.bat -Novetus `"pasta extraida`""
    }
    & $sevenZip x $archive "-o$target" -y | Out-Null
    if ($LASTEXITCODE -ne 0) { Stop-Install "O 7-Zip nao conseguiu extrair $archive." }
}

function Find-NovetusDownload {
    $downloads = Join-Path $env:USERPROFILE "Downloads"
    return Get-ChildItem -Path $downloads -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^Novetus.*\.(zip|7z)$' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Wait-NovetusDownload {
    Write-Host "Vou abrir a pagina do Novetus no navegador. Baixe a versao completa do Novetus (o download e gratuito)."
    Write-Host "Assim que o arquivo terminar de baixar na pasta Downloads, este instalador continua sozinho."
    Start-Process "https://bitl.itch.io/novetus"
    $deadline = (Get-Date).AddMinutes(30)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 5
        $candidate = Find-NovetusDownload
        if ($candidate) {
            # espera o navegador terminar de escrever o arquivo
            $size = $candidate.Length
            Start-Sleep -Seconds 3
            $candidate.Refresh()
            if ($size -gt 0 -and $candidate.Length -eq $size) { return $candidate }
        }
    }
    return $null
}

if (-not (Test-Path (Join-Path $launcher "data\bin\Novetus.exe"))) {
    Stop-Install "Nao encontrei a pasta 'launcher' ao lado deste script. Extraia o zip RobloxServerLauncher inteiro antes de rodar."
}

Write-Step "Procurando o Novetus"
$root = $null
if ($Novetus) {
    if (Test-Path $Novetus -PathType Container) {
        $root = Find-NovetusRoot $Novetus
    } elseif (Test-Path $Novetus) {
        Write-Step "Extraindo $Novetus em $Destino"
        Expand-Novetus (Resolve-Path $Novetus).Path $Destino
        $root = Find-NovetusRoot $Destino
    }
    if (-not $root) { Stop-Install "Nao encontrei o Novetus completo (NovetusBootstrapper.exe + pasta data) em $Novetus." }
} else {
    $root = Find-NovetusRoot $Destino
}

if (-not $root) {
    $download = Find-NovetusDownload
    if (-not $download) { $download = Wait-NovetusDownload }
    if (-not $download) {
        Stop-Install "Nao encontrei o download do Novetus na pasta Downloads. Se voce baixou um instalador (.exe), instale e rode: instalar.bat -Novetus `"pasta do Novetus`""
    }
    Write-Step "Extraindo $($download.Name) em $Destino"
    Expand-Novetus $download.FullName $Destino
    $root = Find-NovetusRoot $Destino
    if (-not $root) { Stop-Install "O arquivo $($download.Name) nao tem o Novetus completo (NovetusBootstrapper.exe + pasta data)." }
}
Write-Host "Novetus encontrado em: $root"

if (Get-Process -Name "Novetus", "NovetusBootstrapper" -ErrorAction SilentlyContinue) {
    Stop-Install "Feche o Novetus antes de instalar."
}

Write-Step "Fazendo backup"
$backup = Join-Path $root ("backup-robloxserver-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
New-Item -ItemType Directory -Force -Path $backup | Out-Null
Copy-Item (Join-Path $root "data\bin") (Join-Path $backup "bin") -Recurse
Copy-Item (Join-Path $root "NovetusBootstrapper.exe") $backup
$loader = Join-Path $root "data\addons\core\AddonLoader.lua"
if (Test-Path $loader) { Copy-Item $loader $backup }
Write-Host "Backup em: $backup"

Write-Step "Copiando o launcher do RobloxServer"
Copy-Item (Join-Path $launcher "data\bin\*") (Join-Path $root "data\bin") -Recurse -Force
Get-ChildItem -Path $launcher -File | Copy-Item -Destination $root -Force

$addons = Join-Path $root "data\addons"
New-Item -ItemType Directory -Force -Path (Join-Path $addons "core") | Out-Null
Copy-Item (Join-Path $addonsSource "RobloxServerAuth.lua") $addons -Force
if (Test-Path $loader) {
    # adiciona "RobloxServerAuth" a lista de addons sem trocar o resto do AddonLoader desta versao do Novetus
    $text = [IO.File]::ReadAllText($loader)
    if ($text -notmatch '"RobloxServerAuth"') {
        $text = [regex]::Replace($text, '(?m)^(\s*Addons\s*=\s*\{)([^}]*)\}', {
            param($m)
            $inner = $m.Groups[2].Value.Trim()
            if ($inner) { $m.Groups[1].Value + $inner + ', "RobloxServerAuth"}' } else { $m.Groups[1].Value + '"RobloxServerAuth"}' }
        })
        [IO.File]::WriteAllText($loader, $text)
    }
} else {
    Copy-Item (Join-Path $addonsSource "core\AddonLoader.lua") (Join-Path $addons "core") -Force
}

Write-Step "Criando atalho na Area de Trabalho"
try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "RobloxServer Launcher.lnk"))
    $shortcut.TargetPath = Join-Path $root "NovetusBootstrapper.exe"
    $shortcut.WorkingDirectory = $root
    $shortcut.Save()
} catch {
    Write-Host "Nao foi possivel criar o atalho: $($_.Exception.Message)"
}

Write-Step "Pronto!"
Write-Host "No launcher: Server Browser -> ROBLOXSERVER GAMES... -> endereco do site (ex.: localhost:8080)."
Write-Host "Se os clientes aparecerem como invalidos, o clientinfo.nov desta versao do Novetus usa chaves privadas"
Write-Host "que o codigo publico nao tem; recrie-o em SDK -> ClientInfo Creator."
if (-not $NaoAbrir) {
    Start-Process -FilePath (Join-Path $root "NovetusBootstrapper.exe") -WorkingDirectory $root
}
if (-not $SemPausa) { Read-Host "Aperte Enter para fechar" | Out-Null }
