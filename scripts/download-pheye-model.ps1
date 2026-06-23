# Copyright 2026 Philterd, LLC
# Licensed under the Apache License, Version 2.0.
#
# Downloads the bundled on-device PhEye name-detection model (GLiNER xsmall) into
# PhilterDesktop\Models\ph-eye-pii-en-xsmall. The Release build does this automatically;
# run this script to populate the model for a Debug build or to test name detection.

$ErrorActionPreference = 'Stop'

$modelName = 'ph-eye-pii-en-xsmall'
$repoRoot  = Split-Path -Parent $PSScriptRoot
$modelDir  = Join-Path $repoRoot "PhilterDesktop\Models\$modelName"
$baseUrl   = "https://huggingface.co/philterd/$modelName/resolve/main/onnx"
$files     = @('gliner_config.json', 'spm.model', 'model.onnx')

New-Item -ItemType Directory -Force $modelDir | Out-Null

foreach ($f in $files) {
    $dest = Join-Path $modelDir $f
    if (Test-Path $dest) {
        Write-Host "exists: $f"
        continue
    }
    Write-Host "downloading $f ..."
    Invoke-WebRequest -Uri "$baseUrl/$f" -OutFile $dest -UseBasicParsing
}

Write-Host "Model ready at: $modelDir"
Get-ChildItem $modelDir | Select-Object Name, Length
