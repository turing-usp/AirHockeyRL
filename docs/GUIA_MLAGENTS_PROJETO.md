# Guia do Projeto Unity + ML-Agents (AirHockeyRL)

Este documento resume como o projeto esta organizado, quais sao os requisitos, como treinar os agentes e como usar modelos treinados no jogo.

## 1) Visao geral do projeto

Projeto de Air Hockey em Unity com:
- modo de jogo (humano vs agente),
- modo de treino com ML-Agents (PPO),
- multiplas arenas para acelerar coleta de experiencias.

### Cenas principais
- `Assets/Scenes/MenuPrincipal.unity` (habilitada no Build Settings)
- `Assets/Scenes/GameScene.unity` (habilitada no Build Settings)
- `Assets/Scenes/SampleScene.unity` (cena de treino, desabilitada no Build Settings por padrao)

### Scripts centrais (ML)
- `Assets/Scripts/PusherAgent.cs`
- `Assets/Scripts/ArenaManager.cs`
- `Assets/Scripts/RLGoalRewarder.cs`
- `Assets/Scripts/EnvironmentGridSpawner.cs`
- `Assets/ML-Agents/Configs/air_hockey.yaml`

## 2) Requisitos

## Unity / pacotes
- Unity Editor: `6000.0.23f1`
- Pacote Unity ML-Agents: `com.unity.ml-agents 3.0.0`
- Sentis (inferencia de modelo): `com.unity.sentis 2.1.0`

## Python (treino externo)
Recomendado usar Python 3.10 (logs do projeto foram gerados com 3.10.12).

Pacotes Python recomendados para este projeto:
- `mlagents==1.1.0`
- `mlagents-envs==1.1.0`
- `tensorboard`

Exemplo (Windows PowerShell):

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install --upgrade pip
pip install mlagents==1.1.0 mlagents-envs==1.1.0 tensorboard
```

### Faixa de versao importante (ML-Agents 1.1.0)
`mlagents==1.1.0` exige Python entre `3.10.1` e `3.10.12`.

Se seu ambiente estiver em `3.10.19` (ou outro fora dessa faixa), ajuste:

```powershell
conda install -n <nome-do-env> python=3.10.12 -y
```

### Script de setup com ambiente ativo
Se voce ja ativou o ambiente conda e quer instalar tudo nele:

```powershell
conda activate ml_agents
powershell -ExecutionPolicy Bypass -File .\setup_conda_mlagents.ps1 -EnvName ml_agents -UseActiveEnvironment
```

### Script de setup a partir de terminal normal (sem Anaconda Prompt)
O script agora tenta localizar `conda.exe` automaticamente (ex: `<ANACONDA_ROOT>\Scripts\conda.exe`).

Exemplo:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup_conda_mlagents.ps1 -EnvName ml_agents
```

Se o conda estiver em caminho diferente, passe manualmente:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup_conda_mlagents.ps1 -EnvName ml_agents -CondaExe "<ANACONDA_ROOT>\Scripts\conda.exe"
```

### Diagnostico rapido (para colar no terminal e compartilhar saida)

```powershell
where conda
python --version
python -m pip --version
conda info --envs
conda list -n ml_agents
conda run -n ml_agents python --version
conda run -n ml_agents python -m pip show mlagents mlagents-envs setuptools
```

## 3) Estrutura de Assets (resumo)

- `Assets/ML-Agents/Configs`: YAML de treino (`air_hockey.yaml`)
- `Assets/ML-Agents/results`: modelos ONNX importados no Unity (para inferencia)
- `Assets/Scenes`: cenas do jogo e treino
- `Assets/Scripts`: logica de gameplay, controle humano, IA e RL
- `Assets/Models`, `Assets/Materials`, `Assets/Audios`: recursos visuais e de audio

## 4) Como o sistema de agentes funciona

## Agente RL (`PusherAgent`)
- Usa `BehaviorName = AirHockeyBrain`
- `MaxStep = 1000`
- Acao discreta com 5 opcoes:
  - `0` parar
  - `1` frente
  - `2` tras
  - `3` esquerda
  - `4` direita

### Observacoes (13 valores)
- posicao normalizada do proprio agente (2)
- velocidade normalizada do proprio agente (2)
- indicador de lado (1)
- posicao e velocidade do oponente (4)
- posicao e velocidade do puck (4)

### Recompensas
- penalidade por passo: `-0.001`
- shaping por lado do puck (pequena penalidade/bonus)
- toque no puck: `+0.0005` para quem toca, `-0.0005` para o oponente
- gol (`RLGoalRewarder` na cena): `+50` para quem marca, `-10` para quem sofre
- timeout no fim do episodio (`MaxStep`): `-5` e reset

### Reset de episodio
`ArenaManager` faz reset de puck + pushers e zera velocidades.

## Mistura ML x AI classica
Em `ArenaManager`, a cada episodio cada lado tem:
- 90% chance de usar `PusherAgent` (ML),
- 10% chance de usar `AIController`.

Isso esta no metodo `ApplyModeToOneSide()`.

## 5) Setup de treino no projeto

Cena recomendada para treino: `Assets/Scenes/SampleScene.unity`.

Nessa cena:
- `EnvironmentGridSpawner` esta ativo com `rows=3` e `columns=3`,
- isso cria 9 arenas em paralelo para acelerar treino,
- em varias arenas o `ScoreManager` fica com UI desativada (`uiEnabled=0`) para reduzir custo visual.

Arquivo de config PPO:
- `Assets/ML-Agents/Configs/air_hockey.yaml`
- comportamento: `AirHockeyBrain`
- `trainer_type: ppo`
- `max_steps: 1e8`

## 6) Como treinar os agentes

## Opcao A: treino com Unity aberto (Editor)
1. Abra `SampleScene` no Unity.
2. Deixe pronto para Play.
3. No terminal, na raiz do projeto, rode:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20
```

4. Aperte Play no Unity quando o trainer estiver aguardando conexao.

## Opcao B: treino com build (.exe)
1. Gere build da cena de treino.
2. Rode:

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --env "builds/AirHockeyRL.exe" --run-id AirHockey --time-scale 20 --no-graphics
```

## Retomar treino

```powershell
python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --resume --time-scale 20
```

## TensorBoard

```powershell
tensorboard --logdir results
```

## Onde saem os modelos
- Saida padrao do trainer: `results/<run-id>/AirHockeyBrain.onnx`
- Checkpoints: `results/<run-id>/AirHockeyBrain/`

## 7) Como usar um agente treinado no jogo

## Fluxo principal (GameScene)
1. Pegue o modelo ONNX final em:
   - `results/<run-id>/AirHockeyBrain.onnx`
2. Copie para dentro de `Assets` (exemplo):
   - `Assets/ML-Agents/results/<run-id>/AirHockeyBrain.onnx`
3. No Unity, selecione o pusher laranja em `GameScene`.
4. Em `Behavior Parameters`:
   - `Behavior Name`: `AirHockeyBrain`
   - `Behavior Type`: `Inference Only`
   - `Model`: arraste o `.onnx`
5. Execute a cena.

No setup atual de `GameScene`, o lado azul usa controle humano (`PusherController`) e o laranja usa inferencia ML.

## Fluxo alternativo (menu com selecao de modelo)
Existem scripts `MainMenuUI` + `GameSetup` para trocar modelo por menu via `GameConfig`, porem hoje o menu principal ativo usa `MainMenuController`.

Se quiser usar `MainMenuUI`, coloque modelos em:
- `Assets/Resources/Models/*.onnx`

## 8) Observacoes importantes do estado atual

- `Assets/Scripts/comand.txt` ja foi padronizado com comando relativo (`builds/AirHockeyRL.exe`).
- Existem resultados em dois lugares:
  - `results/` (saida do trainer Python),
  - `Assets/ML-Agents/results/` (modelos importados no Unity).
- O dropdown de dificuldade no menu (`EnemyAgentSelector`) esta configurado, mas no estado atual os tres campos (`easy/medium/hard`) apontam para o mesmo objeto.

## 9) Troubleshooting rapido

- Erro de versao/handshake entre Unity e Python:
  - alinhe versoes (`com.unity.ml-agents 3.0.0` no Unity e `mlagents==1.1.0` no Python).
- Erro `ModuleNotFoundError: No module named 'pkg_resources'`:
  - rode `python -m pip install "setuptools<81"` e tente novamente.
- Trainer nao conecta:
  - confirme que Unity esta em Play (modo Editor) ou que o caminho de `--env` esta correto (modo build).
- Agente nao se move:
  - confirme `Behavior Name` igual ao YAML (`AirHockeyBrain`) e modelo ONNX atribuido no `Behavior Parameters`.
- Porta ocupada:
  - troque `--base-port` no comando `mlagents-learn`.
