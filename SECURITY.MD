# Security Note for WelsonJS

## License
The WelsonJS project is available under either the GPLv3 or MS-RL opensource licenses. If the GPLv3 license is not compatible with Microsoft products, the MS-RL license applies; otherwise, the GPLv3 license is used. Under these licenses, if you distribute modified versions of this project's source code to third parties, you may be required to disclose the source code. For more details, please refer to the `LICENSE` and `LICENSE_MSRL` files.

## Caution
This repository contains information on accessing Windows APIs and functions in the JavaScript runtime, along with recent case studies. While this can provide a flexible development environment for anyone, it can also be misused for malicious purposes. Please be aware that using this project to create abuse tools, such as for DoS attacks, may result in legal consequences in your country. We encourage you to use this project only for creating web technology-based applications, like Electron, or legally permitted testing tools.

## Known Use Cases
WelsonJS is typically used for the following purposes:

  * Testing web accessibility and compliance, including adherence to W3C standards (WEB-ARIA, WCAG), national laws (ADA/DDA, GDPR), and other relevant regulations.
  * Exploring vulnerabilities of equipment within the local network.
  * Improving the availability of VPN or proxy clients.
  * Building automation, CI/CD (Continuous Integration/Continuous Delivery), DevOps, and SecOps.
  * Asset evaluation (e.g., obtaining purchase history from online shopping and delivery websites).
  * Online video streaming quality testing and improvement.
  * Office automation and integration with LLM-based AI (e.g., ChatGPT) services.

## Notes
  1. If you plan to use WelsonJS for a purpose other than those mentioned above, please contact us beforehand.
  2. If you are looking for ways to use WelsonJS more efficiently, referencing the [LOLBAS (Living Off The Land Binaries and Scripts)](https://lolbas-project.github.io/) list can be helpful.

## Guidelines

### Use of Online Shopping and Delivery Websites
We are aware of cases where WelsonJS has been used to access the websites of online shopping or delivery companies for asset valuation. This is a good use case, but there have been reports of website downtime caused by excessive concurrent requests. Please exercise caution and avoid excessive simultaneous executions.

### Use for Online Video Streaming Quality Testing and Improvement
We are aware of cases where WelsonJS is used for video streaming quality testing and improvement. It should be used solely for expert-level streaming quality testing, often referred to by terms like 4K, 8K, HD, FHD, UHD, 720p, 1080p, etc. For such purposes, it is recommended to use videos provided by television manufacturers (e.g., LG, Samsung) or graphics card manufacturers (e.g., NVIDIA, AMD) specifically for testing purposes. It is essential to avoid using videos that contain content not legally permitted in your region. The WelsonJS developers and maintainers take no responsibility for the use of videos containing illegal content.

### Use for Scientific Research Institutes
WelsonJS is designed for flexible industrial facility control (a.k.a. Industrial Scripting) in environments where modifying compiled binaries is restricted. Parts or all of this project's source code may be found in use within scientific research institutes. In such cases, appropriate safety measures tailored to the specific application area may be required. If support is needed for these applications, please do not hesitate to contact us.

### Use for Security Testing
We are aware of instances where WelsonJS has been used by legitimate cybersecurity firms to discover and test vulnerabilities (such as credential stuffing) in IoT devices. If you intend to use WelsonJS as a security testing tool, it should be done in a controlled environment that complies with legal regulations.

### Use for Cloud Monitoring
WelsonJS is a project inspired by the requirements of a cloud service provider to develop lightweight software (e.g., agents) for collecting metrics on Windows systems. While using WelsonJS for this purpose is desirable, ensuring security in the server-client communication is entirely the responsibility of the user.

## Alternative Names
This program is also known by the following names. These names are used solely for the purpose of identifying the work and do not impact the license:

- DOI [10.5281/zenodo.11382384](https://zenodo.org/doi/10.5281/zenodo.11382384) (CERN/OpenAIRE Zenodo)
- ["A0562"](https://www.oss.kr/dev_competition_activities/show/544723e6-850a-4956-9194-79640420c19a)(2023) (2023 Open-source Development Contest, NIPA National IT Industry Promotion Agency, Republic of Korea)
- "C-2021-000237"(2021) (Copyright Registration Online System, Korea Copyright Commission, Republic of Korea)
- "Codename Macadamia"(2020) (Heavy industry specialized CSP in the Republic of Korea)

## Report Abuse
If you discover any instances of this project being misused, please report them.

- [GitHub Security Advisories](https://github.com/gnh1201/welsonjs/security)
- abuse@catswords.net
- ActivityPub [@catswords_oss@catswords.social](https://catswords.social/@catswords_oss)
- XMPP [catswords@conference.xmpp.catswords.net](xmpp:catswords@conference.xmpp.catswords.net?join)
- [Join Catswords on Microsoft Teams](https://teams.live.com/l/community/FEACHncAhq8ldnojAI)
