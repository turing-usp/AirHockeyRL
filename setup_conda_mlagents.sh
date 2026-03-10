#!/usr/bin/env bash
set -euo pipefail

ENV_NAME="${1:-ml_agents}"
PYTHON_VERSION="${2:-3.10.12}"
FORCE_RECREATE="${FORCE_RECREATE:-0}"
USE_ACTIVE_ENV="${USE_ACTIVE_ENV:-0}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REQUIREMENTS_FILE="${SCRIPT_DIR}/requirements-mlagents.txt"

if ! command -v conda >/dev/null 2>&1; then
  echo "Conda nao encontrado no PATH. Abra um terminal com Anaconda/Miniconda." >&2
  exit 1
fi

if [[ ! -f "${REQUIREMENTS_FILE}" ]]; then
  echo "Arquivo nao encontrado: ${REQUIREMENTS_FILE}" >&2
  exit 1
fi

env_exists() {
  conda env list | awk '{print $1}' | grep -qx "${ENV_NAME}"
}

run_in_target_env() {
  if [[ "${USE_ACTIVE_ENV}" == "1" ]]; then
    "$@"
  else
    conda run -n "${ENV_NAME}" "$@"
  fi
}

echo ">> Preparando ambiente conda '${ENV_NAME}' (Python ${PYTHON_VERSION})..."

if [[ "${USE_ACTIVE_ENV}" == "1" ]]; then
  ACTIVE_ENV="${CONDA_DEFAULT_ENV:-}"
  if [[ -z "${ACTIVE_ENV}" ]]; then
    echo "Nenhum ambiente conda ativo. Rode 'conda activate <nome-do-env>' ou USE_ACTIVE_ENV=0." >&2
    exit 1
  fi

  if [[ "${ACTIVE_ENV}" != "${ENV_NAME}" ]]; then
    echo ">> Aviso: ambiente ativo '${ACTIVE_ENV}' diferente de '${ENV_NAME}'. Usando '${ACTIVE_ENV}'."
    ENV_NAME="${ACTIVE_ENV}"
  fi

  PY_VER_RAW="$(python -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}')")"
  echo ">> Ambiente ativo detectado: '${ENV_NAME}' (Python ${PY_VER_RAW})"

  python -c "import sys; v=sys.version_info[:3]; raise SystemExit(0 if (3,10,1) <= v <= (3,10,12) else 1)" || {
    echo "Python ${PY_VER_RAW} nao e compativel com mlagents==1.1.0." >&2
    echo "Rode: conda install -n ${ENV_NAME} python=3.10.12 -y" >&2
    exit 1
  }
else
  if [[ "${FORCE_RECREATE}" == "1" ]] && env_exists; then
    echo ">> Removendo ambiente existente '${ENV_NAME}'..."
    conda remove -y -n "${ENV_NAME}" --all >/dev/null
  fi

  if ! env_exists; then
    echo ">> Criando ambiente '${ENV_NAME}'..."
    conda create -y -n "${ENV_NAME}" "python=${PYTHON_VERSION}" >/dev/null
  else
    echo ">> Ambiente '${ENV_NAME}' ja existe. Reutilizando..."
  fi

  # Garante versao de Python compativel para mlagents==1.1.0.
  conda install -y -n "${ENV_NAME}" "python=${PYTHON_VERSION}" >/dev/null
fi

echo ">> Atualizando ferramentas base de pip..."
# mlagents==1.1.0 depende de pkg_resources (setuptools recente removeu).
run_in_target_env python -m pip install --upgrade pip wheel
run_in_target_env python -m pip install "setuptools<81"

echo ">> Instalando dependencias do ML-Agents..."
run_in_target_env python -m pip install -r "${REQUIREMENTS_FILE}"

echo ">> Validando instalacao..."
run_in_target_env python -c "import mlagents, mlagents_envs; print('mlagents:', mlagents.__version__); print('mlagents_envs:', mlagents_envs.__version__)"
run_in_target_env python -m mlagents.trainers.learn --help >/dev/null

echo
echo "Instalacao concluida."
if [[ "${USE_ACTIVE_ENV}" != "1" ]]; then
  echo "Para usar o ambiente:"
  echo "  conda activate ${ENV_NAME}"
fi
echo "Exemplo de treino:"
echo "  python -m mlagents.trainers.learn Assets/ML-Agents/Configs/air_hockey.yaml --run-id AirHockey --time-scale 20"
