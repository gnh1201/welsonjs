#-*- coding: utf-8 -*-

import sys

def main(args):
    if len(args) < 2:
        print("Insufficient arguments")
        sys.exit()
    
    encoded_domain = args[1].encode('idna').decode("utf-8")

    print(encoded_domain)

if __name__ == "__main__":
    main(sys.argv)
