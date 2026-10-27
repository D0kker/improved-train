#!/usr/bin/env bash
set -euo pipefail

# Opt-in only. Run the same test source against separately built T03.5/T03.6
# checkouts; each scenario gets a new process/container and cannot reach a network.
# Usage: RUN_RESOURCE_LOAD=1 bash scripts/run-resource-load.sh SOURCE_ROOT LABEL OUTPUT_DIR
if [[ "${RUN_RESOURCE_LOAD:-}" != "1" ]]; then
  echo 'Refusing load test: set RUN_RESOURCE_LOAD=1 explicitly.' >&2
  exit 2
fi
source_root="$(realpath "${1:?source checkout required}")"
label="${2:?revision label required}"
output_root="$(realpath -m "${3:?output directory required}")"
if [[ "$output_root" != /tmp/* ]]; then
  echo 'Output must be a dedicated /tmp directory, never a production location.' >&2
  exit 2
fi
mkdir -p "$output_root"
nuget_root="${RESOURCE_LOAD_NUGET_ROOT:-/tmp/t03-nuget.1cuCHa}"
users_list="${RESOURCE_LOAD_USERS_LIST:-1 5 10 25 50}"
cache_list="${RESOURCE_LOAD_CACHE_LIST:-cold warm}"
identity_list="${RESOURCE_LOAD_IDENTITY_LIST:-same distinct}"
flow_list="${RESOURCE_LOAD_FLOW_LIST:-analysis}"
client_mode="${RESOURCE_LOAD_CLIENTS:-distinct}"

# Restore/build beforehand, outside the measurement. --no-build below ensures
# startup compilation is excluded and the image must already be available locally.
for flow in $flow_list; do
  for identities in $identity_list; do
    for cache in $cache_list; do
      for users in $users_list; do
        result_name="${label}-${flow}-${identities}-${client_mode}-${cache}-${users}.json"
        # This host may lack memory cgroups: GCHeapHardLimit bounds managed GC
        # memory only; the harness cancels if sampled RSS exceeds 384 MiB.
        # Neither mechanism is a hard native-RSS cap. Run scenarios serially.
        docker run --rm --pull never --network none --cpus 1 --memory 768m --pids-limit 256 \
          -e DOTNET_GCHeapHardLimit=0x10000000 -e RESOURCE_LOAD_RSS_GUARD_MIB=384 \
          -e RUN_RESOURCE_LOAD=1 -e "RESOURCE_LOAD_LABEL=$label" \
          -e "RESOURCE_LOAD_USERS=$users" -e "RESOURCE_LOAD_CACHE=$cache" \
          -e "RESOURCE_LOAD_IDENTITIES=$identities" -e "RESOURCE_LOAD_FLOW=$flow" \
          -e "RESOURCE_LOAD_CLIENTS=$client_mode" \
          -e "RESOURCE_LOAD_WAVES=${RESOURCE_LOAD_WAVES:-3}" \
          -e "RESOURCE_LOAD_DELAY_MS=${RESOURCE_LOAD_DELAY_MS:-5}" \
          -e "RESOURCE_LOAD_RESULT_PATH=/results/$result_name" \
          -v "$source_root:/src" -v "$nuget_root:/root/.nuget/packages" \
          -v "$output_root:/results" -w /src mcr.microsoft.com/dotnet/sdk:10.0 \
          dotnet test apps/api/tests/LolAnalyzer.IntegrationTests/LolAnalyzer.IntegrationTests.csproj \
          --configuration Release --no-build --no-restore \
          --filter FullyQualifiedName~ResourceLoadBenchmarkTests \
          --logger 'console;verbosity=normal'
      done
    done
  done
done
