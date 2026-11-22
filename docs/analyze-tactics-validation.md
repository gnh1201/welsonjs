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
| **All attack segments** | **101** | **51 / 71** | |

### False results

| Kind | Result |
| --- | --- |
| Attack code answered "none" | 4 of 101: the Flame screenshot function, the Duqu driver version information, the EKANS service list, BPFDoor's bare RC4 functions |
| Ordinary snippets answered with a tactic | 1 of 14 (`config_read`, which reads its own settings: Reconnaissance, Discovery). The Enterprise prompt had 0. |
| WelsonJS `lib/` modules answered with a tactic | **6 of 6**: registry → Persistence, task-scheduler → Persistence, shell → Execution, security → Defense Impairment, powershell → Execution, virtualinput → Execution. Legitimate code that changes the host is not told apart from malware. |

### Region

| Snippets | Result |
| --- | --- |
| With evidence (10) | 9 right. A Kimsuky-style Korean HWP lure came out as South Korea. |
| Attack code without evidence (101) | All "unknown", including Stuxnet, Flame, Duqu and BPFDoor, which industry ties to states. |
| Ordinary code | Not asked: earlier, a comment naming Korea and a `.kr` address were taken as evidence for South Korea. |

### Effect of the ICS tactics

- **FrostyGoop:** its Modbus write function now gets Impact (ICS).
- **TRITON:** it still gets no ICS tactic. Its code that writes a program into a safety controller came out as Execution and Initial Access, not Inhibit Response Function.
- **Spurious ICS answer:** the Russian ransom note got Impact (ICS) next to Impact.
- **Small regression:** on the 56 segments tested with both prompts, the right tactic came first in 27 instead of 30, and was among the answers in 42 instead of 41. Mirai dropped from 3 to 1 first, and one more ordinary snippet got a tactic.
- **Initial Access:** answered less often than with the Enterprise prompt (PowerShell download, port scan).

### Observations

- **Real WSH malware does well:** Houdini 4/5 first, the Gootloader decoded stage gets Command and Control. Even with every name obfuscated, the Dorkbot RunPE loader is Stealth 4/4.
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

## ATT&CK for ICS

ATT&CK for ICS 20.1 has 12 tactics. Nine share their names with Enterprise tactics (Execution, Persistence, ...), so the prompt keeps the Enterprise ones. It adds the three that only ICS has:
- **Inhibit Response Function (TA0107):** "prevent your safety, protection, quality assurance, and operator intervention functions from responding to a failure, hazard, or unsafe state"
- **Impair Process Control (TA0106):** "manipulate, disable, or damage physical control processes"
- **Impact (ICS) (TA0105):** "manipulate, interrupt, or destroy your ICS systems, data, and their surrounding environment"

The ICS samples are scored against ATT&CK for ICS: Triton S1009, Industroyer2 S1072, EKANS S0605, FrostyGoop S1165.

## Next

- **Hard negatives:** the WelsonJS modules show that host-changing library code is called malicious. A prompt instruction (for example: code that only wraps an API for others to call is not an attack) has to be tested on the whole set. Instructions about what not to answer have backfired before.
- **WSH prompt on other languages:** C, decompiled code and command lines score lower. A language-neutral prompt, or one per file type, needs its own run.
- **EKANS:** use its kill routine, not only its lists.
- **Other samples:** BlackEnergy 3 / KillDisk, Havex, PIPEDREAM / INCONTROLLER and SocGholish, through code quoted in analyses.
