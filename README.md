# DarkHub

[Leia o README em Português](README.pt.md)

DarkHub is a powerful and versatile WPF application designed to maximize your PC's performance and simplify everyday tasks. Combining optimization tools,
automation and media handling tools, DarkHub offers a complete solution for users and developers.

## Features
- **PC Optimizer**: Improve system performance by cleaning up unnecessary files, deactivating heavy processes and activating Windows for maximum efficiency.
- **Auto Clicker**: Automate repetitive clicks with configurable intervals, ideal for games or routine tasks.
- **File Converter**: Easily convert text, image, audio and video files into popular formats.
- **Metadata Editor**: Organize your media collections by editing titles, artists, albums and more directly in the files.
- **Image Text Extractor**: Use OCR to extract text from images, perfect for scanning documents or screenshots.
- **YouTube Video Downloader**: Download YouTube videos or playlists in MP4 format with H.264 and AAC support.
- **YouTube Video Transcription**: Extract and copy transcripts from YouTube Videos.
- **Text Editor with Python Interpreter**: Write, edit and execute Python scripts in real time, with integrated debugging support.
- **Resource Monitor**: Real-time information and metrics on hardware and software. Plus a Benchmark for performance evaluation.
- **Text Summarizer**:  Allows you to summarize your giant texts and PDFs into a size and parameters defined by you.
- **Password Manager**: This password manager provides a secure and user-friendly way to store, manage, and retrieve passwords.
- **Advanced Security**: Helps the user identify malicious applications and websites.

## Prerequisites
- Windows 10 or higher.
- [ASP.NET Core 8.0 Runtime](https://dotnet.microsoft.com/pt-br/download/dotnet/thank-you/runtime-aspnetcore-8.0.14-windows-x64-installer) or higher.
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework) or higher.

## Prerequisites for using the youtube downloader with the source code `Archives already included in the release` (place both in the `assets` folder):
- [ffmpeg.exe](https://www.gyan.dev/ffmpeg/builds/#release-builds)
- [yt-dlp.exe](https://github.com/yt-dlp/yt-dlp/releases/)

## Restoring local files
Large binaries, third-party tools, and secrets stay outside Git. After cloning, rebuild the local asset folder with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Restore-LocalAssets.ps1
```

The script downloads `yt-dlp.exe` and attempts to restore `ffmpeg.exe`. For `CPU-Z.exe`, `GPU-Z.exe`, `HWiNFO64.exe`, `DDU.exe`, and `assets\settings`, pass a backup or extracted release folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Restore-LocalAssets.ps1 -ThirdPartyToolsDirectory "C:\path\to\backup"
```

Certificates, passwords, and `.pfx` files should not be committed or copied into `assets`.

## Build and signing
To create an unsigned local release:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1 -SkipSigning
```

To sign `DarkHub.exe`, install the Windows SDK for `signtool.exe` and provide your own certificate through environment variables:

```powershell
$env:DARKHUB_SIGN_CERT_PATH = "C:\certs\DarkHub-release.pfx"
$env:DARKHUB_SIGN_CERT_PASSWORD = "certificate-password"
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1
```

You can also sign with a certificate already installed in the Windows certificate store:

```powershell
$env:DARKHUB_SIGN_CERT_THUMBPRINT = "CERTIFICATE_THUMBPRINT"
powershell -ExecutionPolicy Bypass -File .\scripts\Build-SignedRelease.ps1
```

## Contribution
Contributions are welcome! Follow these steps:
1. Fork the repository.
2. Create a branch for your feature (`git checkout -b feature/new-feature`).
3. Commit your changes (`git commit -m “Add new feature”`).
4. Send it to the remote repository (`git push origin feature/new-feature`).
5. Open a Pull Request.

## License
This project is licensed under the [MIT License](LICENSE).


## Contact
- Author: Pokeralho
- Email: luisfernandobtu80@gmail.com
- GitHub: [Pokeralho](https://github.com/Pokeralho)


## Credits
- Thanks to the:
- [SpaceSniffer](https://github.com/redtrillix/SpaceSniffer) repository for providing the executable used as part of DarkHub's optimization features.
- [CPU-Z](https://www.cpuid.com/softwares/cpu-z.html) Tool for monitoring and providing detailed information about the CPU and other system components.
- [GPU-Z](https://www.techpowerup.com/gpuz/) Utility for detailed information on video cards.
- [HWiNFO](https://www.hwinfo.com/) Advanced hardware diagnostics and monitoring software.
- [DDU (Display Driver Uninstaller)](https://www.wagnardsoft.com/display-driver-uninstaller-ddu-) Tool for complete removal of video drivers.
