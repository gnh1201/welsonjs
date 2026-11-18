# DownloadUrls.psd1
# External download urls for WelsonJS post-install script
# The SHA-256 of every file listed here is in data/DownloadHashes.txt; postInstall.ps1
# downloads nothing that is not on that list.
# Namhyeon Go <gnh1201@catswords.re.kr>
# Last updated: 2026-11-18
# https://github.com/gnh1201/welsonjs
# 
@{
    # ===========================
    # Python embeddable
    # ===========================
    python = @{
        x64   = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-amd64.zip"
        arm64 = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-arm64.zip"
        x86   = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-win32.zip"
    }

    # ===========================
    # cURL
    # ===========================
    # x64 and ARM64: curl 8.23.0, build 8.23.0_9 of curl-for-win, the build the
    # download page links to (https://curl.se/windows/). Pinned to the versioned
    # file instead of its latest.cgi redirect, so the SHA-256 in
    # data/DownloadHashes.txt belongs to the exact file this table names.
    # x86: the WelsonJS build (catswords blob), unchanged.
    # The archive holds one top folder (curl-8.23.0_9-win64-mingw\, or
    # curl-8.23.0_9-win64a-mingw\ for ARM64), which postInstall.ps1 unwraps, so
    # curl sits at curl\bin\curl.exe.
    curl = @{
        x64   = "https://curl.se/windows/dl-8.23.0_9/curl-8.23.0_9-win64-mingw.zip"
        arm64 = "https://curl.se/windows/dl-8.23.0_9/curl-8.23.0_9-win64a-mingw.zip"
        x86   = "https://catswords.blob.core.windows.net/welsonjs/packages/native/curl/8.20.0-x86/curl-8.20.0-win-x86-full.2026-05-02.zip"
    }

    # Llama.cpp is not downloaded: postInstall.ps1 installs the WelsonJS build
    # from the artifacts package (artifacts\llamacpp\<arch>, native\llamacpp).

    # ===========================
    # Hy-MT2 1.8B 1.25-bit GGUF model (Tencent AngelSlim, STQ1_0; needs the
    # WelsonJS llama.cpp build, see native\llamacpp)
    # ===========================
    hy_mt2_1_8b_1_25bit = @{
        any   = "https://huggingface.co/tencent/Hy-MT2-1.8B-1.25Bit-GGUF/resolve/main/Hy-MT2-1.8B-1.25Bit.gguf"
    }

    # ===========================
    # FrogNano 4B 2609 Q4_K_M GGUF model
    # ===========================
    frognano_4b_2609 = @{
        any   = "https://huggingface.co/gnh1201/FrogNano-4B-2609-Q4_K_M-GGUF/resolve/main/frognano-4b-2609-q4_k_m.gguf"
    }

    # ===========================
    # BitNet b1.58 2B-4T GGUF model (Microsoft, 1.58-bit I2_S; needs the WelsonJS
    # llama.cpp build, see native\llamacpp)
    # ===========================
    bitnet_b1_58_2b_4t = @{
        any   = "https://huggingface.co/microsoft/bitnet-b1.58-2B-4T-gguf/resolve/main/ggml-model-i2_s.gguf"
    }

    # ===========================
    # Qwen3Guard local safety model
    # ===========================
    qwen3guard_06b = @{
        any   = "https://huggingface.co/mradermacher/Qwen3Guard-Gen-0.6B-GGUF/resolve/main/Qwen3Guard-Gen-0.6B.Q4_K_M.gguf"
    }

    # ===========================
    # YARA
    # ===========================
    yara = @{
        x64   = "https://github.com/VirusTotal/yara/releases/download/v4.5.5/yara-4.5.5-2368-win64.zip"
        arm64 = $null  # no official ARM64 build
        x86   = "https://github.com/VirusTotal/yara/releases/download/v4.5.5/yara-4.5.5-2368-win32.zip"
    }

    # ===========================
    # WAMR (WebAssembly Micro Runtime)
    # ===========================
    wamr = @{
        x64   = "https://github.com/wasm-micro-runtime/wasm-micro-runtime/releases/download/WAMR-2.4.5/wamrc-2.4.5-x86_64-windows-2022.tar.gz"
        arm64 = $null  # no official ARM64 build
        x86   = $null  # no official X86 build
    }

    # ===========================
    # websocat
    # ===========================
    websocat = @{
        x64   = "https://catswords.blob.core.windows.net/welsonjs/websocat-1.14.0.x86_64-pc-windows-gnu.zip"
        arm64 = $null  # no official ARM64 build
        x86   = "https://catswords.blob.core.windows.net/welsonjs/websocat-1.14.0.i686-pc-windows-gnu.zip"
    }

    # ===========================
    # WelsonJS artifacts (launcher, service, etc.)
    # ===========================
    artifacts = @{
        x64   = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"
        arm64 = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"  # x86 WelsonJS binaries (x86 emulation) and the arm64 llama.cpp build
        x86   = "https://catswords.blob.core.windows.net/welsonjs/artifacts.zip"
    }

    # ===========================
    # GTK3 runtime (x86 uses GTK2)
    # ===========================
    gtk3runtime = @{
        x64   = "https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases/download/2022-01-04/gtk3-runtime-3.24.31-2022-01-04-ts-win64.exe"
        arm64 = $null  # no official ARM64 build
        x86   = "https://twds.dl.sourceforge.net/project/gtk-win/GTK%2B%20Runtime%20Environment/GTK%2B%202.24/gtk2-runtime-2.24.10-2012-10-10-ash.exe?viasf=1"
    }
    
    # ===========================
    # GTK server
    # ===========================
    gtkserver = @{
        x64   = "https://github.com/jopbrown/gtk-server/releases/download/v2.4.5/gtk-server-2.4.5-gtk-2.24.32-win64.zip"
        arm64 = $null  # no official ARM64 build
        x86   = $null  # no official X86 build
    }

    # ===========================
    # Tesseract trained data
    # ===========================
    tessdata = @{
        any = "https://github.com/tesseract-ocr/tessdata/archive/refs/tags/4.1.0.zip"
    }

    tessdata_best = @{
        any = "https://github.com/tesseract-ocr/tessdata_best/archive/refs/tags/4.1.0.zip"
    }

    tessdata_fast = @{
        any = "https://github.com/tesseract-ocr/tessdata_fast/archive/refs/tags/4.1.0.zip"
    }

    # ===========================
    # Npcap + Nmap
    # ===========================
    npcap = @{
        x64   = "https://npcap.com/dist/npcap-1.88.exe"
        arm64 = $null  # no official ARM64 build
        x86   = "https://npcap.com/dist/npcap-1.88.exe"
    }

    nmap = @{
        x64   = "https://nmap.org/dist/nmap-7.99-setup.exe"
        arm64 = $null  # no official ARM64 build
        x86   = "https://nmap.org/dist/nmap-7.99-setup.exe"
    }

    # ===========================
    # WinDivert
    # ===========================
    windivert = @{
        x64   = "https://github.com/basil00/WinDivert/releases/download/v2.2.2/WinDivert-2.2.2-A.zip"
        arm64 = $null  # no official ARM64 build
        x86   = "https://github.com/basil00/WinDivert/releases/download/v2.2.2/WinDivert-2.2.2-A.zip"
    }

    # ===========================
    # Android Platform Tools (e.g., ADB)
    # ===========================
    # platform-tools r37.0.1 for Windows, the release in the stable channel
    # (channel-0, android-sdk-license) of
    # https://dl.google.com/android/repository/repository2-3.xml, as the versioned
    # archive instead of platform-tools-latest-windows.zip. r37.0.3 is only in the
    # preview channel (channel-2, android-sdk-preview-license). The archive holds
    # one platform-tools\ folder, which postInstall.ps1 unwraps, so adb.exe lands at
    # android_platform_tools\adb.exe where lib/adb.js looks for it.
    android_platform_tools = @{
        x64   = "https://dl.google.com/android/repository/platform-tools_r37.0.1-win.zip"
        arm64 = $null  # no official ARM64 build
        x86   = $null  # no official X86 build
    }
    
    # ===========================
    # tun2socks
    # ===========================
    tun2socks = @{
        x64v3 = "https://github.com/xjasonlyu/tun2socks/releases/download/v2.7.0/tun2socks-windows-amd64-v3.zip"
        x64   = "https://github.com/xjasonlyu/tun2socks/releases/download/v2.7.0/tun2socks-windows-amd64.zip"
        arm64 = "https://github.com/xjasonlyu/tun2socks/releases/download/v2.7.0/tun2socks-windows-arm64.zip"
        arm32 = "https://github.com/xjasonlyu/tun2socks/releases/download/v2.7.0/tun2socks-windows-arm32v7.zip"
        x86   = "https://github.com/xjasonlyu/tun2socks/releases/download/v2.7.0/tun2socks-windows-386.zip"
    }
    
    # ===========================
    # sendboxie
    # ===========================
    sendboxie = @{
        x64   = "https://github.com/sandboxie-plus/Sandboxie/releases/download/v1.18.1/Sandboxie-Plus-x64-v1.18.1.exe"
        arm64 = "https://github.com/sandboxie-plus/Sandboxie/releases/download/v1.18.1/Sandboxie-Plus-ARM64-v1.18.1.exe"
        x86   = $null  # no official X86 build
    }

    # ===========================
    # ldplayer (Android Emulator)
    # ===========================
    ldplayer = @{
        any = "https://res.ldrescdn.com/download/LDPlayer9.exe?n=LDPlayer9_kr_42479921_ld.exe"
    }
    
    # ===========================
    # tap_windows6 (Windows TAP driver (NDIS 6))
    # ===========================
    tap_windows6 = @{
        x64   = "https://github.com/OpenVPN/tap-windows6/releases/download/9.27.0/tap-windows-9.27.0-I0-amd64.msm"
        arm64 = "https://github.com/OpenVPN/tap-windows6/releases/download/9.27.0/tap-windows-9.27.0-I0-arm64.msm"
        x86   = "https://github.com/OpenVPN/tap-windows6/releases/download/9.27.0/tap-windows-9.27.0-I0-i386.msm"
    }
    
    # ===========================
    # THC Hydra Windows build
    # ===========================
    thc_hydra = @{
        any = "https://github.com/maaaaz/thc-hydra-windows/releases/download/v9.1/thc-hydra-windows-v9.1.zip"
    }
    
    # ===========================
    # Shadowsocks
    # ===========================
    shadowsocks_libev = @{
        any = "https://catswords.blob.core.windows.net/welsonjs/shadowsocks-libev-win-build-2022.01.18.zip"
    }
    
    # ===========================
    # WinLibs standalone build of GCC and MinGW-w64 for Windows
    # ===========================
    winlibs_mingw = @{
        x64   = "https://github.com/brechtsanders/winlibs_mingw/releases/download/16.1.0posix-14.0.0-msvcrt-r4/winlibs-x86_64-posix-seh-gcc-16.1.0-mingw-w64msvcrt-14.0.0-r4.zip"
        arm64 = $null  # no official ARM64 build
        x86   = "https://github.com/brechtsanders/winlibs_mingw/releases/download/16.1.0posix-14.0.0-msvcrt-r4/winlibs-i686-posix-dwarf-gcc-16.1.0-mingw-w64msvcrt-14.0.0-r4.zip"
    }
    
    # ===========================
    # The Go Language Programming
    # ===========================
    golang = @{
        x64   = "https://go.dev/dl/go1.26.5.windows-amd64.zip"
        arm64 = "https://go.dev/dl/go1.26.5.windows-arm64.zip"
        x86   = "https://go.dev/dl/go1.26.5.windows-386.zip"
    }
    
    # ===========================
    # x86dbg
    # ===========================
    x86dbg = @{
        any = "https://twds.dl.sourceforge.net/project/x64dbg/snapshots/snapshot_2026-05-27_12-11.zip?viasf=1"
    }
    
    # ===========================
    # 7-zip
    # ===========================
    w7zip = @{
        x64   = "https://github.com/ip7z/7zip/releases/download/26.02/7z2602-x64.exe"
        arm64 = "https://github.com/ip7z/7zip/releases/download/26.02/7z2602-arm64.exe"
        x86   = "https://github.com/ip7z/7zip/releases/download/26.02/7z2602.exe"
    }
    
    # ===========================
    # hashcat
    # ===========================
    hashcat = @{
        any = "https://hashcat.net/files/hashcat-7.1.2.7z"
    }
    
    # ===========================
    # OpenJDK build
    # ===========================
    # x64 and ARM64: Microsoft Build of OpenJDK 25 LTS, at the current Windows patch
    # level (25.0.5.0.2, the build carrying the October 2026 CPU fixes). Microsoft
    # publishes Windows builds for these two architectures only: the 32-bit x86 port
    # was removed with JDK 25 (JEP 503).
    #   https://learn.microsoft.com/en-us/java/openjdk/download
    # x86: Azul Zulu 17 LTS (17.0.21.1) for Windows i686, an OpenJDK build that still
    # ships 32-bit Windows builds. It is at the patch level the JDK 17 fixes in the
    # November 2026 CPU land in.
    # All three are the vendor's .zip package rather than its installer:
    # postInstall.ps1 unpacks it under %APPDATA%\welsonjs\jdk instead of
    # installing it, so nothing is left in Program Files and no Programs and Features
    # entry is created; the JDK goes away with that directory, as the other
    # components the script downloads do. Each .zip holds one
    # top folder named after the build, and postInstall.ps1 keeps that folder, so the
    # JDK sits at jdk\jdk-25.0.5.0.2+1\ (or jdk\zulu17...-win_i686\), and what an
    # earlier version of the component left in %APPDATA%\welsonjs\microsoft_jdk is
    # removed once the new JDK is registered.
    # Both vendors serve the file behind a redirect that keeps the file name.
    microsoft_jdk = @{
        x64   = "https://aka.ms/download-jdk/microsoft-jdk-25.0.5.0.2-windows-x64.zip"
        arm64 = "https://aka.ms/download-jdk/microsoft-jdk-25.0.5.0.2-windows-aarch64.zip"
        x86   = "https://cdn.azul.com/zulu/bin/zulu17.70.201-ca-jdk17.0.21.1-win_i686.zip"
    }
    
    # ===========================
    # HWP Automation
    # ===========================
    hwp_automation = @{
        any = "https://catswords.blob.core.windows.net/welsonjs/packages/native/FilePathCheckerModuleExample/2007.01.09/FilePathCheckerModuleExample.dll.gz"
    }
    
    # ===========================
    # Nuget
    # ===========================
    # NuGet 8.0.0, the build dist.nuget.org/win-x86-commandline/latest/nuget.exe
    # serves, named by version; it used to be that latest link. postInstall.ps1
    # keeps nuget\nuget.exe and puts that folder on the user PATH.
    nuget = @{
        any = "https://dist.nuget.org/win-x86-commandline/v8.0.0/nuget.exe"
    }
    
    # ===========================
    # ripgrep
    # ===========================
    ripgrep = @{
        x64   = "https://github.com/BurntSushi/ripgrep/releases/download/15.2.0/ripgrep-15.2.0-x86_64-pc-windows-msvc.zip"
        arm64 = "https://github.com/BurntSushi/ripgrep/releases/download/15.2.0/ripgrep-15.2.0-aarch64-pc-windows-msvc.zip"
        x86   = "https://github.com/BurntSushi/ripgrep/releases/download/15.2.0/ripgrep-15.2.0-i686-pc-windows-msvc.zip"
    }
}
