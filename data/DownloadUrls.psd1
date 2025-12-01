@{
    # ===========================
    # Python embeddable
    # ===========================
    python = @{
        x64   = "https://www.python.org/ftp/python/3.14.0/python-3.14.0-embed-amd64.zip"
        arm64 = "https://www.python.org/ftp/python/3.14.0/python-3.14.0-embed-arm64.zip"
        x86   = "https://www.python.org/ftp/python/3.13.9/python-3.13.9-embed-win32.zip"
    }

    # ===========================
    # cURL
    # ===========================
    curl = @{
        x64   = "https://curl.se/windows/latest.cgi?p=win64-mingw.zip"
        arm64 = "https://curl.se/windows/latest.cgi?p=win64a-mingw.zip"
        x86   = "https://downloads.sourceforge.net/project/muldersoft/cURL/curl-8.17.0-win-x86-full.2025-11-09.zip"
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
        x64   = "https://github.com/bytecodealliance/wasm-micro-runtime/releases/download/WAMR-2.4.3/iwasm-2.4.3-x86_64-windows-2022.tar.gz"
        arm64 = $null  # not supported
        x86   = $null  # not supported
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
		x86   = "https://downloads.sourceforge.net/project/gtk-win/files/GTK%2B%20Runtime%20Environment/GTK%2B%202.24/gtk2-runtime-2.24.10-2012-10-10-ash.exe/download"
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
        x64 = "https://npcap.com/dist/npcap-1.85.exe"
        arm64 = $null  # no official ARM64 build
        x86 = "https://npcap.com/dist/npcap-1.85.exe"
    }

    nmap = @{
        x64 = "https://nmap.org/dist/nmap-7.98-setup.exe"
        arm64 = $null  # no official ARM64 build
        x86 = "https://nmap.org/dist/nmap-7.98-setup.exe"
    }
}
