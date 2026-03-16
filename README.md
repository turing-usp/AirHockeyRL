<p align="center">
  <img src="/utils/banner.png" alt="banner" width="800"/>
</p>


# AirHockeyRL

Ambiente de Air Hockey desenvolvido em Unity para treinamento de agentes com Reinforcement Learning utilizando ML-Agents (PPO), suportando execução e treinamento diretamente no Unity.

![Unity](https://img.shields.io/badge/Unity-6000.0.23f1-black?logo=unity)
![Python](https://img.shields.io/badge/Python-3.10.12-blue?logo=python)
![ML-Agents](https://img.shields.io/badge/ML--Agents-PPO-green)

## Requisitos

| Ferramenta | Versão |
|------------|--------|
| Unity | `6000.0.23f1` |
| Python | `3.10.x` (recomendado `3.10.12`) |
| Conda | Anaconda ou Miniconda |


## Instalação

Na raiz do repositório, execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup_conda_mlagents.ps1 -EnvName ml_agents
```

Se o ambiente já estiver ativado:

```powershell
conda activate ml_agents
powershell -ExecutionPolicy Bypass -File .\scripts\setup_conda_mlagents.ps1 -EnvName ml_agents -UseActiveEnvironment
```

## Treino 

### No Editor

1. Abra `unity/Assets/Scenes/SampleScene.unity` no Unity.
2. No terminal, rode:

```powershell
python -m mlagents.trainers.learn unity/Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20 --resume
```

3. Aperte **Play** no Unity.

4. Acompanhe o progresso com TensorBoard:

```powershell
tensorboard --logdir results
```

### Via Build

Gere (se não houver) a build em `builds/AirHockeyRL.exe` pelo Unity (**File -> Build Settings -> Build**), depois:

```powershell
python -m mlagents.trainers.learn unity/Assets/ML-Agents/Configs/air_hockey.yaml --env "builds/AirHockeyRL.exe" --run-id AirHockey --time-scale 20 --resume --no-graphics
```

Considere usar o modo `--no-graphics`, que acelera bastante o treino em build.

## Arquivos principais

- `scripts/setup_conda_mlagents.ps1`
- `scripts/setup_conda_mlag`
