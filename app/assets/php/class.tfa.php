<?php
// https://github.com/dimamedia/PHP-Simple-TOTP-and-PubKey

class tfa {

	// RFC4648 Base32 alphabet
	private $alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

	function getOtp($key) {

		/* Base32 decoder */

		// Remove spaces from the given public key and converting to an array
		$key = str_split(str_replace(" ","",$key));
		
		$n = 0;
		$j = 0;
		$binary_key = "";

		// Decode public key's each character to base32 and save into binary chunks
		foreach($key as $char) {
			$n = $n << 5;
			$n = $n + stripos($this->alphabet, $char);
			$j += 5;
		
			if($j >= 8) {
				$j -= 8;
				$binary_key .= chr(($n & (0xFF << $j)) >> $j);
			}
		}
		/* End of Base32 decoder */

		// current unix time 30sec period as binary
		$binary_timestamp = pack('N*', 0) . pack('N*', floor(microtime(true)/30));
		// generate keyed hash
		$hash = hash_hmac('sha1', $binary_timestamp, $binary_key, true);
		
		// generate otp from hash
		$offset = ord($hash[19]) & 0xf;
		$otp = (
			((ord($hash[$offset+0]) & 0x7f) << 24 ) |
			((ord($hash[$offset+1]) & 0xff) << 16 ) |
			((ord($hash[$offset+2]) & 0xff) << 8 ) |
			(ord($hash[$offset+3]) & 0xff)
		) % pow(10, 6);
   
	   return $otp;
	}

	function getPubKey() {
		$alphabet = str_split($this->alphabet);
		$key = '';
		// generate 16 chars public key from Base32 alphabet
		for ($i = 0; $i < 16; $i++) $key .= $alphabet[mt_rand(0,31)];
		// split into 4x4 chunks for easy reading
		return implode(" ", str_split($key, 4));
	}

}

?>
