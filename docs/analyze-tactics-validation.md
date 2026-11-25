# Analyze Tactics: validation samples

The editor's **Analyze Tactics (MITRE ATT&CK)** asks the local model FrogNano for the tactics that the selected code performs, then for the region of the threat actor (see `analyzeTactics` in `native/WelsonJS.Augmented/WelsonJS.Launcher/editor.html`, and [#574](https://github.com/gnh1201/welsonjs/issues/574)). Small prompt changes move its answers. For example, telling the model when not to answer Initial Access made it answer Initial Access more often.

**Whenever the tactics or region prompts, the model or the request settings change, re-run the reference samples below and compare with the recorded results.** Report regressions before committing.

Hand-written snippets are not enough. They tend to contain telling words (`beacon`, `ransom`, `Defender`), and they made an earlier approach look accurate. That approach (embedding similarity) then failed on real Gootloader code, which has meaningless names. So the reference set is built around real malware, with representative cases from attacks on industrial automation and related campaigns.

## Reference samples

| Family | What it targeted | ATT&CK | Source (text only) | Segments |
| --- | --- | --- | --- | --- |
| **Stuxnet** | Industrial control systems: Siemens S7 PLCs controlling centrifuges | [S0603](https://attack.mitre.org/software/S0603/) | [research-virus/stuxnet](https://github.com/research-virus/stuxnet) ([Christian-Roggia/open-myrtus](https://github.com/Christian-Roggia/open-myrtus) now points there) | Decoding of module names, injection, file system filter rootkit |
| **Flame** (SkyWiper) | Espionage in the Middle East; shares code with Stuxnet | [S0143](https://attack.mitre.org/software/S0143/) | [andarielsec/flame](https://github.com/andarielsec/flame) (Hex-Rays decompilation) | Screenshot, key state, audio recording, Bluetooth discovery, service creation, network shares; miniFlame process injection and WinHttp POST |
| **Duqu** | Espionage against industrial manufacturers, gathering information for attacks like Stuxnet | [S0038](https://attack.mitre.org/software/S0038/) | [loneicewolf/DUQU](https://github.com/loneicewolf/DUQU) (only its text files) | Kernel driver service registration with an encrypted setting, driver masquerading as an IBM ServeRAID driver |
| **Mirai** | IoT devices (cameras, routers) turned into a DDoS botnet | [S0485](https://attack.mitre.org/software/S0485/) | [jgamblin/mirai-source-code](https://github.com/jgamblin/mirai-source-code) | Telnet scanner with default credentials, killer, obfuscated string table, CNC connection, UDP flood |
| **Gootloader** | Windows Script Host loader delivered as fake documents; the kind of script the editor handles | – | [gnh1201/gootloader-payload-2019](https://github.com/gnh1201/gootloader-payload-2019) | Decoded stage (C2 polling of three domains, decode and run), string scrambler, marker file, delay loop, Function constructor call |
| BPFDoor | Linux backdoor in telecom networks | [S1161](https://attack.mitre.org/software/S1161/) | [gwillgues/BPFDoor](https://github.com/gwillgues/BPFDoor) | BPF magic-packet loop, process masquerading, iptables redirect and shell, RC4 |

### Additional samples

| Family | What it targeted | ATT&CK | Source (text only) | Segments |
| --- | --- | --- | --- | --- |
| **TRITON / TRISIS** | Safety instrumented systems (Schneider Triconex) | [S1009](https://attack.mitre.org/software/S1009/) (ICS) | [MDudek-ICS/TRISIS-TRITON-HATMAN](https://github.com/MDudek-ICS/TRISIS-TRITON-HATMAN) (decompiled Python) | Attack script, `SafeAppendProgramMod` and `WriteFunctionOrProgram` |
| **Industroyer2** | Power grid in Ukraine (2022) | [S1072](https://attack.mitre.org/software/S1072/) (ICS) | [zzzzzzzz-jay/Industroyer2_Research](https://github.com/zzzzzzzz-jay/Industroyer2_Research) | Excel loader macro, Solaris cron worm |
| **EKANS** | Ransomware that kills ICS processes | [S0605](https://attack.mitre.org/software/S0605/) (ICS) | [idafchev/EKANS-String-Decryptor](https://github.com/idafchev/EKANS-String-Decryptor) | Decrypted process and service lists |
| **FrostyGoop** | Modbus TCP commands to heating controllers (2024) | [S1165](https://attack.mitre.org/software/S1165/) (ICS) | [lucasdbrown/frostygoop_analysis](https://github.com/lucasdbrown/frostygoop_analysis) (a commented re-implementation) | Modbus task function |
| **Houdini** (H-worm) | VBS RAT and worm, the editor's language | – | [AhmetHan/h-worm_houdini](https://github.com/AhmetHan/h-worm_houdini) | C2 loop, USB spreading with shortcuts, startup, security software query, POST |
| **Dorkbot** (NgrBot) | "Shortcut" USB worm; here the .NET crypter of a sample detected as Ngrbot | – | [vxunderground/MalwareSourceCode](https://github.com/vxunderground/MalwareSourceCode) `Win32/InternetWorm/Worm.Win32.Ngrbot.dgu.7z` (C# sources only, password `infected`) | Resource payload decryption, RunPE process hollowing with encrypted API names, string decryption, decoy imports |
| [Atomic Red Team](https://github.com/redcanaryco/atomic-red-team) | Labeled tests, 2 Windows tests per tactic (23 techniques) | technique IDs | `atomics/*/T*.yaml` | Command lines |
| [LOLBAS](https://lolbas-project.github.io/) | Living-off-the-land binaries | technique IDs | `yml/OSBinaries/*.yml` | mshta, regsvr32, certutil, bitsadmin, rundll32, msbuild, cmstp, esentutl |
| **Irongate** | Siemens PLC simulation (2014–2016): records process inputs and replays them while it manipulates the process | not in ATT&CK | [es0/IRONGATE-ICS](https://github.com/es0/IRONGATE-ICS) (decompiled C#) | Record-and-replay of the PLC simulator, DLL proxy installer with process kill and start |
| **INCONTROLLER-style testbed** | Modbus PLCs; a research re-creation of [S1045](https://attack.mitre.org/software/S1045/) (ICS) techniques, not the original tool | S1045 (ICS) | [Roddys-Codex/ot-security-test-incontroller](https://github.com/Roddys-Codex/ot-security-test-incontroller) `attacker/exploits/` | Modbus scan and coil write, changed PLC set point (`heater-malicious.st`), man-in-the-middle packet modification, password sniffing; the unchanged PLC program as an ordinary sample |
| **Shamoon** (Disttrack) | Wiper against energy companies (2012) | [S0140](https://attack.mitre.org/software/S0140/) | [micrictor/disttrack](https://github.com/micrictor/disttrack) (reconstructed dropper), [research-virus/shamoon](https://github.com/research-virus/shamoon) (C&C) | Spreading over admin shares, service install, setup with time stamp copying, C&C client and server |
| **SocGholish** (FakeUpdates) | Fake browser update JScript loader | [S1124](https://attack.mitre.org/software/S1124/) | [aaronsamala/socgholish](https://github.com/aaronsamala/socgholish) (base64 text files) | Obfuscated loader, its beautified form, an analyst's partial string decoder |
| **BlackEnergy** 2 | Driver of the toolkit later used against Ukraine's grid | [S0089](https://attack.mitre.org/software/S0089/) | [DanielAvinoam/BlackEnergyV2-Driver-Reverse-Engineering](https://github.com/DanielAvinoam/BlackEnergyV2-Driver-Reverse-Engineering) | An analyst's re-implementation of its API name hashing (`BEHashCalc.cpp`) |
| WelsonJS `lib/` | Hard negatives: legitimate code that changes the host | – | This repository | registry, task-scheduler, shell, security, powershell, virtualinput (first 6,000 characters) |

They are complemented by:
- 24 hand-written WSH attack snippets, one or two behaviors each.
- 14 ordinary snippets, which should get "none".
- 10 snippets with region evidence: a group or tool name, or the language of strings.

### Gold labels

- The right tactics of a segment follow the techniques that ATT&CK maps for the software, in the tactics of ATT&CK Enterprise 20.1. Defense Evasion maps to Stealth (TA0005) or Defense Impairment (TA0112).
- A segment may accept more than one tactic, for example process injection: Stealth, Privilege Escalation or Execution.
- The ICS samples are scored against ATT&CK for ICS, with the three ICS-only tactics the prompt adds (see [ATT&CK for ICS](#attck-for-ics)).
- The region of every reference sample is "unknown". The code holds no evidence of an origin, and the region prompt only summarizes groups whose ATT&CK description states an attribution. Industry ties Stuxnet, Flame and Duqu to the US and Israel, and BPFDoor to a China-nexus group. An answer naming any of them is invented, not read from the code.

### Handling

- **Read only, never run.** Fetch the files as text through the GitHub API or `raw.githubusercontent.com`, into a scratch folder outside the repository. Never build, run or open the samples, and do not download binaries. The Duqu repository is mostly binaries; only `reg/dequ jmiNET3.reg` and `DRVS/SYS_info.txt` are used.
- **Antivirus may quarantine a file when it is saved.**
  - It did so with `191119-original.js`, and the tutorial copy of the same code was used instead.
  - It also quarantined the Houdini source. The owner of the machine then turned antivirus off for the run, and the source was fetched as text. Prefer another copy of the same code. If antivirus has to be off, turn it on again afterwards.
- **Cut segments at real call sites.** Decompiled code repeats API names in declarations (`// extern ...`) and thunks.
- **Long segments are cut** at the editor's limit of 6,000 characters.

## Metrics

| Metric | Wanted |
| --- | --- |
| Right tactic first | as high as possible |
| Right tactic among the answers (at most two are shown) | as high as possible |
| Attack code answered "none" | 0 |
| Ordinary code answered with a tactic | 0 |
| Region "unknown" for code without evidence | all |
| Region right for code with evidence | as high as possible |

## Summary in percentages

All four models on all 147 segments (116 attack, 15 ordinary, 6 WelsonJS modules, 10 with region evidence), from the tables below. The count is in brackets. Black Onyx 5B used the first-JSON parser.

| Metric | Better | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- | --- |
| Right tactic first (116 attack segments) | higher | **49.1%** (57) | 43.1% (50) | 25.9% (30) | 37.1% (43) |
| Right tactic among the answers (116) | higher | **70.7%** (82) | 62.1% (72) | 36.2% (42) | 58.6% (68) |
| Attack code answered "none" (116) | lower | 6.9% (8) | 6.9% (8) | 7.8% (9) | **0.0%** (0) |
| No usable tactics answer (147) | lower | **0.0%** (0) | 0.7% (1) | 36.1% (53) | 0.7% (1) |
| Ordinary snippets with a tactic (15) | lower | 6.7% (1) | 13.3% (2) | 0.0% (0)¹ | 80.0% (12) |
| WelsonJS `lib/` modules with a tactic (6) | lower | 100.0% (6) | 100.0% (6) | 66.7% (4)¹ | 100.0% (6) |
| Region right with evidence (10) | higher | **90.0%** (9) | **90.0%** (9) | 70.0% (7) | 70.0% (7) |

¹ Mostly because Qwensploit gave no answer at all, not because it judged the code ordinary.

Right tactic first / among the answers, by group of attack segments:

| Group | n | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- | --- |
| Real WSH/VBS malware (Gootloader, Houdini, SocGholish) | 16 | 62.5% / 93.8% | 62.5% / 75.0% | 25.0% / 37.5% | 43.8% / 75.0% |
| Hand-written WSH snippets | 24 | 62.5% / 87.5% | 62.5% / 87.5% | 45.8% / 66.7% | 45.8% / 70.8% |
| Stuxnet, Flame and miniFlame, Duqu | 13 | 46.2% / 61.5% | 15.4% / 53.8% | 23.1% / 46.2% | 46.2% / 53.8% |
| ICS (TRITON, Industroyer2, EKANS, FrostyGoop, Irongate, INCONTROLLER) | 13 | 38.5% / 61.5% | 23.1% / 30.8% | 15.4% / 15.4% | 46.2% / 61.5% |
| Other compiled malware (BPFDoor, Mirai, Dorkbot, Shamoon, BlackEnergy) | 19 | 47.4% / 78.9% | 52.6% / 68.4% | 5.3% / 10.5% | 31.6% / 42.1% |
| Commands (Atomic Red Team, LOLBAS) | 31 | 38.7% / 48.4% | 32.3% / 48.4% | 29.0% / 32.3% | 22.6% / 51.6% |
| **All attack segments** | **116** | **49.1% / 70.7%** | **43.1% / 62.1%** | **25.9% / 36.2%** | **37.1% / 58.6%** |

The groups are small (13 to 31 segments), so one segment moves a group by 3 to 8 points.

### Missed by every model

In 26 of the 116 attack segments (22.4%), none of the four models had a right tactic among its answers. No ordinary snippet was tagged by all four. They fall into five kinds:

| Kind | n | Segments | What the models answered | Right |
| --- | --- | --- | --- | --- |
| **Signed Windows binaries used to run or fetch code** | 8 | LOLBAS mshta, regsvr32, bitsadmin, rundll32, cmstp, esentutl; Atomic T1218.011 (rundll32), T1564.012 (exclusions that hide files) | Execution, Initial Access, Resource Development | Stealth (system binary proxy execution, hiding artifacts); Command and Control for esentutl's remote copy |
| **Impact without telling words** | 5 | Mirai UDP flood, Atomic T1491.001 (defacement), shadow copy deletion, EKANS process kill list and service stop list | Initial Access, Defense Impairment, Discovery, "none" | Impact; for EKANS also Inhibit Response Function |
| **Collection in decompiled or scripted code** | 5 | Flame screenshot, key state and audio recording; Atomic T1113 (screenshot); INCONTROLLER password sniffing | Execution, Stealth, "none" | Collection, Credential Access |
| **Bare building blocks** | 4 | BPFDoor RC4 and its packet loop, BlackEnergy API hashing, Duqu driver version information | "none", Initial Access | Command and Control, Stealth |
| **Movement and control** | 4 | Hand-written lateral movement and UAC bypass, Atomic T1047 (WMI), INCONTROLLER packet modification | Initial Access, Persistence, Discovery, "none" | Lateral Movement, Privilege Escalation, Execution, Impair Process Control |

What they have in common:
- **The tool is mistaken for the purpose.** Running code through `mshta` or `rundll32` is read as Execution, where ATT&CK files it under Stealth because a trusted binary hides the code. A WMI command is read as Discovery, where it is Execution. A command line names the binary, not why it is used.
- **Initial Access stands in for "something arrives".** Lateral movement, remote copies and downloads by LOLBins get Initial Access, the tactic the Qwen-based models over-answer.
- **Impact needs a word.** Ransom notes get Impact; a flood loop, a defacement or deleting shadow copies does not. Shadow copy deletion was read as Defense Impairment, which is close but not what ATT&CK says (Inhibit System Recovery, Impact).
- **Code that only prepares an attack gets "none".** A cipher, a hash routine or version strings carry no behavior of their own. A human reads them by their place in the malware, which a single segment does not show.
- **ICS process manipulation** (see [above](#irongate-incontroller-shamoon-socgholish-and-blackenergy)).

These are the cases a prompt change should be tested on first. Some of them, such as the bare building blocks, may stay out of reach for a classifier that sees one segment at a time.

## Recorded results

FrogNano 4B (`frognano_4b_2609`), CPU, 2026-11-22. Two prompt versions:
- **Enterprise:** the Enterprise tactics (#575).
- **Enterprise + ICS:** the current prompt, which adds the three ICS-only tactics.

Each cell is "right tactic first / right tactic among the answers". At most two tactics are shown.

| Set | n | Enterprise + ICS | Enterprise |
| --- | --- | --- | --- |
| Gootloader | 8 | 4 / 7 | 5 / 7 |
| Hand-written WSH | 24 | 15 / 21 | 15 / 21 |
| BPFDoor | 4 | 1 / 2 | 1 / 1 |
| Mirai | 5 | 1 / 4 | 3 / 4 |
| Stuxnet | 3 | 2 / 3 | 2 / 3 |
| Flame and miniFlame | 8 | 3 / 4 | 3 / 4 |
| Duqu | 2 | 1 / 1 | 1 / 1 |
| TRITON | 2 | 1 / 2 | – |
| Industroyer2 | 2 | 2 / 2 | – |
| EKANS | 2 | 0 / 0 | – |
| FrostyGoop | 1 | 1 / 1 | – |
| Houdini | 5 | 4 / 5 | – |
| Dorkbot (NgrBot loader) | 4 | 4 / 4 | – |
| Atomic Red Team | 23 | 11 / 14 | – |
| LOLBAS | 8 | 1 / 1 | – |
| **First 101 attack segments** | **101** | **51 / 71** | |
| Irongate | 2 | 0 / 2 | – |
| INCONTROLLER-style testbed | 4 | 1 / 1 | – |
| Shamoon | 5 | 3 / 5 | – |
| SocGholish | 3 | 2 / 3 | – |
| BlackEnergy API hashing | 1 | 0 / 0 | – |
| **All attack segments** | **116** | **57 / 82** | |

The last five sets were added on 2026-11-25 and ran with the same prompt (see [Irongate, INCONTROLLER, Shamoon, SocGholish and BlackEnergy](#irongate-incontroller-shamoon-socgholish-and-blackenergy)).

### False results

| Kind | Result |
| --- | --- |
| Attack code answered "none" | 8 of 116: the Flame screenshot function, the Duqu driver version information, the EKANS service list, BPFDoor's bare RC4 functions; the changed PLC set point, the Modbus packet modification, the password sniffing, the BlackEnergy hashing routine |
| Ordinary snippets answered with a tactic | 1 of 15 (`config_read`, which reads its own settings: Reconnaissance, Discovery). The Enterprise prompt had 0 of 14. The unchanged PLC program got "none". |
| WelsonJS `lib/` modules answered with a tactic | **6 of 6**: registry → Persistence, task-scheduler → Persistence, shell → Execution, security → Defense Impairment, powershell → Execution, virtualinput → Execution. Legitimate code that changes the host is not told apart from malware. |

### Region

| Snippets | Result |
| --- | --- |
| With evidence (10) | 9 right. A Kimsuky-style Korean HWP lure came out as South Korea. |
| Attack code without evidence (116) | All "unknown" where the region was asked, including Stuxnet, Flame, Duqu and BPFDoor, which industry ties to states. |
| Ordinary code | Not asked: earlier, a comment naming Korea and a `.kr` address were taken as evidence for South Korea. |

### Effect of the ICS tactics

- **FrostyGoop:** its Modbus write function now gets Impact (ICS).
- **TRITON:** it still gets no ICS tactic. Its code that writes a program into a safety controller came out as Execution and Initial Access, not Inhibit Response Function.
- **Irongate and INCONTROLLER:** no ICS tactic. Record-and-replay came out as Execution and Stealth, the Modbus coil write as Discovery and Initial Access, and the changed set point and packet modification as "none".
- **Spurious ICS answer:** the Russian ransom note got Impact (ICS) next to Impact.
- **Small regression:** on the 56 segments tested with both prompts, the right tactic came first in 27 instead of 30, and was among the answers in 42 instead of 41. Mirai dropped from 3 to 1 first, and one more ordinary snippet got a tactic.
- **Initial Access:** answered less often than with the Enterprise prompt (PowerShell download, port scan).

### Observations

- **Real WSH malware does well:** Houdini 4/5 first, the Gootloader decoded stage gets Command and Control, the SocGholish loader gets Stealth and Execution even when obfuscated. Even with every name obfuscated, the Dorkbot RunPE loader is Stealth 4/4.
- **ICS process manipulation is missed:** Modbus writes, PLC logic changes and record-and-replay get no ICS tactic, and often "none".
- **Decompiled C and one-line commands do worse.**
  - Flame and Mirai: only 1 to 3 first.
  - LOLBAS (`mshta.exe {PATH:.hta}` and the like): only 1 of 8. The answer is mostly Initial Access and Execution.
- **Lists without actions get "none" or Discovery.** EKANS's process and service lists say what it kills, not that it kills them. Test it with its kill routine instead.
- **Speed:** 52 seconds per segment on average (tactics, then region). Long decompiled code took 100 to 280 seconds.

## FrogNano against its base model

FrogNano 4B is a fine-tune of Qwen3.5-4B. The base model (`Qwen_Qwen3.5-4B-Q4_K_M.gguf` from [bartowski/Qwen_Qwen3.5-4B-GGUF](https://huggingface.co/bartowski/Qwen_Qwen3.5-4B-GGUF), SHA-256 `13c16f42...a983`) ran on the same 131 segments. The prompts (Enterprise + ICS, then region) and the request settings were the same: temperature 0, thinking off, no prompt translation. Each cell is "right tactic first / among the answers".

| Set | n | FrogNano | Qwen3.5-4B |
| --- | --- | --- | --- |
| Gootloader | 8 | 4 / 7 | 4 / 5 |
| Hand-written WSH | 24 | 15 / 21 | 15 / 21 |
| BPFDoor | 4 | 1 / 2 | 2 / 2 |
| Mirai | 5 | 1 / 4 | 2 / 4 |
| Stuxnet | 3 | 2 / 3 | 0 / 2 |
| Flame and miniFlame | 8 | 3 / 4 | 1 / 4 |
| Duqu | 2 | 1 / 1 | 1 / 1 |
| TRITON | 2 | 1 / 2 | 0 / 0 |
| Industroyer2 | 2 | 2 / 2 | 1 / 2 |
| EKANS | 2 | 0 / 0 | 0 / 0 |
| FrostyGoop | 1 | 1 / 1 | 1 / 1 |
| Houdini | 5 | 4 / 5 | 4 / 5 |
| Dorkbot | 4 | 4 / 4 | 3 / 3 |
| Atomic Red Team | 23 | 11 / 14 | 10 / 15 |
| LOLBAS | 8 | 1 / 1 | 0 / 0 |
| **All attack segments** | **101** | **51 / 71** | **44 / 65** |

| | FrogNano | Qwen3.5-4B |
| --- | --- | --- |
| Attack code answered "none" | 4 | 4 (the same four) |
| Ordinary snippets with a tactic | 1 of 14 | 2 of 14 |
| WelsonJS `lib/` modules with a tactic | 6 of 6 | 6 of 6 |
| Region right with evidence | 9 of 10 | 9 of 10 (the same miss) |
| Region invented without evidence | 0 | 1 (`powershell.js` → South Korea, from the `.kr` author address in its header) |
| Answers including Initial Access (131 segments) | 35 | 51 |
| ICS tactics answered | 2 (FrostyGoop right, the Russian ransom note wrong) | 5 (FrostyGoop right; VSS deletion, BPFDoor masquerading, the Mirai killer and the Russian note wrong) |
| No usable answer | 0 | 1 (Dorkbot RunPE) |
| Seconds per segment (average) | 52 | 42 |

**The fine-tune is worth about 7 more right first answers and 6 more right answers in 101 attack segments** (51 against 44 first, 71 against 65 among the answers). The gain is in the samples that matter most here:
- Stuxnet, Flame, TRITON, Industroyer2 and LOLBAS score better with FrogNano.
- On WSH scripts the two tie: the hand-written snippets and Houdini give the same scores.
- Qwen3.5-4B adds Initial Access far more often (51 against 35 answers), gives ICS tactics where they do not belong, and once took an author's address as evidence of a region.
- Qwen3.5-4B is about 20% faster.
- Neither model tells legitimate host-changing library code from malware.

## Security-specialized candidates

Two cybersecurity models were proposed in [#574](https://github.com/gnh1201/welsonjs/issues/574) as candidates to test against FrogNano. Both ran with the same prompts and settings as FrogNano: temperature 0, thinking off, no translation, a 200-token output limit and the same segments. FrogNano and Qwen3.5-4B are the controls.

- **Qwensploit V2 4B**
  - File: `Qwen3.5-4B-Claude-3xMerge-Jackrong-UNCENSORED-heretic.Q4_K_M.gguf` from [xtools-at/Qwensploit-V2-4B-CyberSecurity-Qwen3.5-GGUF](https://huggingface.co/xtools-at/Qwensploit-V2-4B-CyberSecurity-Qwen3.5-GGUF), SHA-256 `5fbeb00b...f0f5`.
  - The model card says these are the static quants of Qwensploit V2 4B, although the files carry the name of its base model, a Qwen3.5-4B merge.
  - Its license is "other" and needs review.
- **Black Onyx 5B**
  - File: `Black-Onyx-5B-Q4_K.gguf` from [Clownstein/Black-Onyx-5B-GGUF](https://huggingface.co/Clownstein/Black-Onyx-5B-GGUF), SHA-256 `6d5f8b6f...a4c7`. It is based on Gemma 4 E2B IT, under the Gemma terms of use.
  - The repository is gated and was downloaded with an account that accepted its conditions.
  - The WelsonJS llama.cpp build loads its Gemma 4 architecture.
  - Its answers end with a stray `<unused53>` token and a second JSON object. The editor's parser reads from the first `{` to the last `}`, so it fails on these answers. Its run used a parser that reads the first complete JSON object. With FrogNano, which writes a single object, both parsers read the same answers.

Each cell is "right tactic first / among the answers".

| Set | n | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- | --- |
| Gootloader | 8 | 4 / 7 | 4 / 5 | 1 / 2 | 2 / 5 |
| Hand-written WSH | 24 | 15 / 21 | 15 / 21 | 11 / 16 | 11 / 17 |
| BPFDoor | 4 | 1 / 2 | 2 / 2 | 1 / 1 | 1 / 1 |
| Mirai | 5 | 1 / 4 | 2 / 4 | 0 / 1 | 0 / 1 |
| Stuxnet | 3 | 2 / 3 | 0 / 2 | 1 / 3 | 1 / 1 |
| Flame and miniFlame | 8 | 3 / 4 | 1 / 4 | 2 / 3 | 4 / 5 |
| Duqu | 2 | 1 / 1 | 1 / 1 | 0 / 0 | 1 / 1 |
| TRITON | 2 | 1 / 2 | 0 / 0 | 0 / 0 | 2 / 2 |
| Industroyer2 | 2 | 2 / 2 | 1 / 2 | 2 / 2 | 2 / 2 |
| EKANS | 2 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| FrostyGoop | 1 | 1 / 1 | 1 / 1 | 0 / 0 | 1 / 1 |
| Houdini | 5 | 4 / 5 | 4 / 5 | 3 / 4 | 3 / 5 |
| Dorkbot | 4 | 4 / 4 | 3 / 3 | 0 / 0 | 1 / 1 |
| Atomic Red Team | 23 | 11 / 14 | 10 / 15 | 9 / 10 | 6 / 14 |
| LOLBAS | 8 | 1 / 1 | 0 / 0 | 0 / 0 | 1 / 2 |
| **All attack segments** | **101** | **51 / 71** | **44 / 65** | **30 / 42** | **36 / 58** |

| | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- |
| Attack code answered "none" | 4 | 4 | 7 | **0** |
| No usable tactics answer (131 segments) | 0 | 1 | **39** | 1 |
| Region answer not readable | 0 | 0 | **33** | 0 |
| Ordinary snippets with a tactic | 1 of 14 | 2 of 14 | 0 of 14 | **11 of 14** |
| WelsonJS `lib/` modules with a tactic | 6 of 6 | 6 of 6 | 4 of 6 | 6 of 6 |
| Region right with evidence | 9 of 10 | 9 of 10 | 7 of 10 | 7 of 10 |
| Region invented without evidence | 0 | 1 | 2 | 1 (the tourist data snippet → South Korea) |
| Answers including Initial Access | 35 | 51 | 24 | 2 |
| ICS tactics answered | 2 | 5 | 0 | 1 (FrostyGoop, right) |
| Seconds per segment (average) | 52 | 42 | 57 | 38 |

### What the candidates show

**Qwensploit V2 4B does not follow `"enable_thinking": false`.**
- It writes its reasoning first (`reasoning_content`) and uses up the 200-token limit before any answer. In the Houdini USB spreading segment, for example, the finish reason was `length`, with 200 completion tokens and an empty content.
- That causes most of its 39 empty tactics answers and 33 unreadable region answers, and its low "none" and false-positive counts come from the same empty answers.
- Under the settings the other models use, it is not usable.

**Black Onyx 5B almost never says "none".**
- It gave a tactic to 11 of 14 ordinary snippets and to every attack segment. That is why it never missed attack code.
- It reads industrial and decompiled code well: TRITON 2 of 2 (Execution and Impact on the safety controller code), Flame and miniFlame 4 of 5, FrostyGoop with Impact (ICS).
- It is weaker on WSH scripts and commands: Gootloader, the hand-written snippets, Dorkbot 1 of 4, Atomic Red Team 6 of 23 first.
- It rarely answers Initial Access, the opposite of the Qwen models.
- It is the fastest of the four.

**FrogNano stays the model for Analyze Tactics.**
- It has the most right first answers (51) and right answers overall (71).
- It has the fewest false results on ordinary code, and it parses with the editor as it is.
- Black Onyx is the strongest on the ICS samples but would turn most ordinary code into attack reports. That matters more for the editor, whose users mostly write ordinary and host-changing scripts.

## Irongate, INCONTROLLER, Shamoon, SocGholish and BlackEnergy

These 16 segments (15 attack, 1 ordinary PLC program) were added after the runs above. All four models ran on them with the same prompts and settings; Black Onyx again with the first-JSON parser. The base-model and candidate tables above cover the first 131 segments; the last row below gives the totals with these sets. [Recorded results](#recorded-results) includes them for FrogNano.

Each cell is "right tactic first / among the answers"; for the ordinary PLC program, "none" is right.

| Set | n | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- | --- |
| Irongate | 2 | 0 / 2 | 0 / 0 | 0 / 0 | 0 / 1 |
| INCONTROLLER-style testbed | 4 | 1 / 1 | 1 / 1 | 0 / 0 | 1 / 2 |
| Shamoon | 5 | 3 / 5 | 3 / 4 | 0 / 0 | 4 / 5 |
| SocGholish | 3 | 2 / 3 | 2 / 2 | 0 / 0 | 2 / 2 |
| BlackEnergy API hashing | 1 | 0 / 0 | 0 / 0 | 0 / 0 | 0 / 0 |
| **Attack segments** | **15** | **6 / 11** | **6 / 7** | **0 / 0** | **7 / 10** |
| Ordinary PLC program ("none") | 1 | right | right | no answer | Impact (ICS) |
| **All attack segments, both sets** | **116** | **57 / 82** | **50 / 72** | **30 / 42** | **43 / 68** |

| | FrogNano | Qwen3.5-4B | Qwensploit V2 4B | Black Onyx 5B |
| --- | --- | --- | --- | --- |
| Attack code answered "none" | 4 | 4 | 2 | 0 |
| No usable tactics answer | 0 | 0 | 14 | 0 |
| ICS tactics answered | 0 | 1 | 0 | 2 |
| Seconds per segment (average) | 99 | 106 | 72 | 72 |

**ICS process manipulation is not recognized.**
- No model gave Impair Process Control to Irongate's record-and-replay, the Modbus coil write or the packet modification.
- FrogNano and Qwen3.5-4B answered "none" to the changed set point, the packet modification and the password sniffing.
- Black Onyx gave Impact (ICS) to the changed set point, and the same answer to the unchanged program. The two differ in one constant (`max_temp` 95 → 98), so the code alone cannot tell them apart; a set point change needs the process context.

**Windows code is read as before.**
- Shamoon: Black Onyx 4 of 5 first, FrogNano 3 of 5. FrogNano and Qwen3.5-4B answered Initial Access for spreading over admin shares, where Lateral Movement is right.
- SocGholish: FrogNano and Qwen3.5-4B answered Stealth and Execution on the obfuscated and beautified loader. Black Onyx answered Execution and Persistence.
- The BlackEnergy hashing routine alone was "none" for three models.

**Qwensploit** again gave no answer to 14 of 16 segments, for the reason above.

## ATT&CK for ICS

ATT&CK for ICS 20.1 has 12 tactics. Nine share their names with Enterprise tactics (Execution, Persistence, ...), so the prompt keeps the Enterprise ones. It adds the three that only ICS has:
- **Inhibit Response Function (TA0107):** "prevent your safety, protection, quality assurance, and operator intervention functions from responding to a failure, hazard, or unsafe state"
- **Impair Process Control (TA0106):** "manipulate, disable, or damage physical control processes"
- **Impact (ICS) (TA0105):** "manipulate, interrupt, or destroy your ICS systems, data, and their surrounding environment"

The ICS samples are scored against ATT&CK for ICS: Triton S1009, Industroyer2 S1072, EKANS S0605, FrostyGoop S1165, INCONTROLLER S1045. Irongate is not in ATT&CK; its segments are scored by the ICS tactics of what the code does.

## Next

- **Hard negatives:** the WelsonJS modules show that host-changing library code is called malicious. A prompt instruction (for example: code that only wraps an API for others to call is not an attack) has to be tested on the whole set. Instructions about what not to answer have backfired before.
- **WSH prompt on other languages:** C, decompiled code and command lines score lower. A language-neutral prompt, or one per file type, needs its own run.
- **EKANS:** use its kill routine, not only its lists.
- **Candidates:** Qwensploit V2 4B could be retried with a larger output limit to separate its task ability from its thinking output. If the editor's parser should accept models that add text after the JSON (Black Onyx), make it read the first complete JSON object.
- **ICS process manipulation:** Modbus writes, PLC logic changes and record-and-replay were not recognized (see above). The prompt could describe what Impair Process Control looks like in code.
- **OrcaCyber Nano 1.0** ([orcarouter/OrcaCyber-Nano-1.0](https://huggingface.co/orcarouter/OrcaCyber-Nano-1.0), gated) is a 27B model based on Qwen3.8-27B. Its smallest model file is 13 GB. The 2 GB `DFlash2-Q8_0` file is a speculative-decoding drafter (GGUF architecture `dflash`, non-causal, reading the main model's layers) and does not run alone. It was not tested: it needs more memory than the 16 GB test machine has.
- **Other samples, no public code found:** only analyses, YARA rules, indicators or binaries were found for these families, from the [awesome-ics-malware](https://github.com/donadelden/awesome-ics-malware) list and a GitHub search. They could be used through code quoted in analyses:
  - fast16, Night Dragon, Havex, BlackEnergy 3 / KillDisk, Industroyer (2016);
  - PIPEDREAM / INCONTROLLER (the original tools; a re-creation is in the set above), COSMICENERGY;
  - Fuxnet, IOCONTROL, Chaya_003, DynoWiper, ZionSiphon.
