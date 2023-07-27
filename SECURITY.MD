# Security Note for WelsonJS

## Caution
This repository contains information on accessing Windows APIs and functions on the JavaScript runtime, along with recent case studies. While this can provide a flexible development environment for anyone, it can also be misused for malicious purposes. Please be aware that using this project to create abuse tools, such as a DoS attack, may result in legal punishment in your country. We encourage you to use this project only for creating web technology-based applications, like Electron, or legally permitted testing tools.

## Known use cases
WelsonJS is typically used for the following purposes:

  * Testing web accessibility and compliance, including adherence to W3C standards (WEB-ARIA, WCAG), national laws (ADA/DDA, GDPR), and other relevant regulations.
  * Exploring vulnerabilities of equipment within the local network.
  * Improving the availability of VPN or Proxy clients.
  * Building automation, CD/CI (Continuous Integration/Continuous Delivery), DevOps, and SecOps.
  * Asset evaluation (e.g. Get a purchase history from online shopping and delivery websites)
  * Online video streaming quality testing and improvement.

## Notes
  1. If you plan to use WelsonJS for a purpose other than those mentioned above, please contact us beforehand.
  2. If you are looking for ways to use WelsonJS more efficiently, referencing the [LOLBAS (Living Off The Land Binaries and Scripts)](https://lolbas-project.github.io/) list can be helpful.

## Guidelines for the use of online shopping and delivery websites
We are aware of cases where WelsonJS has been used for asset valuation to access websites of online shopping or delivery companies. This is a good use case, but there have been reports of website downtime caused by excessive concurrent requests. Please exercise caution and avoid excessive simultaneous executions.

## Report abuse
If you discover any instances of this project being misused, please report them.

  * abuse@catswords.net
