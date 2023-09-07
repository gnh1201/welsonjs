#-*- coding: utf-8 -*-

# apploader_test.py
# WelsonJS app loader over gRPC protocol (gRPC proto 3) - Client
# https://github.com/gnh1201/welsonjs

from __future__ import print_function

import os
import sys
sys.path.insert(0, os.path.dirname(__file__))

from concurrent import futures
import logging

import grpc
import WelsonAppLoader_pb2
import WelsonAppLoader_pb2_grpc

def run():
    with grpc.insecure_channel("localhost:50051") as channel:
        stub = WelsonAppLoader_pb2_grpc.WelsonAppLoaderStub(channel)
        response = stub.Run(WelsonAppLoader_pb2.AppRequest(appName="extramath-test"))
    print("Response: " + response.responseText)

if __name__ == "__main__":
    logging.basicConfig()
    run()
