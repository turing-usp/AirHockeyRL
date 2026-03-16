param(
    [string]$EnvName = "ml_agents",
    [string]$PythonVersion = "3.10.12",
    [switch]$ForceRecreate,
    [switch]$UseActiveEnvironment,
    [string]$CondaExe = ""
)

$ErrorActionPreference = "Stop"

$script:CondaExePath = $null

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $display = "$FilePath $($Arguments -join ' ')".Trim()
    Write-Host ">> $display"

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao executar: $display"
    }
}

function Test-CondaEnvExists {
    param([string]$Name)
    $escaped = [Regex]::Escape($Name)
    $pattern = "^\s*$escaped(\s|$)"
    $envList = & $script:CondaExePath env list
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao listar ambientes conda."
    }
    return $null -ne ($envList | Select-String -Pattern $pattern)
}

function Invoke-InTargetEnv {
    param([string[]]$CommandArgs)

    if ($UseActiveEnvironment) {
        $file = $CommandArgs[0]
        $args = @()
        if ($CommandArgs.Length -gt 1) {
            $args = $CommandArgs[1..($CommandArgs.Length - 1)]
        }
        Invoke-Checked -FilePath $file -Arguments $args
    }
    else {
        Invoke-Checked -FilePath $script:CondaExePath -Arguments (@("run", "-n", $EnvName) + $CommandArgs)
    }
}

function Resolve-CondaExecutable {
    param([string]$PreferredPath)

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($PreferredPath)) {
        $candidates += $PreferredPath
    }

    $condaCmd = Get-Command conda -ErrorAction SilentlyContinue
    if ($condaCmd) { $candidates += $condaCmd.Source }

    $condaExeCmd = Get-Command conda.exe -ErrorAction SilentlyContinue
    if ($condaExeCmd) { $candidates += $condaExeCmd.Source }

    if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        $candidates += (Join-Path $env:USERPROFILE "anaconda3\Scripts\conda.exe")
        $candidates += (Join-Path $env:USERPROFILE "miniconda3\Scripts\conda.exe")
    }

    $candidates += "C:\ProgramData\anaconda3\Scripts\conda.exe"
    $candidates += "C:\ProgramData\miniconda3\Scripts\conda.exe"

    $seen = @{}
    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if ($seen.ContainsKey($candidate)) { continue }
        $seen[$candidate] = $true

        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    return $null
}

$requirementsFile = Join-Path $PSScriptRoot "requirements-mlagents.txt"
if (-not (Test-Path $requirementsFile)) {
    throw "Arquivo nao encontrado: $requirementsFile"
}

Write-Host ">> Preparando ambiente conda '$EnvName' (Python $PythonVersion)..."

$script:CondaExePath = Resolve-CondaExecutable -PreferredPath $CondaExe
if (-not $script:CondaExePath) {
    throw "Nao foi possivel localizar o conda.exe. Informe o caminho com -CondaExe, por exemplo: -CondaExe `"$env:USERPROFILE\anaconda3\Scripts\conda.exe`""
}

Write-Host ">> Usando conda em: $script:CondaExePath"

if ($UseActiveEnvironment) {
    $activeEnv = $env:CONDA_DEFAULT_ENV
    if ([string]::IsNullOrWhiteSpace($activeEnv)) {
        throw "Nenhum ambiente conda ativo. Rode 'conda activate <nome-do-env>' ou remova -UseActiveEnvironment."
    }

    if ($activeEnv -ne $EnvName) {
        Write-Host ">> Aviso: ambiente ativo '$activeEnv' diferente de '$EnvName'. Usando '$activeEnv'."
        $EnvName = $activeEnv
    }

    $pyVer = (& python -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}')").Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "Nao foi possivel ler a versao do Python do ambiente ativo."
    }
    Write-Host ">> Ambiente ativo detectado: '$EnvName' (Python $pyVer)"

    $pyVersionObj = [version]$pyVer
    if ($pyVersionObj -lt [version]"3.10.1" -or $pyVersionObj -gt [version]"3.10.12") {
        throw "Python $pyVer nao e compativel com mlagents==1.1.0. Rode: conda install -n $EnvName python=3.10.12 -y"
    }
}
else {
    if ($ForceRecreate -and (Test-CondaEnvExists -Name $EnvName)) {
        Write-Host ">> Removendo ambiente existente '$EnvName'..."
        Invoke-Checked -FilePath $script:CondaExePath -Arguments @("remove", "-y", "-n", $EnvName, "--all")
    }

    if (-not (Test-CondaEnvExists -Name $EnvName)) {
        Write-Host ">> Criando ambiente '$EnvName'..."
        Invoke-Checked -FilePath $script:CondaExePath -Arguments @("create", "-y", "-n", $EnvName, "python=$PythonVersion")
    }
    else {
        Write-Host ">> Ambiente '$EnvName' ja existe. Reutilizando..."
    }

    # Garante versao de Python compativel para mlagents==1.1.0.
    Invoke-Checked -FilePath $script:CondaExePath -Arguments @("install", "-y", "-n", $EnvName, "python=$PythonVersion")
}

Write-Host ">> Atualizando ferramentas base de pip..."
# mlagents==1.1.0 depende de pkg_resources (setuptools recente removeu).
Invoke-InTargetEnv @("python", "-m", "pip", "install", "--upgrade", "pip", "wheel")
Invoke-InTargetEnv @("python", "-m", "pip", "install", "setuptools<81")

Write-Host ">> Instalando dependencias do ML-Agents..."
Invoke-InTargetEnv @("python", "-m", "pip", "install", "-r", $requirementsFile)

Write-Host ">> Validando instalacao..."
Invoke-InTargetEnv @("python", "-c", "import mlagents, mlagents_envs; print('mlagents:', mlagents.__version__); print('mlagents_envs:', mlagents_envs.__version__)")
Invoke-InTargetEnv @("python", "-m", "mlagents.trainers.learn", "--help")

Write-Host ""
Write-Host "Instalacao concluida."
if (-not $UseActiveEnvironment) {
    Write-Host "Para usar o ambiente no terminal atual:"
    Write-Host "  conda activate $EnvName"
    Write-Host "Se 'conda' nao estiver no PATH no terminal atual, use:"
    Write-Host "  & `"$script:CondaExePath`" run -n $EnvName python -m mlagents.trainers.learn --help"
    Write-Host "Exemplo de treino sem ativar ambiente:"
    Write-Host "  & `"$script:CondaExePath`" run -n $EnvName python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20"
}
Write-Host "Exemplo de treino:"
Write-Host "  python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20"
