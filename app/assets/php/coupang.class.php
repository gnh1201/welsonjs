<?php
// coupang.class.php
// Coupang Product Search API integration class
// Namhyeon Go <gnh1201@gmail.com>
// https://github.com/gnh1201/welsonjs
// 
date_default_timezone_set("GMT+0");

class CoupangProductSearch {
    private $accessKey = "";
    private $secretKey = "";
    private $baseUrl = "https://api-gateway.coupang.com";

    private function generateSignature($method, $path, $query = "") {
        $datetime = (new \DateTime("now", new \DateTimeZone("GMT")))->format("ymd\THis\Z");
        $message = $datetime . $method . $path . $query;

        $signature = hash_hmac('sha256', $message, $this->secretKey);
        return [
            'authorization' => "CEA algorithm=HmacSHA256, access-key={$this->accessKey}, signed-date={$datetime}, signature={$signature}",
            'datetime' => $datetime
        ];
    }

    public function searchProducts($keyword, $limit = 10, $subId = null, $imageSize = null, $srpLinkOnly = false) {
        $path = "/v2/providers/affiliate_open_api/apis/openapi/products/search";
        $queryParams = http_build_query([
            'keyword' => $keyword,
            'limit' => $limit,
            'subId' => $subId,
            'imageSize' => $imageSize,
            'srpLinkOnly' => $srpLinkOnly
        ]);
        $fullPath = $path . '?' . $queryParams;
        $url = $this->baseUrl . $fullPath;

        $signatureData = $this->generateSignature("GET", $path, $queryParams);
        $authorization = $signatureData['authorization'];
        $datetime = $signatureData['datetime'];

        $headers = [
            "Content-Type: application/json;charset=UTF-8",
            "Authorization: $authorization"
        ];

        $curl = curl_init();
        curl_setopt($curl, CURLOPT_URL, $url);
        curl_setopt($curl, CURLOPT_CUSTOMREQUEST, "GET");
        curl_setopt($curl, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($curl, CURLOPT_RETURNTRANSFER, true);

        $response = curl_exec($curl);
        $httpCode = curl_getinfo($curl, CURLINFO_HTTP_CODE);

        curl_close($curl);

        if ($httpCode === 200) {
            return json_decode($response, true);
        } else {
            try {
                return json_decode($response, true);
            } catch (Exception $e) {
                return [
                    "status" => $httpCode,
                    "message" => $e->getMessage()
                ];
            }
        }
    }
}
