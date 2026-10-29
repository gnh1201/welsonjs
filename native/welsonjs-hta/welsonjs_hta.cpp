#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commdlg.h>
#include <shellapi.h>
#include <shlwapi.h>

#include <algorithm>
#include <cwctype>
#include <filesystem>
#include <memory>
#include <stdexcept>
#include <string>
#include <vector>

#include "webview.hpp"

namespace {

std::filesystem::path chooseDocument() {
    std::vector<wchar_t> path(32768, L'\0');
    OPENFILENAMEW dialog{};
    dialog.lStructSize = sizeof(dialog);
    dialog.lpstrFilter = L"Web documents\0*.hta;*.html;*.htm\0All files\0*.*\0\0";
    dialog.lpstrFile = path.data();
    dialog.nMaxFile = static_cast<DWORD>(path.size());
    dialog.Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR;
    dialog.lpstrTitle = L"Open a local web document";
    if (!GetOpenFileNameW(&dialog)) return {};
    return std::filesystem::absolute(path.data());
}

std::filesystem::path documentFromCommandLine() {
    int count = 0;
    LPWSTR* arguments = CommandLineToArgvW(GetCommandLineW(), &count);
    if (!arguments) throw std::runtime_error("Could not read command line.");

    std::filesystem::path document;
    for (int i = 1; i < count; ++i) {
        if (wcscmp(arguments[i], L"--chakra-console") == 0 ||
            wcscmp(arguments[i], L"--no-chakra") == 0 ||
            wcscmp(arguments[i], L"--chakra") == 0) {
            continue;
        }
        if (wcscmp(arguments[i], L"--chakra-script") == 0) {
            ++i;
            continue;
        }
        if (arguments[i][0] != L'-') {
            document = std::filesystem::absolute(arguments[i]);
            break;
        }
    }
    LocalFree(arguments);
    if (document.empty()) document = chooseDocument();
    if (document.empty()) return {};
    if (!std::filesystem::is_regular_file(document))
        throw std::runtime_error("The selected document does not exist.");

    std::wstring extension = document.extension().wstring();
    std::transform(extension.begin(), extension.end(), extension.begin(),
                   [](wchar_t ch) { return static_cast<wchar_t>(towlower(ch)); });
    if (extension != L".hta" && extension != L".html" && extension != L".htm")
        throw std::runtime_error("Choose an .hta, .html, or .htm document.");
    return document;
}

class HtaSnapshot {
public:
    explicit HtaSnapshot(const std::filesystem::path& source) {
        wchar_t temporary[MAX_PATH]{};
        const auto directory = source.parent_path();
        if (GetTempFileNameW(directory.c_str(), L"wva", 0, temporary) == 0)
            throw std::runtime_error("Could not create a temporary HTML path.");
        path_ = temporary;
        path_.replace_extension(L".tmp.html");
        if (!MoveFileW(temporary, path_.c_str())) {
            DeleteFileW(temporary);
            throw std::runtime_error("Could not prepare the HTA for MSHTML.");
        }
        if (!CopyFileW(source.c_str(), path_.c_str(), FALSE)) {
            DeleteFileW(path_.c_str());
            throw std::runtime_error("Could not copy the HTA for MSHTML.");
        }
    }

    ~HtaSnapshot() { DeleteFileW(path_.c_str()); }
    const std::filesystem::path& path() const { return path_; }

private:
    std::filesystem::path path_;
};

std::wstring fileUrl(const std::filesystem::path& document) {
    DWORD length = 32768;
    std::vector<wchar_t> url(length);
    const HRESULT result = UrlCreateFromPathW(
        document.c_str(), url.data(), &length, 0);
    if (FAILED(result)) throw std::runtime_error("Could not create a file URL.");
    return std::wstring(url.data());
}

wv::ChakraCore::Options chakraOptions() {
    wv::ChakraCore::Options options;
    options.enabled = true;
    int count = 0;
    LPWSTR* arguments = CommandLineToArgvW(GetCommandLineW(), &count);
    for (int i = 1; arguments && i < count; ++i) {
        if (wcscmp(arguments[i], L"--no-chakra") == 0) {
            options.enabled = false;
        } else if (wcscmp(arguments[i], L"--chakra") == 0) {
            options.enabled = true;
        } else if (wcscmp(arguments[i], L"--chakra-console") == 0) {
            options.enabled = true;
            options.openDebugConsole = true;
        } else if (wcscmp(arguments[i], L"--chakra-script") == 0 &&
                   i + 1 < count) {
            options.enabled = true;
            options.scriptPath = arguments[++i];
        }
    }
    if (arguments) LocalFree(arguments);
    return options;
}

}  // namespace

WEBVIEW_MAIN {
    try {
        auto document = documentFromCommandLine();
        if (document.empty()) return 0;

        std::unique_ptr<HtaSnapshot> snapshot;
        std::wstring extension = document.extension().wstring();
        std::transform(extension.begin(), extension.end(), extension.begin(),
                       [](wchar_t ch) { return static_cast<wchar_t>(towlower(ch)); });
        if (extension == L".hta") {
            snapshot = std::make_unique<HtaSnapshot>(document);
            document = snapshot->path();
        }

        const auto url = fileUrl(document);
        wv::WebView view{1100, 760, true, true, Str("WelsonJS HTA"), url};
        if (view.init() != 0) return 1;

        const auto options = chakraOptions();
        wv::ChakraCore chakra(view, options);
        if (options.enabled) chakra.start();

        while (view.run() == 0) {}
        return 0;
    } catch (const std::exception& error) {
        MessageBoxA(nullptr, error.what(), "WelsonJS HTA",
                    MB_OK | MB_ICONERROR);
        return 1;
    }
}
