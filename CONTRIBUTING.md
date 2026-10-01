# Contributing to WelsonJS

[한국어 문서](CONTRIBUTING_KO.md)

WelsonJS is an open-source industrial JavaScript runtime and application framework for Windows, built on the built-in JavaScript engine. It connects JavaScript with Windows capabilities such as COM, desktop applications, files, processes, and optional .NET components.

Many organizations rely on long-running Windows software, local data, desktop workflows, and connected equipment. WelsonJS explores how a lightweight runtime and reusable modules can connect these established environments with newer services and tools. The project is grounded in Windows today; work on other platforms is exploratory rather than a current promise.

We welcome contributions of all sizes: code, issue reports, reproducible test cases, documentation, examples, reviews, and experiments. You do not need experience in every area. Interest in Windows, JavaScript runtimes, business or industrial software, desktop automation, or maintaining systems with real-world constraints is useful.

## Table of contents

- [Code of conduct](#code-of-conduct)
- [Project and development context](#project-and-development-context)
- [Ask a question](#ask-a-question)
- [Report a bug](#report-a-bug)
- [Suggest an enhancement](#suggest-an-enhancement)
- [Make a contribution](#make-a-contribution)
- [Code and test guidelines](#code-and-test-guidelines)
- [Security issues](#security-issues)
- [Developer support and contact](#developer-support-and-contact)

## Code of conduct

Participation in this project is governed by the [WelsonJS Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior to <abuse@catswords.net>.

## Project and development context

The repository contains the WSH-based runtime and loaders, JavaScript modules in `lib/`, examples, test profiles in `data/`, and optional managed components under `native/`. Review [AGENTS.md](AGENTS.md) for the architecture and contributor conventions, and consult the relevant module or project files for current behavior.

The runtime is not a general-purpose sandbox. Modules may use COM, Windows APIs, external programs, remote services, or optional managed components. Before proposing or testing an integration, identify its requirements and effects. Some work requires Windows features, installed software, credentials, network access, user interaction, or elevated privileges.

WelsonJS uses legacy JScript in its WSH scripts. Keep root scripts and `lib/` compatible with the target runtime; do not assume modern JavaScript or Node.js APIs are available unless the target file explicitly uses another engine.

## Ask a question

Search the [project documentation](https://catswords-oss.rdbl.io/5719744820/5330609327) and [existing issues](https://github.com/gnh1201/welsonjs/issues) first. If you still need help, [open an issue](https://github.com/gnh1201/welsonjs/issues/new) with the relevant environment and what you are trying to do. Include Windows, runtime, and dependency versions where they affect the question.

## Report a bug

Before opening an issue, check whether it is reproducible with the latest project version and whether an existing issue describes it. A useful report includes:

- What you expected and what happened
- Steps or a small example that reproduces the problem
- Windows version and architecture, WelsonJS revision, and relevant software or dependency versions
- Error text, logs, and input/output details that help diagnose it
- Whether the issue is consistent or limited to a particular environment

Use the [bug report form](https://github.com/gnh1201/welsonjs/issues/new). Do not include credentials, personal data, or other sensitive information.

## Suggest an enhancement

Search existing issues and documentation before proposing a change. In the [feature request](https://github.com/gnh1201/welsonjs/issues/new), describe the use case, current behavior, desired behavior, and relevant alternatives or constraints. For ideas involving new operating systems, devices, or protocols, distinguish the proposed direction from functionality that is already implemented and tested.

## Make a contribution

You can start with a small bug fix, reproduce behavior on a Windows version, clarify documentation, add an example, or review a change. For code contributions:

1. Find or open an issue to discuss substantial changes and avoid duplicating ongoing work.
2. Keep the change focused on the responsible module or project.
3. Describe externally visible behavior, dependencies, privileges, and side effects where relevant.
4. Open a pull request with the motivation, summary of changes, and the verification you performed. Link related issues when applicable.

By submitting a contribution, you confirm that you have the rights to provide it under the project's applicable license.

## Code and test guidelines

- Follow the existing style and preserve WSH/JScript compatibility in root scripts and `lib/`, unless a file explicitly targets another runtime.
- Validate values at JavaScript-to-COM and JavaScript-to-managed boundaries, and report errors with enough context to diagnose failures.
- Keep optional dependencies optional where the surrounding API already supports their absence.
- Update relevant documentation, examples, or test profile entries when externally visible behavior changes.
- Test only the affected behavior and describe what you ran. The JavaScript profile runner in `testloader.js` runs one selected test ID; it is an interactive/manual harness, not an automatic pass over a profile. Inspect the selected implementation before running it because it may access Windows features, applications, networks, or user interfaces.
- The managed cryptography validation project is separate from the JavaScript profile runner; follow its project documentation and build configuration.

## Security issues

Do not report vulnerabilities or sensitive security details in public issues. Send them to <abuse@catswords.re.kr> instead.

## Developer support and contact

WelsonJS does not currently offer formal employment. The project may consider bug bounties or other developer support for meaningful contributions; availability and terms depend on the contribution and project circumstances. Contact <oss@catswords.re.kr> to discuss a specific proposal.

You can also find the project community through [Discord](https://discord.gg/XKG5CjtXEj), [Microsoft Teams](https://teams.live.com/l/community/FEACHncAhq8ldnojAI), or [ActivityPub](https://catswords.social/@catswords_oss).
