$ErrorActionPreference = "Stop"

$buildScript = Join-Path $PSScriptRoot "build-simulator-ui.mjs"
& node $buildScript

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
