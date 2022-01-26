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
    result = nm.scan(hosts=options.hosts, arguments='-sV')

    print(json.dumps(result))

if __name__ == "__main__":
    main(sys.argv)
