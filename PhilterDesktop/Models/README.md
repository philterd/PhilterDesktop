# Bundled models

This folder holds the on-device model(s) Philter Desktop ships with. The model files are
**not committed to git** (they are large binaries); they are downloaded at build time and
bundled into the app output and the setup installer.

## ph-eye-pii-en-xsmall (PhEye name detection)

A GLiNER model (~90 MB) that detects **person names** entirely on-device — names are the one
PII type that genuinely needs a model, because they have no fixed format and depend on
context. The pattern-based filters (email, SSN, phone, …) handle the rest. Inference runs
in-process with no network call.

- Source: <https://huggingface.co/philterd/ph-eye-pii-en-xsmall> (the `onnx/` folder)
- Expected layout: `Models/ph-eye-pii-en-xsmall/{gliner_config.json, spm.model, model.onnx}`

### How it gets here

- **Release builds**: the `DownloadPhEyeModel` MSBuild target in
  `PhilterDesktop.csproj` downloads the files automatically if they are missing.
- **Debug builds / CI**: the download is skipped so builds stay fast and offline. The app
  detects the absence and simply disables name detection.
- **To test name detection in a Debug build**, populate this folder first:

  ```powershell
  pwsh scripts/download-pheye-model.ps1
  # or force it during build:
  dotnet build -p:DownloadPhEyeModel=true
  ```

Enable name detection per policy in the **Policy Editor** → *AI Detection* → *Names
(on-device AI)*.
