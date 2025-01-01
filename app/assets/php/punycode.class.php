<?php
/**
 * The MIT License (MIT)
 * 
 * Copyright (c) 2013 mk-j, zedwood.com
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

function_exists('mb_internal_encoding') or die('unsupported dependency, mbstring');

class Punycode
{
	const TMIN = 1;
	const TMAX = 26;
	const BASE = 36;
	const INITIAL_N = 128;
	const INITIAL_BIAS = 72;
	const DAMP = 700;
	const SKEW = 38;
	const DELIMITER = '-';

	//Punycode::::encodeHostName() corresponds to idna_toASCII('xärg.örg');
	public static function encodeHostName($hostname)
	{
		if (!self::is_valid_utf8($hostname))
		{
			return $hostname;//invalid
		}

		if (function_exists('idn_to_ascii') && 0) 
		{ 
			return idn_to_ascii($hostname);//php 5.3+
		}

		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");

		$pieces = explode(".", self::mb_strtolower($hostname) );
		$punycode_pieces = array();
		foreach($pieces as $piece)
		{
			if (preg_match("/[\x{80}-\x{FFFF}]/u", $piece))//is multi byte utf8
			{
				$punycode_pieces[] = "xn--".self::encode($piece);
			}
			else if (preg_match('/^[a-z\d][a-z\d-]{0,62}$/i', $piece) && !preg_match('/-$/', $piece) )//is valid ascii hostname
			{
				$punycode_pieces[] = $piece;
			}
			else
			{
				mb_internal_encoding($old_encoding);
				return $hostname;//invalid domain
			}
		}
		mb_internal_encoding($old_encoding);
		return implode(".", $punycode_pieces);
	}

	//Punycode::::decodeHostName() corresponds to idna_toUnicode('xn--xrg-9ka.xn--rg-eka');
	public static function decodeHostName($encoded_hostname)
	{
		if (!preg_match('/[a-z\d.-]{1,255}/', $encoded_hostname))
		{
			return false;
		}

		if (function_exists('idn_to_utf8') && 0) 
		{ 
			return idn_to_utf8($encoded_hostname); 
		}

		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");

		$pieces = explode(".", strtolower($encoded_hostname));
		foreach($pieces as $piece)
		{
			if (!preg_match('/^[a-z\d][a-z\d-]{0,62}$/i', $piece) || preg_match('/-$/', $piece) )
			{
				mb_internal_encoding($old_encoding);
				return $encoded_hostname;//invalid 
			}
			$punycode_pieces[] = strpos($piece, "xn--")===0 ? self::decode(substr($piece,4)) : $piece;
		}
		mb_internal_encoding($old_encoding);
		return implode(".", $punycode_pieces);
	}

	protected static function encode($input)
	{
		try
		{
			$n = self::INITIAL_N;
			$delta = 0;
			$bias = self::INITIAL_BIAS;
			$output='';
			$input_length = self::mb_strlen($input);

			$b=0;
			for($i=0; $i<$input_length; $i++)
			{
				$chr = self::mb_substr($input,$i,1);
				$c = self::uniord( $chr );//autoloaded class
				if ($c < self::INITIAL_N)
				{
					$output.= $chr;
					$b++;
				}
			}
			
			if ($b==$input_length)//no international chars to convert to punycode here
			{
				throw new Exception("PunycodeException.BAD_INPUT");
			}
			else if ($b>0)
			{
				$output.= self::DELIMITER;
			}

			$h = $b;
			while($h < $input_length)
			{
				$m = PHP_INT_MAX;

				// Find the minimum code point >= n
				for($i=0; $i<$input_length; $i++)
				{
					$chr = self::mb_substr($input,$i,1);
					$c = self::uniord( $chr );
					if ($c >= $n && $c < $m)
					{
						$m = $c;
					}
				}
				
				
				if (($m - $n) > (PHP_INT_MAX - $delta) / ($h+1))
				{
					throw new Exception("PunycodeException.OVERFLOW");
				}
				$delta = $delta + ($m - $n) * ($h + 1);
				$n = $m;
				

				for($j=0; $j<$input_length; $j++)
				{
					$chr = self::mb_substr($input,$j,1);
					$c = self::uniord( $chr );
					if ($c < $n)
					{
						$delta++;
						if (0==$delta)
						{
							throw new Exception("PunycodeException.OVERFLOW");
						}
					}
					
					if ($c == $n)
					{
						$q = $delta;
						for($k= self::BASE;; $k+=self::BASE)
						{
							$t=0;
							if ($k <= $bias)
							{
								$t= self::TMIN;
							} else if ($k >= $bias + self::TMAX) { 
								$t= self::TMAX;
							} else {
								$t = $k - $bias;
							}
							if ($q < $t)
							{
								break;
							}
							$output.= chr( self::digit2codepoint($t + ($q - $t) % (self::BASE - $t)) );
							$q = floor(  ($q-$t) / (self::BASE - $t) );//integer division
						}
						$output.= chr( self::digit2codepoint($q) );
						$bias = self::adapt($delta, $h+1, $h==$b);
						$delta=0;
						$h++;
					}
				}
				$delta++;
				$n++;
			}
		}
		catch (Exception $e)
		{
			error_log("[PUNYCODE] error ".$e->getMessage());
			return $input;
		}
		return $output;
	}

	protected static function decode($input)
	{
		try
		{
			$n = self::INITIAL_N;
			$i = 0;
			$bias = self::INITIAL_BIAS;
			$output = '';

			$d = self::rstrpos($input, self::DELIMITER);
			if ($d>0) {
				for($j=0; $j<$d; $j++) {
					$chr = self::mb_substr($input,$j,1);
					$c = self::uniord( $chr );
					if ($c>=self::INITIAL_N) {
						throw new Exception("PunycodeException.BAD_INPUT");
					}
					$output.=$chr;
				}
				$d++;
			} else {
				$d = 0;
			}

			$input_length = self::mb_strlen($input);
			while ($d < $input_length) {
				$oldi = $i;
				$w = 1;

				for($k= self::BASE;; $k += self::BASE) {
					if ($d == $input_length) {
						throw new Exception("PunycodeException.BAD_INPUT");
					}
					$chr = self::mb_substr($input,$d++,1);
					$c = self::uniord( $chr );
					$digit = self::codepoint2digit($c);
					if ($digit > (PHP_INT_MAX - $i) / $w) {
						throw new Exception("PunycodeException.OVERFLOW");
					}

					$i = $i + $digit * $w;

					$t=0;
					if ($k <= $bias) {
						$t = self::TMIN;
					} else if ($k >= $bias + self::TMAX) {
						$t = self::TMAX;
					} else {
						$t = $k - $bias;
					}
					if ($digit < $t) {
						break;
					}
					$w = $w * (self::BASE - $t);
				}
				$output_length = self::mb_strlen($output);

				$bias = self::adapt($i - $oldi, $output_length + 1, $oldi == 0);

				if ($i / ($output_length + 1) > PHP_INT_MAX - $n) {
					throw new Exception("PunycodeException.OVERFLOW");
				}
				$n = floor($n + $i / ($output_length + 1));
				$i = $i % ($output_length + 1);
				$output = self::mb_strinsert($output, self::utf8($n), $i);
				$i++;
			}
		}
		catch(Exception $e)
		{
			error_log("[PUNYCODE] error ".$e->getMessage());
			return $input;
		}
		return $output;
	}

//adapt patched from:
//https://github.com/takezoh/php-PunycodeEncoder/blob/master/punycode.php
	protected static function adapt($delta, $numpoints, $firsttime)
	{
		$delta = (int)($firsttime ? $delta / self::DAMP : $delta / 2);
		$delta += (int)($delta / $numpoints);
		$k = 0;
		while ($delta > (((self::BASE - self::TMIN) * self::TMAX) / 2)) {
			$delta = (int)($delta / (self::BASE - self::TMIN));
			$k += self::BASE;
		}
		return $k + (int)((self::BASE - self::TMIN + 1) * $delta / ($delta + self::SKEW));
	}

	protected static function digit2codepoint($d)
	{
		if ($d < 26) {
			// 0..25 : 'a'..'z'
			return $d + ord('a');
		} else if ($d < 36) {
			// 26..35 : '0'..'9';
			return $d - 26 + ord('0');
		} else {
			throw new Exception("PunycodeException.BAD_INPUT");
		}
	}

	protected static function codepoint2digit($c)
	{
		if ($c - ord('0') < 10) {
			// '0'..'9' : 26..35
			return $c - ord('0') + 26;
		} else if ($c - ord('a') < 26) {
			// 'a'..'z' : 0..25
			return $c - ord('a');
		} else {
			throw new Exception("PunycodeException.BAD_INPUT");
		}
	}

	protected static function rstrpos($haystack, $needle)
	{
		$pos = strpos (strrev($haystack), $needle);
		if ($pos === false)
			return false;
		return strlen ($haystack)-1 - $pos;
	}

	protected static function mb_strinsert($haystack, $needle, $position)
	{
		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");
		$r = mb_substr($haystack,0,$position).$needle.mb_substr($haystack,$position);
		mb_internal_encoding($old_encoding);
		return $r;
	}
	
	protected static function mb_substr($str,$start,$length)
	{
		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");
		$r = mb_substr($str,$start,$length);
		mb_internal_encoding($old_encoding);
		return $r;
	}

	protected static function mb_strlen($str)
	{
		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");
		$r = mb_strlen($str);
		mb_internal_encoding($old_encoding);
		return $r;
	}

	protected static function mb_strtolower($str)
	{
		$old_encoding = mb_internal_encoding();
		mb_internal_encoding("UTF-8");
		$r = mb_strtolower($str);
		mb_internal_encoding($old_encoding);
		return $r;
	}
	
	public static function uniord($c)//cousin of ord() but for unicode
	{
		$ord0 = ord($c[0]); if ($ord0>=0   && $ord0<=127) return $ord0;
		$ord1 = ord($c[1]); if ($ord0>=192 && $ord0<=223) return ($ord0-192)*64 + ($ord1-128);
		if ($ord0==0xed && ($ord1 & 0xa0) == 0xa0) return false; //code points, 0xd800 to 0xdfff
		$ord2 = ord($c[2]); if ($ord0>=224 && $ord0<=239) return ($ord0-224)*4096 + ($ord1-128)*64 + ($ord2-128);
		$ord3 = ord($c[3]); if ($ord0>=240 && $ord0<=247) return ($ord0-240)*262144 + ($ord1-128)*4096 + ($ord2-128)*64 + ($ord3-128);
		return false;
	}

	public static function utf8($num)//cousin of ascii() but for utf8
	{
		if($num<=0x7F)       return chr($num);
		if($num<=0x7FF)      return chr(($num>>6)+192).chr(($num&63)+128);
		if(0xd800<=$num && $num<=0xdfff) return '';//invalid block of utf8
		if($num<=0xFFFF)     return chr(($num>>12)+224).chr((($num>>6)&63)+128).chr(($num&63)+128);
		if($num<=0x10FFFF)   return chr(($num>>18)+240).chr((($num>>12)&63)+128).chr((($num>>6)&63)+128).chr(($num&63)+128);
		return '';
	}
	
	public static function is_valid_utf8($string)
	{
		for ($i=0, $ix=strlen($string); $i < $ix; $i++)
		{
			$c = ord($string[$i]);
			if ($c==0x09 || $c==0x0a || $c==0x0d || (0x20 <= $c && $c < 0x7e) ) $n = 0; # 0bbbbbbb
			else if (($c & 0xE0) == 0xC0) $n=1; # 110bbbbb
			else if ($c==0xed && (ord($string[$i+1]) & 0xa0)==0xa0) return false; //code points, 0xd800 to 0xdfff
			else if (($c & 0xF0) == 0xE0) $n=2; # 1110bbbb
			else if (($c & 0xF8) == 0xF0) $n=3; # 11110bbb
			//else if (($c & 0xFC) == 0xF8) $n=4; # 111110bb //byte 5, unnecessary in 4 byte UTF-8
			//else if (($c & 0xFE) == 0xFC) $n=5; # 1111110b //byte 6, unnecessary in 4 byte UTF-8
			else return false;
			for ($j=0; $j<$n; $j++) { // n bytes matching 10bbbbbb follow ?
				if ((++$i == $ix) || ((ord($string[$i]) & 0xC0) != 0x80))
					return false;
			}
		}
		return true;
	}

}


