#-*- coding: utf-8 -*-

import sys
import nmap
from optparse import OptionParser
import json

def main(args):
    parser = OptionParser()
    parser.add_option("-H", "--hosts", type="string", dest="hosts")
    (options, args) = parser.parse_args()

    nm = nmap.PortScanner(nmap_search_path=('bin/nmap-7.92/nmap',))
    result = nm.scan(hosts=options.hosts, arguments='-T5 -sV -p21-25,80,139,443,445,1883,2179,2323,3389,7547,8080,8443,8883')

    print(json.dumps(result))

if __name__ == "__main__":
    main(sys.argv)
