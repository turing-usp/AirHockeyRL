#!/usr/bin/env python3
"""
Interactive training menu for ML-Agents.

Features:
- Choose a base YAML from the Unity ML-Agents config folder
- Override existing reward values via environment_parameters
- Choose shared training (single behavior) or split training (Blue vs Orange)
- Choose trainer type per side (PPO/SAC/POCA). A2C/DQN are mapped to PPO.
- Optional training against deterministic AI (puck-following) with configurable episode percentage
- Optionally run mlagents-learn command immediately
"""

from __future__ import annotations

import copy
import datetime as dt
import shlex
import subprocess
import sys
from pathlib import Path
from typing import Dict, List, Optional, Tuple

import yaml


REPO_ROOT = Path(__file__).resolve().parents[1]
UNITY_PROJECT_ROOT = REPO_ROOT / "unity" if (REPO_ROOT / "unity" / "Assets").exists() else REPO_ROOT
CONFIG_DIR = UNITY_PROJECT_ROOT / "Assets" / "ML-Agents" / "Configs"
GENERATED_DIR = CONFIG_DIR / "generated"
RESULTS_DIR = REPO_ROOT / "results"
BUILDS_DIR = REPO_ROOT / "builds"

TRAINER_OPTIONS = [
    ("PPO", "ppo"),
    ("SAC", "sac"),
    ("POCA", "poca"),
    ("A2C (mapped to PPO)", "a2c"),
    ("DQN (mapped to PPO)", "dqn"),
]
TRAINER_ALIAS = {
    "a2c": "ppo",
    "dqn": "ppo",
}


def ask_text(prompt: str, default: Optional[str] = None) -> str:
    if default is None:
        raw = input(f"{prompt}: ").strip()
        return raw
    raw = input(f"{prompt} [{default}]: ").strip()
    return raw if raw else default


def ask_yes_no(prompt: str, default: bool) -> bool:
    suffix = "Y/n" if default else "y/N"
    raw = input(f"{prompt} ({suffix}): ").strip().lower()
    if not raw:
        return default
    return raw in ("y", "yes", "s", "sim")


def ask_choice(prompt: str, options: List[str], default_index: int = 0) -> int:
    print(f"\n{prompt}")
    for i, item in enumerate(options, 1):
        marker = " (default)" if i - 1 == default_index else ""
        print(f"  {i}. {item}{marker}")
    while True:
        raw = input("Escolha o numero: ").strip()
        if not raw:
            return default_index
        if raw.isdigit():
            idx = int(raw) - 1
            if 0 <= idx < len(options):
                return idx
        print("Entrada invalida. Tente novamente.")


def ask_optional_float(prompt: str, default: Optional[float] = None) -> Optional[float]:
    label = "vazio = manter valor do YAML"
    if default is not None:
        raw = input(f"{prompt} [{default}] ({label}): ").strip()
        if not raw:
            return default
    else:
        raw = input(f"{prompt} ({label}): ").strip()
        if not raw:
            return None
    try:
        return float(raw)
    except ValueError:
        print("Valor invalido, mantendo sem override.")
        return None


def ask_positive_odd_int(prompt: str, default: int) -> int:
    while True:
        raw = input(f"{prompt} [{default}] (apenas impar): ").strip()
        if not raw:
            value = default
        else:
            if not raw.isdigit():
                print("Entrada invalida. Use um inteiro positivo impar.")
                continue
            value = int(raw)

        if value < 1:
            print("Valor invalido. Deve ser >= 1.")
            continue
        if value % 2 == 0:
            print("Valor invalido. Use apenas numero impar.")
            continue
        return value


def ask_float_in_range(prompt: str, default: float, min_value: float, max_value: float) -> float:
    while True:
        raw = input(f"{prompt} [{default}]: ").strip()
        if not raw:
            value = default
        else:
            try:
                value = float(raw)
            except ValueError:
                print("Entrada invalida. Use um numero.")
                continue

        if value < min_value or value > max_value:
            print(f"Valor invalido. Deve estar entre {min_value} e {max_value}.")
            continue
        return value


def load_yaml(path: Path) -> Dict:
    with path.open("r", encoding="utf-8") as f:
        return yaml.safe_load(f) or {}


def dump_yaml(path: Path, data: Dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as f:
        yaml.safe_dump(data, f, sort_keys=False)


def list_base_configs() -> List[Path]:
    return sorted(
        p for p in CONFIG_DIR.glob("*.yaml") if p.is_file()
    )


def list_existing_runs() -> List[Path]:
    if not RESULTS_DIR.exists():
        return []
    runs = [p for p in RESULTS_DIR.iterdir() if p.is_dir()]
    runs.sort(key=lambda p: p.stat().st_mtime, reverse=True)
    return runs


def choose_run_id() -> Tuple[str, bool]:
    default_run_id = f"AirHockey_{dt.datetime.now().strftime('%Y%m%d_%H%M%S')}"
    existing_runs = list_existing_runs()

    if not existing_runs:
        run_id = ask_text("Run ID (novo)", default_run_id)
        return run_id, False

    options = ["Criar novo Run ID"]
    options.extend(f"{run.name} (existente)" for run in existing_runs)
    selected = ask_choice(
        "Escolha o Run ID (se escolher existente, usa --resume automaticamente)",
        options,
        default_index=0,
    )

    if selected == 0:
        while True:
            run_id = ask_text("Run ID (novo)", default_run_id).strip()
            if not run_id:
                print("Run ID nao pode ser vazio.")
                continue

            if (RESULTS_DIR / run_id).exists():
                print(f"Run ID '{run_id}' ja existe. Vou usar --resume automaticamente.")
                return run_id, True

            return run_id, False

    chosen_run = existing_runs[selected - 1].name
    print(f"Run ID existente selecionado: {chosen_run}. --resume sera ativado.")
    return chosen_run, True


def list_build_executables() -> List[Path]:
    if not BUILDS_DIR.exists():
        return []

    exes = []
    for exe in BUILDS_DIR.rglob("*.exe"):
        exe_name = exe.name.lower()
        if "unitycrashhandler" in exe_name:
            continue
        exes.append(exe)

    exes.sort(key=lambda p: str(p.relative_to(REPO_ROOT)).lower())
    return exes


def choose_environment_path() -> str:
    available_builds = list_build_executables()
    options = ["Treino no Editor (sem --env)"]
    options.extend(str(build.relative_to(REPO_ROOT)) for build in available_builds)
    options.append("Informar caminho manual")

    selected = ask_choice(
        "Escolha o ambiente de treino",
        options,
        default_index=0,
    )

    if selected == 0:
        return ""

    if selected == len(options) - 1:
        return ask_text("Caminho da build", "").strip()

    chosen_build = available_builds[selected - 1]
    chosen_relative = chosen_build.relative_to(REPO_ROOT)
    return str(chosen_relative).replace("\\", "/")


def select_trainer(label: str) -> str:
    idx = ask_choice(
        f"Escolha o trainer para {label}",
        [opt[0] for opt in TRAINER_OPTIONS],
        default_index=0,
    )
    chosen = TRAINER_OPTIONS[idx][1]
    mapped = TRAINER_ALIAS.get(chosen, chosen)
    if chosen != mapped:
        print(f"  Aviso: '{chosen.upper()}' nao e nativo no ML-Agents aqui. Usando '{mapped.upper()}'.")
    return mapped


def first_behavior_template(config: Dict) -> Tuple[str, Dict]:
    behaviors = config.get("behaviors") or {}
    if not behaviors:
        raise ValueError("YAML sem secao 'behaviors'.")
    first_name = next(iter(behaviors.keys()))
    template = copy.deepcopy(behaviors[first_name])
    return first_name, template


def apply_reward_overrides(config: Dict, overrides: Dict[str, Optional[float]]) -> None:
    env_params = config.get("environment_parameters")
    if not isinstance(env_params, dict):
        env_params = {}

    for key, value in overrides.items():
        if value is not None:
            env_params[key] = float(value)

    config["environment_parameters"] = env_params


def main() -> int:
    print("=== AirHockeyRL - Training Menu ===")

    configs = list_base_configs()
    if not configs:
        print(f"Nenhum YAML encontrado em {CONFIG_DIR.relative_to(REPO_ROOT)}.")
        return 1

    config_idx = ask_choice(
        "Escolha o YAML base",
        [str(p.relative_to(REPO_ROOT)) for p in configs],
        default_index=0,
    )
    base_yaml = configs[config_idx]
    config = load_yaml(base_yaml)

    mode_idx = ask_choice(
        "Modo de treino",
        [
            "Shared brain (um behavior para os dois agentes)",
            "Split brains (Blue vs Orange com trainers diferentes)",
        ],
        default_index=0,
    )
    split_mode = mode_idx == 1

    _, template = first_behavior_template(config)
    if split_mode:
        blue_trainer = select_trainer("BLUE")
        orange_trainer = select_trainer("ORANGE")
        blue_cfg = copy.deepcopy(template)
        orange_cfg = copy.deepcopy(template)
        blue_cfg["trainer_type"] = blue_trainer
        orange_cfg["trainer_type"] = orange_trainer
        config["behaviors"] = {
            "BlueBrain": blue_cfg,
            "OrangeBrain": orange_cfg,
        }
    else:
        shared_trainer = select_trainer("SHARED")
        shared_cfg = copy.deepcopy(template)
        shared_cfg["trainer_type"] = shared_trainer
        config["behaviors"] = {
            "AirHockeyBrain": shared_cfg,
        }

    train_vs_deterministic_ai = ask_yes_no(
        "Treinar contra IA deterministica (segue o puck)",
        False,
    )
    deterministic_ai_probability = 0.0
    if train_vs_deterministic_ai:
        probability_percent = ask_float_in_range(
            "Porcentagem de episodios contra IA deterministica (0-100)",
            50.0,
            0.0,
            100.0,
        )
        deterministic_ai_probability = probability_percent / 100.0

    reward_overrides = {
        # Read by RLGoalRewarder.cs
        "reward_goal_scored": ask_optional_float("Reward: gol marcado (+)", default=50.0),
        "reward_goal_conceded": ask_optional_float("Reward: gol sofrido (-)", default=-10.0),
        # Read by PusherAgent.cs
        "reward_step_alive": ask_optional_float("Reward: penalidade por passo", default=-0.001),
        "reward_puck_side": ask_optional_float("Reward: shaping lado do puck", default=0.00015),
        "reward_puck_touch": ask_optional_float("Reward: toque no puck", default=0.0005),
        "reward_timeout": ask_optional_float("Reward: timeout episodio", default=-5.0),
        # Read by ArenaManager.cs
        "split_behavior_names": 1.0 if split_mode else 0.0,
        "train_vs_deterministic_ai": 1.0 if train_vs_deterministic_ai else 0.0,
        "deterministic_ai_probability": deterministic_ai_probability,
    }

    grid_rows = ask_positive_odd_int("Quantidade de linhas (mesas no eixo Z)", 3)
    grid_columns = ask_positive_odd_int("Quantidade de colunas (mesas no eixo X)", 3)
    reward_overrides["grid_rows"] = float(grid_rows)
    reward_overrides["grid_columns"] = float(grid_columns)

    apply_reward_overrides(config, reward_overrides)

    run_id, resume = choose_run_id()
    time_scale = ask_text("Time scale", "20")
    no_graphics = ask_yes_no("Usar --no-graphics", True)
    env_path = choose_environment_path()

    generated_yaml = GENERATED_DIR / f"{run_id}.yaml"
    dump_yaml(generated_yaml, config)
    rel_yaml = generated_yaml.relative_to(REPO_ROOT)

    cmd = [
        sys.executable,
        "-m",
        "mlagents.trainers.learn",
        str(rel_yaml),
        "--run-id",
        run_id,
        "--time-scale",
        str(time_scale),
    ]
    if env_path.strip():
        cmd += ["--env", env_path.strip()]
    if no_graphics:
        cmd.append("--no-graphics")
    if resume:
        cmd.append("--resume")

    printable = " ".join(shlex.quote(c) for c in cmd)
    print("\nConfig gerada:", rel_yaml)
    if resume:
        print("Run existente detectado/selecionado: --resume ativo.")
    print("Comando:")
    print(printable)

    if ask_yes_no("Executar agora?", True):
        print("\nIniciando treino...\n")
        result = subprocess.run(cmd, cwd=REPO_ROOT)
        return result.returncode

    print("\nTreino nao iniciado. Copie e rode o comando acima quando quiser.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
