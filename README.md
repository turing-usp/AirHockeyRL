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

## Menu interativo de treino (YAML + rewards + matchup)

Com ambiente `ml_agents` ativo:

```powershell
python tools/train_menu.py
```

Esse menu permite:
- escolher o YAML base em `Assets/ML-Agents/Configs`
- ajustar rewards existentes (gol, penalidade, toque, timeout, etc)
- escolher quantidade de mesas (`rows` e `columns`, somente valores impares)
- escolher se quer treinar contra IA deterministica (que segue o puck) e a porcentagem de episodios disso
- listar `run-id` ja existentes em `results/` e ativar `--resume` automaticamente ao selecionar um
- criar um `run-id` novo quando quiser
- escolher a build detectada em `builds/` direto no menu (ou digitar caminho manual)
- escolher treino compartilhado (um behavior) ou split (BlueBrain vs OrangeBrain)
- escolher trainer de cada lado

Observacao: no stack atual do ML-Agents deste projeto, A2C e DQN nao sao trainers nativos.  
No menu eles sao mapeados para PPO automaticamente.
Quando o modo contra IA deterministica esta ativo, em cada episodio escolhido o lado da IA e sorteado (blue ou orange).

## Treino (Build)

Se existir build em `builds/training/AirHockeyRL.exe`:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --env "builds/training/AirHockeyRL.exe" --run-id AirHockey --time-scale 20 --resume --no-graphics
```

Exemplo split com dois agentes (PPO vs SAC) ja pronto:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey_split_example.yaml --env "builds/training/AirHockeyRL.exe" --run-id AirHockey_split --time-scale 20 --resume --no-graphics
```

## Duas builds recomendadas

- Build de treino:
  - Cena principal: `Assets/Scenes/SampleScene.unity`
  - Salvar em: `builds/Training/AirHockeyRL.exe`

- Build de jogo:
  - Cenas: `Assets/Scenes/MenuPrincipal.unity` e `Assets/Scenes/GameScene.unity`
  - Salvar em: `builds/Game/AirHockeyRL.exe`

## Arquivos principais

- `setup_conda_mlagents.ps1`
- `setup_conda_mlagents.sh`
- `requirements-mlagents.txt`
- `GUIA_MLAGENTS_PROJETO.md`
