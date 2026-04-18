# AGENTS.md

*WelsonJS Agent Architecture Guide*

WelsonJS consists of two major execution layers:

1. **JavaScript execution environment** based on Windows Script Host (WSH) with `core-js` polyfills
2. **Native/Managed module environment** provided through the `WelsonJS.Toolkit` suite

This document defines the agents that operate inside WelsonJS, including their responsibilities, interaction boundaries, and design principles.

---

## **1. Overview & Design Principles**

WelsonJS aims to enable Windows application development using JavaScript on top of the legacy WSH engine while safely extending system-level functionality through .NET modules.

All agents follow these principles:

* **Single responsibility** — each agent performs one well-defined task
* **Minimal & explicit interfaces**
* **Security & integrity first**
* **Polyfill-based compatibility** (ES3 → ES5)
* **Graceful degradation when native modules are unavailable**

---

## **2. Agent Types**

WelsonJS defines two main categories of agents:

| Category                         | Description                                                                                                                |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **JavaScript Runtime Agent**     | Executes user scripts in a WSH + polyfill environment. Provides compatibility up to ES5 using `core-js` and related shims. |
| **Native/Managed Module Agents** | Extensions written in C# or VB.NET under the `WelsonJS.Toolkit` solution that provide system-level capabilities.           |

---

## **3. JavaScript Runtime Agent**

### **3.1 Description**

The JavaScript Runtime Agent executes scripts using the Windows Script Host engine (JScript).
Although the engine itself is **ES3-level**, WelsonJS uses **core-js polyfills** to emulate many **ES5 features**, allowing more modern syntax and APIs.

### **3.2 Responsibilities**

* Load and initialize polyfills (`core-js`, `JSON2.js`, additional shims)
* Provide CommonJS-style module system (`require`, `exports`, caching, resolution)
* Execute user scripts in a controlled environment
* Provide bound APIs that connect to native agents
* Ensure minimal pollution of the global scope
* Provide compatibility with classical WSH functions and COM interoperability
* Handle script errors and propagate exceptions with useful metadata

### **3.3 Execution Constraints**

* Must assume an **ES3 core** with **ES5 via polyfills**
* Features requiring ES6+ must be explicitly shimmed or avoided
* All host-bound functionality must route through defined native agents

---

## **4. Native/Managed Module Agents (Toolkit Agents)**

The **WelsonJS.Toolkit** solution provides several functional agents designed as modular .NET libraries.
Each project acts as a distinct agent with its own role and responsibility.

---

### **4.1 Catswords.Phantomizer — Assembly Loading Agent**

**Purpose:**
Dynamic loading and resolution of native/managed assemblies.

**Responsibilities:**

* Load DLLs from disk or remote CDN
* Support `.dll.gz` compressed downloads for performance
* Perform integrity/signed verification
* Cache resolved versions locally
* Provide fallback when assemblies fail to load

**Notes:**
This agent enables WelsonJS to remain lightweight while extending capabilities dynamically.

---

### **4.2 WelsonJS.Esent — Database Access Agent**

**Purpose:**
Expose Windows’ native **ESENT** database engine to JavaScript.

**Responsibilities:**

* Create, open, and manage ESENT databases
* Provide JS-friendly abstraction for tables, cursors, and transactions
* Perform safe parameter marshaling
* Ensure proper disposal of unmanaged resources
* Support transactional metadata operations (e.g., metadata store, instance tracking)

---

### **4.3 WelsonJS.Cryptography — Cryptography Agent**

**Purpose:**
Provide cryptographic functions not available in WSH/JS.

**Responsibilities:**

* Expose symmetric/legacy/industrial cryptographic algorithms
* Bridge specialized Korean algorithms (SEED, ARIA, HIGHT, LEA) where required
* Provide secure random generation utilities
* Ensure compliance with expected test vectors
* Offer safe JS bindings that validate parameters and reject unsafe operations

**Additional:**
`WelsonJS.Cryptography.Test` provides validation and regression testing for algorithm correctness.

---

### **4.4 WelsonJS.Launcher — Bootstrap Agent**

**Purpose:**
Serve as the entry point of a WelsonJS application.

**Responsibilities:**

* Initialize environment
* Load Phantomizer & toolkit modules
* Load and execute the main JavaScript script
* Handle configuration (AppName, BaseUrl, service mode, etc.)
* Provide safe-mode and fallback execution
* Manage initial logging and diagnostics

**Notes:**
Launcher must start with only .NET BCL dependencies to ensure predictable initialization.

---

### **4.5 WelsonJS.Service — Windows Service Agent**

**Purpose:**
Allow WelsonJS applications to run as Windows services.

**Responsibilities:**

* Install/uninstall service
* Run JavaScript scripts in service context
* Manage service lifecycle events (start, stop)
* Provide safe shutdown & worker loop management

---

### **4.6 WelsonJS.Toolkit — General Utility Agent**

**Purpose:**
A shared utility library providing cross-cutting functionality.

Typical responsibilities include:

* IO helpers
* Reflection helpers
* JS–Native bridging utilities
* Shared types and error-handling helpers
* Logging abstractions

It acts as the common foundation for other Toolkit modules.

---

## **5. Optional & Internal Agents**

### **5.1 Interop / Binding Agent**

Acts as a transport layer between JS Runtime Agent and Toolkit Agents.

Responsibilities:

* Marshal parameters and return values
* Validate type compatibility
* Protect against injection or malformed input
* Normalize exceptions into JS-friendly error objects
* Enforce version negotiation and capability detection

Often implemented inside each Toolkit module but may be abstracted.

---

### **5.2 Security / Policy Agent**

Provides security boundaries.

Responsibilities:

* Module integrity verification
* Allowed module list management
* Restricting file system or registry access
* Enforcing sandbox policies for script execution
* Logging suspicious activity

---

### **5.3 Fallback / Compatibility Agent**

Ensures application execution even without native modules.

Responsibilities:

* JS-only polyfill alternatives
* Reduced functionality mode
* Diagnostics for missing dependencies

---

## **6. Agent Interaction Model**

```text
[WelsonJS.Launcher] 
       ↓ Initialization + Configuration
[JavaScript Runtime Agent] ←→ [Toolkit Agents]
       ↑                           ↑
   (Interop Layer)          (Security/Policy)
```

* **Launcher** bootstraps the system.
* **JS Runtime Agent** handles all user logic.
* **Toolkit Agents** provide extended OS-level capabilities.
* **Interop Layer** ensures safe crossing between JS and .NET worlds.
* **Security Agent** governs what is allowed.

---

## **7. Implementation Guidelines**

* Follow "least-privilege" principles for all native module operations.
* All JS ↔ Native APIs must be explicit and documented.
* Native agents must degrade gracefully when unavailable.
* Polyfills must not assume ES6+ features unless explicitly shimmed.
* Fallback behaviors must be deterministic and logged.
* All agents must respect long-term compatibility from Windows XP → Windows 10/11.

---

## **8. Purpose of This Document**

AGENTS.md provides a shared understanding across contributors so that:

* New toolkit modules follow consistent architecture.
* JS runtime bindings remain predictable and safe.
* Extension developers can easily understand boundaries and responsibilities.
* The ecosystem grows without introducing breaking or conflicting functionality.

---

## **9. Test Structure (Test Plan)**

WelsonJS uses **JSON-based test profiles** plus a script runner (`testloader.js`) to verify both the JavaScript Runtime Agent and the Toolkit Agents.

A representative profile is `test-oss-korea-2023.json`, used in the 2023 South Korea OSS Contest to validate the WelsonJS environment.

---

### **9.1 Testing Approach**

* Tests are grouped into **profiles** (single JSON file per profile).
* Each profile describes:

  * Test **metadata** (description, updated date, dependencies, authors, references, tags)
  * **Schema version**
  * An array of **test entries**, each with `id`, `description`, and `tags`
* `testloader.js`:

  * Loads a given profile JSON
  * Interprets each test `id` as a runnable unit (usually bound to a script or implementation defined inside WelsonJS)
  * Executes them in the same WSH + polyfill environment that real users will use
  * Aggregates pass/fail status and reports the result

This keeps tests:

* Close to **real execution** (same engine, same polyfills)
* Capable of testing **JS-only behavior** and **JS ↔ native agent integration**
* Easy to extend by just adding new entries into JSON

---

### **9.2 Test Layers Mapped to Existing IDs**

The existing test profile covers multiple layers of the system.

1. **JavaScript Runtime / Polyfill Layer**

   * `es5_polyfills` – checks whether polyfills above ES5 level run successfully on the built-in engine.
   * These tests verify `core-js`, JSON handling, and basic language/runtime correctness.

2. **Windows Systems & Toolkit Integration**

   * Registry: `registry_find_provider`, `registry_write`, `registry_read`
   * WMI: `wmi_create_object`, `wmi_execute_query`, `wmi_result_query`
   * Shell: `shell_create_object`, `shell_build_command_line`, `shell_set_charset`, `shell_working_directory`, `shell_create_process`, `shell_execute`, `shell_run`, `shell_run_as`, `shell_find_my_documents`, `shell_release`
   * PowerShell: `powershell_set_command`, `powershell_set_file`, `powershell_set_uri`, `powershell_execute`, `powershell_run_as`
   * System information: `system_resolve_env`, `system_check_as`, `system_get_os_version`, `system_get_architecture`, `system_get_uuid`, `system_get_working_directory`, `system_get_script_directory`, `system_get_network_interfaces`, `system_get_process_list`, `system_get_process_list_by_name`, `system_register_uri`, `system_pipe_ipc`

   These exercise the **Interop/Binding Agent** and various **Toolkit Agents** that expose Windows APIs.

3. **Human Interface / Virtual Input (VHID)**

   * `vhid_find_window`, `vhid_send_click`, `vhid_send_keys`, `vhid_send_key_enter`, `vhid_send_key_functions`, `vhid_alert`, `vhid_confirm`, `vhid_prompt`

   These validate keyboard/mouse simulation and dialog APIs, ensuring the Virtual Human Interface agent behaves as expected.

4. **Network & HTTP Layer**

   * `network_http_get`, `network_http_post`, `network_http_extended`, `network_attach_debugger`, `network_detect_charset`, `network_detect_http_ssl`, `network_send_icmp`

   These tests exercise HTTP/ICMP functionality and optional debugger integration (e.g., Fiddler) via Toolkit Agents.

5. **Advanced String / NLP / Utility**

   * `extramath_dtm`, `extramath_cosine_similarity`, `base64_encode`, `base64_decode`

   These validate extra math/string/NLP-style helpers that may be implemented in JS or as native helpers.

6. **Chromium / Browser Control**

   * `chromium_run`, `chromium_create_profile`, `chromium_run_incognito`, `chromium_navigate`, `chromium_get_active_pages`, `chromium_find_page_by_id`, `chromium_find_pages_by_title`, `chromium_move_focused`, `chromium_adjust_window_size`, `chromium_get_element_position`, `chromium_get_mapreduced_element_position`, `chromium_set_value_to_textbox`, `chromium_send_click`, `chromium_send_keys`, `chromium_auto_scroll_until_end`

   These test the ChromiumDevTools-related agent responsible for controlling a Chromium-based browser.

7. **gRPC & GUI Integration**

   * gRPC: `grpc_run_server`, `grpc_receive_command`
   * WebView: `gui_check`

   These validate that **network service agents** (gRPC) and **GUI/WebView integration** work correctly in real environments.

---

### **9.3 Test Profile Schema**

A test profile JSON follows this general schema:

* **description**: Human-readable description of the profile
* **updated_on**: Last update date (string, e.g. `"2023-10-30"`)
* **dependencies**: Object listing required WelsonJS version or other requirements
* **authors**: Array of strings identifying authors
* **references**: Array of related URLs (repository, social, external descriptions)
* **tags**: Profile-wide tags (technologies, platforms, etc.)
* **schema**:

  * `version`: Schema version used by `testloader.js` (e.g. `"0.2"`)
* **tests**:

  * Array of test objects, each having:

    * `id`: Unique test identifier (string)
    * `description`: What the test checks (string)
    * `tags`: Array of tags describing category and domain

Example (simplified):

```json
{
  "description": "2023 South Korea OSS Contest Test Profile for WelsonJS",
  "updated_on": "2023-10-30",
  "dependencies": {
    "welsonjs": "0.2.7"
  },
  "schema": {
    "version": "0.2"
  },
  "tests": [
    {
      "id": "es5_polyfills",
      "description": "Checks whether polyfills above the ES5 level run successfully (using Windows' built-in engine).",
      "tags": ["JavaScript Engine", "ECMAScript Polyfills"]
    }
  ]
}
```

> The exact semantics for how each `id` maps to a concrete script or implementation are defined in `testloader.js` and the surrounding WelsonJS test harness.

---

### **9.4 Using `testloader.js`**

`testloader.js` is the standard runner that:

1. Accepts one or more **profile JSON** paths (e.g., `test-oss-korea-2023.json`).
2. Parses the profile according to the schema version.
3. Iterates over `tests[]`, and for each `id`:

   * Resolves the underlying implementation (JS file, bound function, etc.)
   * Executes the test logic in the WSH + polyfill environment
   * Captures success/failure and any diagnostic output
4. Produces a summary result (per test and overall).

When adding or modifying tests:

* Prefer **extending existing profiles** rather than inventing new formats.
* Keep `id` stable once published, so existing CI or documentation references do not break.
* Use `tags` to reflect which agent(s) a test touches (e.g., `"Windows Systems"`, `"Chromium-Based Browser"`, `"gRPC"`).

---

### **9.5 Naming & Organization**

Recommended file organization:

```text
/tests
  test-oss-korea-2023.json   # Contest profile (broad coverage)
  ...                        # Future profiles (e.g., cryptography-only, ESENT-only)
testloader.js
```

Guidelines:

* Use **profile-level tags** to describe the scope (`"windows"`, `"wsh"`, `"chromium"`, `"grpc"`, etc.).
* Keep large, real-world coverage (like the OSS contest profile) but also allow smaller focused profiles for specific agents (e.g., ESENT regression suite, Cryptography regression suite).
* For new features or agents, introduce corresponding `tests[]` entries with descriptive `id` and `description`, and appropriate `tags`.

---

### **9.6 Regression & Compatibility**

Because WelsonJS targets a wide range of Windows environments:

* Use the existing wide-coverage profile (like the OSS contest profile) as a **baseline regression suite**.
* When fixing a bug in any agent (e.g., ESENT, Chromium control, VHID input, HTTP stack):

  * Add or update a test entry in a JSON profile.
  * Ensure `testloader.js` can run it in automated environments.
* Keep compatibility in mind: if behavior must differ by OS version, reflect that in tests or tags (e.g., Windows XP vs Windows 10).

---

### **9.7 CI / Automation (Optional)**

If a CI pipeline is configured:

* Add a step that:

  * Invokes `testloader.js` on one or more profiles
  * Fails the build if any test in the profile fails
* Optionally support:

  * Running a **quick subset** (e.g., only `es5_polyfills` + core system tests)
  * Running the full OSS profile for release candidates or nightly builds
