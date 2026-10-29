#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <commdlg.h>
#include <objbase.h>
#include <shellapi.h>
#include <winrt/Windows.Web.UI.Interop.h>

#include <algorithm>
#include <cwctype>
#include <filesystem>
#include <stdexcept>
#include <string>
#include <vector>

namespace {

std::wstring quoteArgument(const std::wstring& value) {
    std::wstring quoted = L"\"";
    size_t backslashes = 0;
    for (const wchar_t ch : value) {
        if (ch == L'\\') {
            ++backslashes;
        } else if (ch == L'"') {
            quoted.append(backslashes * 2 + 1, L'\\');
            quoted += ch;
            backslashes = 0;
        } else {
            quoted.append(backslashes, L'\\');
            backslashes = 0;
            quoted += ch;
        }
    }
    quoted.append(backslashes * 2, L'\\');
    quoted += L'"';
    return quoted;
}

std::vector<std::wstring> commandLineArguments() {
    int count = 0;
    LPWSTR* arguments = CommandLineToArgvW(GetCommandLineW(), &count);
    if (!arguments) throw std::runtime_error("Could not read command line.");

    std::vector<std::wstring> result;
    result.reserve(static_cast<size_t>(count));
    for (int i = 1; i < count; ++i) result.emplace_back(arguments[i]);
    LocalFree(arguments);
    return result;
}

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

std::filesystem::path executableDirectory() {
    std::vector<wchar_t> modulePath(32768, L'\0');
    const DWORD length = GetModuleFileNameW(
        nullptr, modulePath.data(), static_cast<DWORD>(modulePath.size()));
    if (length == 0 || length >= modulePath.size())
        throw std::runtime_error("Could not locate the application directory.");
    return std::filesystem::path(modulePath.data()).parent_path();
}

std::filesystem::path findDocument(
    const std::vector<std::wstring>& arguments,
    std::vector<std::wstring>& forwardedArguments) {
    std::filesystem::path document;
    for (size_t i = 0; i < arguments.size(); ++i) {
        const auto& argument = arguments[i];
        forwardedArguments.push_back(argument);
        if (argument == L"--chakra-script" && i + 1 < arguments.size()) {
            forwardedArguments.push_back(arguments[++i]);
        } else if (argument.empty() || argument[0] == L'-') {
            continue;
        } else if (document.empty()) {
            document = std::filesystem::absolute(argument);
            if (!std::filesystem::is_regular_file(document) &&
                std::filesystem::path(argument).is_relative()) {
                document = executableDirectory() / argument;
            }
        }
    }

    if (document.empty()) {
        const auto currentDirectory = std::filesystem::current_path();
        const auto appDirectory = executableDirectory();
        const std::vector<std::wstring> defaultDocuments = {
            L"app.hta", L"index.hta", L"index.html"};
        for (const auto& filename : defaultDocuments) {
            const auto candidate = currentDirectory / filename;
            if (std::filesystem::is_regular_file(candidate)) {
                document = candidate;
                break;
            }
        }
        if (document.empty() && appDirectory != currentDirectory) {
            for (const auto& filename : defaultDocuments) {
                const auto candidate = appDirectory / filename;
                if (std::filesystem::is_regular_file(candidate)) {
                    document = candidate;
                    break;
                }
            }
        }
        if (document.empty()) {
            document = chooseDocument();
        }
    }
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

bool webView2RuntimeAvailable(const std::filesystem::path& loaderPath) {
    HMODULE loader = LoadLibraryW(loaderPath.c_str());
    if (!loader) return false;
    using GetAvailableVersion = HRESULT(STDAPICALLTYPE*)(PCWSTR, LPWSTR*);
    const auto getVersion = reinterpret_cast<GetAvailableVersion>(
        GetProcAddress(loader, "GetAvailableCoreWebView2BrowserVersionString"));
    LPWSTR version = nullptr;
    const bool available = getVersion &&
                           SUCCEEDED(getVersion(nullptr, &version)) && version;
    if (version) CoTaskMemFree(version);
    FreeLibrary(loader);
    return available;
}

bool edgeHtmlAvailable() {
    bool apartmentInitialized = false;
    try {
        winrt::init_apartment(winrt::apartment_type::single_threaded);
        apartmentInitialized = true;
        {
            winrt::Windows::Web::UI::Interop::WebViewControlProcessOptions options;
            winrt::Windows::Web::UI::Interop::WebViewControlProcess process(options);
            (void)process.GetWebViewControls();
        }
        winrt::uninit_apartment();
        return true;
    } catch (...) {
        if (apartmentInitialized) winrt::uninit_apartment();
        return false;
    }
}

bool launchViewer(const std::filesystem::path& executable,
                  const std::filesystem::path& document,
                  const std::vector<std::wstring>& forwardedArguments) {
    std::wstring commandLine = quoteArgument(executable.native()) + L" " +
                               quoteArgument(document.native());
    for (const auto& argument : forwardedArguments)
        commandLine += L" " + quoteArgument(argument);

    STARTUPINFOW startup{};
    startup.cb = sizeof(startup);
    PROCESS_INFORMATION process{};
    if (!CreateProcessW(executable.c_str(), commandLine.data(), nullptr, nullptr,
                        FALSE, 0, nullptr, nullptr, &startup, &process)) {
        MessageBoxW(nullptr, L"Could not start the selected WebView engine.",
                    L"WelsonJS HTA", MB_OK | MB_ICONERROR);
        return false;
    }
    CloseHandle(process.hThread);
    CloseHandle(process.hProcess);
    return true;
}

}  // namespace

int WINAPI WinMain(HINSTANCE, HINSTANCE, LPSTR, int) {
    try {
        const auto arguments = commandLineArguments();
        std::vector<std::wstring> forwardedArguments;
        const auto document = findDocument(arguments, forwardedArguments);
        if (document.empty()) return 0;

        const auto directory = executableDirectory();
        std::wstring extension = document.extension().wstring();
        std::transform(extension.begin(), extension.end(), extension.begin(),
                       [](wchar_t ch) { return static_cast<wchar_t>(towlower(ch)); });

        std::filesystem::path viewer;
        if (extension == L".hta") {
            viewer = directory / L"welsonjs-hta-mshtml.exe";
        } else if (webView2RuntimeAvailable(directory / L"WebView2Loader.dll")) {
            viewer = directory / L"welsonjs-hta-webview2.exe";
        } else if (edgeHtmlAvailable()) {
            viewer = directory / L"welsonjs-hta-edgehtml.exe";
        } else {
            viewer = directory / L"welsonjs-hta-mshtml.exe";
        }

        return launchViewer(viewer, document, forwardedArguments) ? 0 : 1;
    } catch (const std::exception& error) {
        MessageBoxA(nullptr, error.what(), "WelsonJS HTA",
                    MB_OK | MB_ICONERROR);
        return 1;
    }
}
