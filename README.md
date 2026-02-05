# GenSubtitle

Windows WPF app for generating bilingual (source + Chinese) subtitles with Whisper + Qwen.

## Prerequisites
- Windows 10/11
- .NET 7 SDK
- Qwen API key (qwen-plus)

## Third-Party Dependencies (not committed)
Place the following files under `src/GenSubtitle.App`:

### FFmpeg
Path: `src/GenSubtitle.App/ffmpeg/x64/ffmpeg.exe`

### Whisper (whisper.cpp)
Path: `src/GenSubtitle.App/whisper/`
- `x64/whisper-cli.exe`
- `x64/whisper.dll`
- `x64/ggml.dll`
- `x64/ggml-base.dll`
- `x64/ggml-cpu.dll`
- `x64/ggml-cuda.dll` (if using GPU build)
- `x64/cublas64_12.dll`, `x64/cublaslt64_12.dll`, `x64/cudart64_12.dll` (if using GPU build)
- `models/ggml-small.bin`
- `models/ggml-medium.bin`
- `models/ggml-large-v3.bin`

## Usage
1. Launch the app.
2. Open **Settings** and set Whisper model, GPU, Qwen key.
3. Import a video.
4. Transcription and translation run in the task queue.
6. Edit subtitle rows and align start/end to current playback position.
7. Right-click a completed task and export (soft or burn-in).

## Notes
- The app writes outputs to `C:\Users\Admin\Documents\GenSubtitle\<VideoName>\`.
- Whisper and FFmpeg are required at the paths above and are excluded from git.
