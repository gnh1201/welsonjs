# DownloadUrls.psd1
# External download urls for WelsonJS post-install script
# Namhyeon Go <gnh1201@catswords.re.kr>
# Last updated: 2026-08-06
# https://github.com/gnh1201/welsonjs
# 
@{
    # ===========================
    # Python embeddable
    # ===========================
    python = @{
        x64   = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-amd64.zip"
        arm64 = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-arm64.zip"
        x86   = "https://www.python.org/ftp/python/3.14.7/python-3.14.7-embed-amd64.zip"
    }

    # ===========================
    # cURL
    # ===========================
    curl = @{
        x64   = "https://curl.se/windows/latest.cgi?p=win64-mingw.zip"
        arm64 = "https://curl.se/windows/latest.cgi?p=win64a-mingw.zip"
        x86   = "https://twds.dl.sourceforge.net/project/muldersoft/cURL/curl-8.17.0-win-x86-full.2026-05-02.zip?viasf=1"
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
        arm64 = $null  # no official ARM64 build
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
    android_platform_tools = @{
        x64   = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"
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
    # Microsoft OpenJDK build
    # ===========================
    microsoft_jdk = @{
        x64 = "https://aka.ms/download-jdk/microsoft-jdk-25.0.4-windows-x64.exe"
        arm64 = $null  # no official ARM64 build
        x86   = "https://aka.ms/download-jdk/microsoft-jdk-25.0.4-windows-aarch64.exe"
    }
}
