#-*- coding: utf-8 -*-

# apploader.py
# WelsonJS app loader over gRPC protocol (gRPC proto 3) - Server
# https://github.com/gnh1201/welsonjs

import os
import sys
import win32com
import win32com.client
import pythoncom

sys.path.insert(0, os.path.dirname(__file__))

from concurrent import futures
import logging

import grpc
import WelsonAppLoader_pb2
import WelsonAppLoader_pb2_grpc

# get file path
workingDirectory = os.path.join(os.path.dirname(__file__), '../../..')
print('[*] Working Directory: ' + workingDirectory);

def runApp(appName):
    # Initalization for SubThread
    pythoncom.CoInitialize()

    # load scriptcontrol.js file
    f = open(workingDirectory + "/scriptcontrol.js", 'r')
    payload_code = f.read()

    # load JScript engine
    sc = win32com.client.Dispatch('MSScriptControl.ScriptControl') 
    sc.Language = "JScript"
    sc.AddCode(payload_code);
    sc.Run('setWorkingDirectory', workingDirectory);
    result = sc.Run('run', appName)

    # check a result
    print (result)

    # Return a result
    return result

class WelsonAppLoader(WelsonAppLoader_pb2_grpc.WelsonAppLoaderServicer):
    def Run(self, request, context):
        return WelsonAppLoader_pb2.AppResponse(responseText=runApp(request.appName))

def serve():
    port = "50051"
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=1))  # Only allow 1 worker
    WelsonAppLoader_pb2_grpc.add_WelsonAppLoaderServicer_to_server(WelsonAppLoader(), server)
    server.add_insecure_port("[::]:" + port)
    server.start()
    print("Server started, listening on " + port)
    server.wait_for_termination()

if __name__ == "__main__":
    logging.basicConfig()
    serve()
