## WelsonJS-WebRequest.ps1
이 스크립트를 사용하는 몇 가지 예제를 제공합니다. 스크립트에는 여러 옵션이 있으며 이를 활용하여 다양한 HTTP 요청을 수행할 수 있습니다.

1. **GET 요청:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1"
    ```

2. **POST 요청:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/posts" -method "POST" -data '{"title":"foo","body":"bar","userId":1}' -headers @("Content-Type: application/json")
    ```

3. **GET 요청 및 결과를 파일로 저장:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1" -outputFile "output.txt"
    ```

4. **GET 요청 및 SSL 인증서 검증 무시:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1" -insecure
    ```

5. **GET 요청 및 HTTP 프록시 사용:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1" -proxy "http://proxyserver:8080"
    ```

6. **GET 요청 및 여러 헤더 전달:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1" -headers @("Authorization: Bearer YourToken", "CustomHeader: CustomValue")
    ```

7. **GET 요청 및 SOCKS5 프록시 사용:**
    ```powershell
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/todos/1" -proxy "socks5://proxyserver:1080"
    ```

8. **POST 요청 및 파일에서 데이터 읽기:**
    ```powershell
    $dataFromFile = Get-Content -Path "data.json" -Raw
    .\WelsonJS-WebRequest.ps1 -url "https://welsonjs.catswords.net/posts" -method "POST" -data $dataFromFile -headers @("Content-Type: application/json")
    ```

이러한 예제를 통해 다양한 HTTP 요청을 스크립트를 사용하여 처리할 수 있습니다. 필요에 따라 옵션을 추가하거나 수정하여 사용하실 수 있습니다.