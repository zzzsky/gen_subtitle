# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GenSubtitle is a Windows WPF desktop application for generating bilingual (source language + Chinese) subtitles. It uses:
- **Whisper (whisper.cpp)** for speech transcription
- **Qwen API** (Alibaba DashScope) for translation to Chinese
- **FFmpeg** for audio extraction and subtitle burn-in/muxing

## Solution Structure

- **GenSubtitle.App** - WPF UI layer (ViewModels, Views, app-level services)
- **GenSubtitle.Core** - Core business logic and domain models (no WPF dependencies)
- **GenSubtitle.Tests** - xUnit tests

## Build & Run

```bash
# Build entire solution
dotnet build GenSubtitle.sln

# Run tests
dotnet test src/GenSubtitle.Tests/GenSubtitle.Tests.csproj

# Run the WPF app
dotnet run --project src/GenSubtitle.App/GenSubtitle.App.csproj
```

## Third-Party Dependencies (Not in Git)

The following external binaries must be placed under `src/GenSubtitle.App/`:

### FFmpeg
- `ffmpeg/x64/ffmpeg.exe`

### Whisper (whisper.cpp)
- `whisper/x64/whisper-cli.exe`
- `whisper/x64/whisper.dll`, `ggml.dll`, `ggml-base.dll`, `ggml-cpu.dll`
- `whisper/x64/ggml-cuda.dll`, `cublas64_12.dll`, `cublaslt64_12.dll`, `cudart64_12.dll` (GPU build)
- `whisper/models/ggml-small.bin`, `ggml-medium.bin`, `ggml-large-v3.bin`

## Architecture Notes

### Pipeline Flow
`JobPipelineService.RunAsync()` orchestrates the full workflow:
1. Extract audio via FFmpeg (cached: skip if WAV exists)
2. Transcribe via Whisper (cached: skip if `{base}_vtt_{model}_{lang}.srt` exists)
3. Translate via Qwen (cached: skip if `{base}_zh_{model}_{lang}.srt` exists)
4. Write bilingual SRT and ASS files

### MVVM Implementation
- Custom `ObservableObject` base class (not CommunityToolkit.Mvvm)
- Custom `RelayCommand` implementation
- ViewModels in `src/GenSubtitle.App/ViewModels/`
- Views (XAML) in `src/GenSubtitle.App/Views/`

### Task Queue System
- `TaskQueueViewModel` manages concurrent jobs with `SemaphoreSlim`
- Tasks progress through states: Queued → Transcribing → Translating → Completed
- Task state is cached to disk via `TaskCacheService` and restored on app restart
- Output directory: `C:\Users\Admin\Documents\GenSubtitle\<VideoName>\`

### Cache File Naming Convention
Cache files follow pattern: `{baseName}_{type}_{modelKey}_{language}.srt`
- Type: `vtt` (transcription), `zh` (translation)
- Model key: normalized from Whisper model name (e.g., `large_v3` for `large-v3`)

### Translation Service
- `ITranslationService` interface for abstraction
- `QwenTranslationService` implements using Alibaba DashScope API
- `TranslationPipeline` batches translation requests (20 segments per batch)

### Settings
- `AppSettings` contains user configuration (API keys, model paths, GPU toggle)
- `SettingsService` persists settings to JSON file

## UI Interactions

- **Subtitle Segments**: Users can align start/end times to current video playback position via `MainViewModel.AlignSelectedStart()` / `AlignSelectedEnd()`
- **Export**: Supports soft-mux (subtitle track) and burn-in (subtitles rendered into video)
- **Delete**: `ConfirmDeleteWindow` allows users to optionally delete output folder when removing tasks
