# WelsonJS에 함께 기여해 주세요

## Windows에서 JavaScript를 더 오래, 더 넓게

**WelsonJS는 Windows 기반의 산업용 JavaScript 실행 환경을 확장하고 현대화하는 오픈소스 프로젝트입니다.**

Windows에는 오랫동안 다양한 업무용·산업용 소프트웨어가 사용되어 왔습니다. 새로운 기술이 등장하더라도 기존 시스템과 장비, 데이터, 업무 환경을 한 번에 교체하기는 어렵습니다.

WelsonJS는 이러한 환경에서 **JavaScript를 이용해 기존 Windows 시스템과 새로운 기술을 연결하고, 오래된 실행 환경을 새로운 방식으로 활용할 수 있도록 하는 것**을 목표로 합니다.

동시에 WelsonJS에서 다루는 **Runtime 기술 자체는 Windows에만 한정될 필요가 없습니다.** 다양한 운영체제와 하드웨어 환경에서 JavaScript 실행 환경을 활용할 수 있는 가능성을 열어두고 있으며, Linux와 모바일 환경 등으로의 확장, 서로 다른 플랫폼에서 활용할 수 있는 공통된 Runtime을 만들어가는 방향에도 관심을 가지고 있습니다.

## 이런 경험이 있다면 재미있을 수 있습니다

WelsonJS는 특정 기술이나 경력을 요구하는 프로젝트가 아닙니다. Windows와 JavaScript를 다뤄본 경험부터 산업 현장의 장비나 레거시 시스템을 다뤄본 경험까지, 서로 다른 배경의 기여를 환영합니다.

예를 들어 다음과 같은 경험이나 관심이 프로젝트의 여러 영역에서 활용될 수 있습니다.

* Windows Desktop Application 개발
* JavaScript 또는 Script Runtime 개발
* C#, .NET Framework 등을 활용한 Windows 개발
* WinForms 또는 WPF 기반 애플리케이션 개발
* Windows API 및 운영체제 기능 활용
* RDBMS, NoSQL 등 로컬 데이터베이스 활용
* 오프라인 환경에서의 데이터 관리 및 동기화
* REST API 또는 Socket을 이용한 시스템 연동
* USB 및 Serial 통신
* RS-232 / RS-485 기반 장비 연동
* Ethernet / TCP/IP 기반 시스템 연동
* Modbus RTU / Modbus TCP 등 산업용 프로토콜
* OPC UA, MQTT 등 산업용 통신 및 메시징
* PLC 또는 각종 산업용 장비 연동
* 프린터, 카드 리더기, 바코드 수신기 등의 입출력 장치 연동
* POS 시스템 또는 유사한 업무용 단말 시스템 개발
* 레거시 시스템 유지보수 및 현대화
* 대용량 데이터 처리 및 성능 개선
* 트래픽 급상승이나 고립·폐쇄 네트워크 환경을 고려한 시스템 설계 및 개발
* Linux, Android, iOS 등 다양한 운영체제 및 플랫폼 개발
* GitHub를 통한 코드 리뷰 및 오픈소스 협업

**모든 항목을 경험할 필요는 없습니다.**

특정 분야에 대한 경험이나 관심만으로도 충분히 의미 있는 기여를 할 수 있습니다. 코드 작성뿐 아니라 **문제 제보, 테스트, 문서 작성, 예제 제작, 성능 측정, 버그 수정, 새로운 아이디어 제안**도 중요한 오픈소스 기여입니다.

## WelsonJS가 다루는 문제

WelsonJS는 특정 산업이나 하나의 사용 사례에 한정되지 않은 다양한 문제를 다룹니다.

### 레거시 시스템과 현대화

오랫동안 운영되어 온 Windows 시스템을 무조건 교체하는 대신, 기존 환경을 유지하면서 새로운 기능과 기술을 어떻게 추가할 수 있을지 고민합니다.

### 로컬 데이터와 오프라인 환경

네트워크에 항상 의존할 수 없는 환경에서 데이터를 안정적으로 저장하고 처리하며, 필요한 경우 서버와 동기화할 수 있는 방법을 다룹니다.

### 제한된 네트워크 환경

네트워크가 제한되거나 고립·폐쇄된 환경에서도 애플리케이션과 시스템을 안정적으로 운영할 수 있는 방법을 고민합니다.

### Windows 시스템과 외부 장비의 연결

**USB, RS-232, RS-485, Ethernet 등의 인터페이스와 Serial, TCP/IP, HTTP, Modbus RTU/TCP, OPC UA, MQTT 등의 통신 방식 및 프로토콜**을 이용하여 다양한 시스템과 장비를 연결합니다.

PLC와 같은 제어장치부터 **프린터 등의 출력장치, 카드 리더기와 바코드 수신기 등의 입력장치**까지 Windows 애플리케이션에서 다양한 장비를 활용할 수 있는 환경을 만들어갑니다.

### 다양한 Windows 환경

서로 다른 세대와 구성의 Windows 환경에서도 소프트웨어를 사용할 수 있도록 호환성과 안정성을 고민합니다.

### Runtime과 플랫폼의 확장

WelsonJS에서 축적되는 Runtime 기술과 경험을 특정 운영체제에만 한정하지 않고, **Linux, 모바일 환경 등 다양한 운영체제와 하드웨어로 확장할 수 있는 가능성**을 함께 탐색합니다.

플랫폼마다 다른 실행 환경과 시스템 API를 어떻게 연결할지, 공통된 Runtime 구조를 어떻게 만들 수 있을지, Windows에서 축적한 경험을 다른 플랫폼에서도 어떻게 활용할 수 있을지 고민합니다.

### 성능과 안정성

대용량 데이터나 예상하지 못한 사용 환경에서도 안정적으로 동작할 수 있도록 성능과 자원 사용, 예외 상황 등을 살펴봅니다.

## 이런 분들과 함께하고 싶습니다

새로운 기술만을 사용하는 것보다 **기존 기술을 이해하고 새로운 기술과 연결하는 과정**에 흥미를 느끼는 분.

화려한 최신 기술보다는 실제 현장에서 오랫동안 사용되어 온 시스템이 왜 그렇게 만들어졌는지 살펴보는 것을 좋아하는 분.

Windows라는 플랫폼을 깊이 파고들어 보고 싶은 분.

JavaScript를 웹 브라우저 밖에서 활용하는 방법에 관심이 있는 분.

산업용 장비, 주변기기, 로컬 시스템과 소프트웨어가 연결되는 과정에 관심이 있는 분.

**POS와 같은 업무용 단말 시스템을 개발하거나 운영해 본 경험**, 또는 **고립·폐쇄 네트워크와 같은 제한된 환경을 고려하여 시스템을 설계해 본 경험**도 WelsonJS에서 활용할 수 있습니다.

또한 특정 운영체제에 한정되지 않고 **Runtime 자체를 연구하고, Linux나 모바일과 같은 다른 플랫폼으로 확장하는 것에 관심이 있는 분**도 함께할 수 있습니다.

그리고 무엇보다 **직접 코드를 살펴보고, 실행해 보고, 문제를 발견하고, 해결 방법을 함께 만들어가는 오픈소스 개발 방식**을 좋아하는 분을 환영합니다.

## 작은 기여부터 시작할 수 있습니다

WelsonJS에 참여하기 위해 큰 기능을 처음부터 구현할 필요는 없습니다.

작은 버그 하나를 수정하거나, 특정 Windows 환경에서 테스트하거나, 예제를 추가하거나, 문서의 잘못된 부분을 고치는 것부터 시작할 수 있습니다.

새로운 플랫폼에서 실행해 보거나, 다른 운영체제에서 Runtime을 실험해 보는 것 역시 의미 있는 기여가 될 수 있습니다.

서로 다른 환경에서 사용해 본 경험 자체가 프로젝트에 도움이 되기도 합니다.

## 개발자 지원

현재 WelsonJS는 **정식 고용 형태의 참여를 지원하고 있지는 않습니다.**

다만 프로젝트에 의미 있는 기여를 해주시는 개발자에게는 **버그 바운티 또는 이와 유사한 형태의 개발자 지원**을 제공할 수 있습니다.

기여 방식이나 지원 형태에 대해 별도로 협의하고 싶다면 언제든지 제안해 주세요. 프로젝트의 상황과 기여 내용에 따라 **지원 범위와 방법을 유연하게 협의할 수 있습니다.**

**Windows와 JavaScript를 함께 다루는 것이 재미있다면, 또는 JavaScript Runtime의 가능성을 더 넓혀보고 싶다면, 그것만으로도 좋은 출발점입니다.**

WelsonJS를 함께 발전시켜 주세요.



# Join WelsonJS

## Making JavaScript Run Longer, and Go Further on Windows

**WelsonJS is an open-source project focused on extending and modernizing an industrial JavaScript runtime environment for Windows.**

Windows has been used for decades in a wide range of business and industrial environments. Even when new technologies become available, replacing existing systems, devices, data, and workflows all at once is often not practical.

WelsonJS aims to **connect existing Windows systems with newer technologies through JavaScript, and make long-standing execution environments useful in new ways.**

At the same time, the **runtime technology behind WelsonJS does not have to be limited to Windows.** We are interested in exploring how JavaScript runtime environments can be extended to different operating systems and hardware platforms, including Linux and mobile environments, as well as developing runtime technologies that can be shared across platforms.

## Your Experience May Be a Great Fit

WelsonJS is not a project that requires a specific background or career path. We welcome contributors with different experiences, from Windows and JavaScript development to industrial equipment and legacy systems.

Experience or interest in any of the following can be useful in different areas of the project:

* Windows desktop application development
* JavaScript or script runtime development
* Windows development with C# or .NET Framework
* WinForms or WPF application development
* Windows APIs and operating system features
* RDBMS, NoSQL, and local databases
* Offline data management and synchronization
* REST APIs or socket-based system integration
* USB and serial communication
* RS-232 / RS-485 device integration
* Ethernet / TCP/IP-based system integration
* Industrial protocols such as Modbus RTU / Modbus TCP
* Industrial communication and messaging such as OPC UA and MQTT
* PLCs and other industrial equipment
* Input and output devices such as printers, card readers, and barcode readers
* POS systems or similar business terminal applications
* Legacy system maintenance and modernization
* Large-scale data processing and performance optimization
* System design and development for traffic spikes or isolated and closed-network environments
* Development for Linux, Android, iOS, and other platforms
* Code review and open-source collaboration through GitHub

**You do not need experience in all of these areas.**

Experience or interest in just one area can be enough to make a meaningful contribution. Contributions are not limited to writing code. **Bug reports, testing, documentation, examples, benchmarks, bug fixes, and new ideas** are all valuable contributions to WelsonJS.

## Problems WelsonJS Explores

WelsonJS is not limited to a single industry or use case. We work on a range of problems that arise when modern software meets long-running systems and real-world environments.

### Legacy Systems and Modernization

Instead of replacing long-running Windows systems outright, we explore how existing environments can be preserved while introducing new technologies and capabilities.

### Local Data and Offline Environments

We work on ways to reliably store and process data in environments that cannot always depend on network connectivity, including synchronization with remote systems when connectivity is available.

### Restricted Network Environments

We explore how applications and systems can remain useful and maintainable in environments with limited connectivity, including **isolated and closed networks**.

### Connecting Windows Systems with External Devices

We work with **USB, RS-232, RS-485, Ethernet, and other interfaces**, as well as communication methods and protocols such as **Serial, TCP/IP, HTTP, Modbus RTU/TCP, OPC UA, and MQTT**.

This includes connecting Windows applications with a wide range of equipment, from **PLCs and other controllers to output devices such as printers and input devices such as card readers and barcode readers**.

### Supporting Diverse Windows Environments

We care about compatibility and stability across different generations and configurations of Windows.

### Runtime and Platform Expansion

We are interested in extending the runtime technologies developed through WelsonJS beyond a single operating system.

This includes exploring **Linux, mobile platforms, and other operating systems and hardware environments**, as well as investigating how different system APIs and execution environments can be connected through a common runtime architecture.

We are also interested in how ideas and experience gained from Windows can be applied to other platforms.

### Performance and Reliability

We consider performance, resource usage, and unexpected operating conditions so that applications can remain reliable under a wide range of real-world environments.

## Who We'd Like to Build With

People who enjoy **understanding existing technologies and connecting them with new ones**, rather than only working with the latest technology.

People who are curious about why systems that have been used in the real world for many years were built the way they were.

People who want to explore Windows as a platform.

People who are interested in using JavaScript beyond the web browser.

People who enjoy understanding how industrial equipment, peripherals, local systems, and software work together.

Experience developing or operating **POS and other business terminal systems**, or designing systems for **isolated and closed-network environments**, can be particularly relevant to the kinds of problems WelsonJS explores.

We are also interested in people who want to explore **runtime technology itself and its potential across platforms such as Linux and mobile operating systems**.

Above all, we welcome people who enjoy **reading code, running experiments, finding problems, and working together to build solutions** through open-source development.

## Start Small

You do not need to implement a major feature to get started with WelsonJS.

You can begin by fixing a small bug, testing the project on a particular Windows environment, adding an example, or improving the documentation.

Trying WelsonJS on a new platform or experimenting with the runtime on another operating system can also be a meaningful contribution.

Your experience with different environments can itself become valuable knowledge for the project.

**If you enjoy working with Windows and JavaScript, or want to explore how far JavaScript runtime technology can go, that is already a good place to start.**

Join us in building WelsonJS.

## Developer Support

WelsonJS **does not currently offer formal employment or hiring arrangements**.

However, we may be able to provide **bug bounties or other forms of developer support** for meaningful contributions to the project.

If you would like to discuss a particular contribution or support arrangement, feel free to reach out at any time. **The scope and form of support can be discussed and negotiated depending on the contribution and the circumstances of the project.**
