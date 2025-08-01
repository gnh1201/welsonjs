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

* DOI [10.5281/zenodo.11382384](https://zenodo.org/doi/10.5281/zenodo.11382384)(2024) (CERN/OpenAIRE Zenodo)
* ["284757291"](https://ics.catswords.net/1494315-Certificate%2BSoR-284757291.pdf)(2024) (Registered with the [UK Copyright Service](https://copyrightservice.co.uk/))
* ["A0562"](https://www.oss.kr/dev_competition_activities/show/544723e6-850a-4956-9194-79640420c19a)(2023) (2023 Open-source Development Contest, NIPA National IT Industry Promotion Agency<sup>(정보통신산업진흥원)</sup>, Republic of Korea)
* ["2025-02-08-1952"](https://ics.catswords.net/20250410092300005.pdf)(2025) (Technical Data Bailment System (Technology Escrow), "Korea Foundation for Cooperation of Large & Small Business, Rural Affairs"<sup>(대·중소기업·농어업협력재단)</sup>, Republic of Korea)
* "C-2021-000237"(2021) (Copyright Registration Online System, Korea Copyright Commission<sup>(한국저작권위원회)</sup>, Republic of Korea)
* "Codename Macadamia"(2020) (Heavy industry specialized CSP in the Republic of Korea)

## Notice on the Use of AI Services
This project may include code that was generated with the help of LLM-based AI services such as [ChatGPT](https://chatgpt.com/) Pro, AI-powered code review tools (e.g., [CodeRabbit](https://www.coderabbit.ai/), [Sourcery](https://sourcery.ai/), [Qodo](https://www.qodo.ai/)), or LLM models provided through [Microsoft Azure AI Foundry](https://ai.azure.com/).

## Offline Distribution
You may [request offline distribution](https://forms.gle/4WCnJmnFNDzubUmk7) if you are in a restricted environment, such as one without internet access or with specific security requirements.

## Report abuse
* [GitHub Security Advisories (gnh1201/welsonjs)](https://github.com/gnh1201/welsonjs/security)
* [abuse@catswords.re.kr](mailto:abuse@catswords.re.kr)

## Join the community
* ActivityPub [@catswords_oss@catswords.social](https://catswords.social/@catswords_oss)
* XMPP [catswords@conference.omemo.id](xmpp:catswords@conference.omemo.id?join)
* [Join Catswords OSS on Microsoft Teams (teams.live.com)](https://teams.live.com/l/community/FEACHncAhq8ldnojAI)
* [Join Catswords OSS #welsonjs on Discord (discord.gg)](https://discord.gg/XKG5CjtXEj)

## Special channels
* [A paid consultation channel (m.expert.naver.com)](https://m.expert.naver.com/mobile/expert/product/detail?storeId=100051156&productId=100144540) is available for Korean<sup>(한국어)</sup> region.
