#!/bin/sh
protoc --python_out=python BridgeChat.proto
protoc --java_out=java BridgeChat.proto
protoc -oBridgeChat.proto.bin BridgeChat.proto
mono ../protobuf-net/ProtoGen/protogen.exe -i:BridgeChat.proto.bin -p:fixCase -p:asynchronous -p:detectMissing -o:Protocol.cs -ns:BridgeChat.Protocol
mv Protocol.cs csharp/BridgeChat.Protocol
