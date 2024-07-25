#!/bin/bash
set -e

if [ -z "$GITHUB_ORG_URL" ]; then
  echo 1>&2 "error: missing GITHUB_ORG_URL environment variable"
  exit 1
fi

if [ -z "$GITHUB_ORG_TOKEN" ]; then
  echo 1>&2 "error: missing GITHUB_ORG_TOKEN environment variable"
  exit 1
fi

if [ -z "$GITHUB_RUNNER_NAME" ]; then
  echo 1>&2 "error: missing GITHUB_RUNNER_NAME environment variable"
  exit 1
fi

print_header() {
  lightcyan='\033[1;36m'
  nocolor='\033[0m'
  echo -e "${lightcyan}$1${nocolor}"
}

cleanup() {
  if [ -e config.sh ]; then
    print_header "Cleanup. Removing Actions Runner..."
    ./config.sh remove --token ${GITHUB_ORG_TOKEN}
  fi
}

print_header "Starting Actions Runner registration process..."

./config.sh --url ${GITHUB_ORG_URL} --token ${GITHUB_ORG_TOKEN} \
   --runnergroup ${GITHUB_RUNNER_GROUP} --work ${GITHUB_RUNNER_WORKDIR} \
   --name ${GITHUB_RUNNER_NAME} --labels ${GITHUB_RUNNER_LABELS} --unattended

trap 'cleanup; exit 0' EXIT
trap 'cleanup; exit 130' INT
trap 'cleanup; exit 143' TERM

chmod +x ./run.sh

# To be aware of TERM and INT signals call run.sh
# Running it with the --once flag at the end will shut down the runner after the build is executed
./run.sh "$@" & wait $!