# Join WelsonJS

[한국어 문서](CAREER_KO.md)

## Make Windows JavaScript useful in more places

WelsonJS is an open-source industrial JavaScript runtime and application framework for Windows, built on the built-in JavaScript engine. It brings JavaScript together with Windows facilities such as COM, desktop applications, files, processes, and optional .NET components.

Many organizations still depend on long-running Windows software, local databases, desktop workflows, and connected devices. Replacing all of them is not always practical. WelsonJS explores how a lightweight JavaScript runtime and reusable modules can connect these existing environments with newer services and tools.

The project is grounded in Windows today. Ideas from its runtime and integration work may inspire experiments on other platforms, but cross-platform support is exploratory rather than a current promise.

## Ways to contribute

You do not need experience in every part of the stack. Contributions can help in areas such as:

- Windows Script Host, legacy JScript, JavaScript runtimes, and compatibility
- Windows desktop development, COM, Windows APIs, C#, and .NET Framework
- Integrations with files, databases, HTTP services, browsers, Microsoft Office, and other desktop software
- Device and system integration, including serial, TCP/IP, and Windows system services
- Runtime performance, reliability, and operation in offline or restricted networks
- Examples, documentation, issue reproduction, code review, and open-source maintenance

The repository includes JavaScript modules in `lib/`, startup and example scripts, optional managed components in `native/`, and manually selected test cases. Check the implementation and its documented requirements before working with an integration: some need Windows features, external software, network access, credentials, user interaction, or elevated privileges. WSH uses legacy JScript, so syntax and host APIs must match the target runtime.

Contributions are not limited to code. A clear bug report, a reproducible test case, a small fix, a useful example, or a documentation correction can make the project better.

## What the project works on

### Connecting Windows capabilities

WelsonJS modules wrap capabilities available through WSH, COM, Windows APIs, optional managed libraries, and external programs. The aim is to make these interfaces usable from JavaScript while keeping each module's dependencies and behavior clear.

### Supporting established environments

The project values practical compatibility with Windows systems and workflows that remain in use. Work may involve diagnosing host-specific behavior, maintaining older code, and improving reliability where modern runtimes or continuous connectivity are unavailable.

### Integrating applications and services

The codebase includes adapters for local data, network protocols, desktop applications, and selected external services. These integrations vary in maturity and requirements; contributions should describe what is implemented and avoid implying universal support for a protocol, device, or platform.

### Exploring runtime ideas

We welcome experiments that investigate how runtime concepts could work beyond Windows. Such work is exploratory and should identify the target platform and what has actually been tested.

## Who might enjoy this project

WelsonJS may suit people who like understanding how existing systems work and connecting them to new tools. You might be interested in Windows internals, JavaScript engines, business or industrial software, desktop automation, or maintaining systems with real-world constraints.

You can start small: reproduce an issue on a Windows version, improve a module's error handling, add an example, clarify documentation, or review a change. Experience with POS or industrial environments is useful context, but it is not a requirement.

## Developer support

WelsonJS does not currently offer formal employment. The project may consider bug bounties or other developer support for meaningful contributions; availability and terms depend on the contribution and project circumstances. Contact us to discuss a specific proposal.

## Contact

* oss@catswords.re.kr
