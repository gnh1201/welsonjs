@echo off

..\..\..\bin\python-3.10.2-embed\win32\python -m grpc_tools.protoc -I../protos --python_out=. --pyi_out=. --grpc_python_out=. ../protos/WelsonAppLoader.proto