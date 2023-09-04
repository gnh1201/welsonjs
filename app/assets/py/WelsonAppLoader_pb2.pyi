from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Optional as _Optional

DESCRIPTOR: _descriptor.FileDescriptor

class AppRequest(_message.Message):
    __slots__ = ["appName"]
    APPNAME_FIELD_NUMBER: _ClassVar[int]
    appName: str
    def __init__(self, appName: _Optional[str] = ...) -> None: ...

class AppResponse(_message.Message):
    __slots__ = ["responseText"]
    RESPONSETEXT_FIELD_NUMBER: _ClassVar[int]
    responseText: str
    def __init__(self, responseText: _Optional[str] = ...) -> None: ...
