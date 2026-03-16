# AirHockeyRL

Projeto Unity de Air Hockey com ML-Agents (PPO), com suporte a treino no Editor e em build.

## Requisitos

- Unity `6000.0.23f1`
- Python `3.10.1` ate `3.10.12` (recomendado `3.10.12`)
- Conda (Anaconda ou Miniconda)

## Setup rapido (terminal normal)

Da raiz do repo:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup_conda_mlagents.ps1 -EnvName ml_agents
```

Se quiser usar ambiente ja ativado:

```powershell
conda activate ml_agents
powershell -ExecutionPolicy Bypass -File .\setup_conda_mlagents.ps1 -EnvName ml_agents -UseActiveEnvironment
```

## Treino (Editor)

1. Abra `Assets/Scenes/SampleScene.unity`.
2. Rode no terminal:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20 --resume
```

3. Aperte `Play` no Unity.

## Treino (Build)

Se existir build em `builds/AirHockeyRL.exe`:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --env "builds/AirHockeyRL.exe" --run-id AirHockey --time-scale 20 --resume --no-graphics
```

## Arquivos principais

- `setup_conda_mlagents.ps1`
- `setup_conda_mlagents.sh`
- `requirements-mlagents.txt`
- `GUIA_MLAGENTS_PROJETO.md`
