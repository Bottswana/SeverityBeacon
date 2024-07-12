#!/bin/bash
sign_cert="74E1F259E1096FC80D85C8488E47E670B316540C"
credential_profile="AppleDev-Bottswana55-PylonOne"


cd SeverityBeacon
rm -rf macos-build > /dev/null
mkdir macos-build

echo Resign the native dylibs
dotnet build -c Release -r osx-arm64
dotnet build -c Release -r osx-amd64
codesign --force --verbose --timestamp --sign $sign_cert bin/Release/net8.0/runtimes/osx-arm64/native/libSystem.IO.Ports.Native.dylib
codesign --force --verbose --timestamp --sign $sign_cert bin/Release/net8.0/runtimes/osx-x64/native/libSystem.IO.Ports.Native.dylib

echo Build arm64
dotnet publish -c Release -r osx-arm64

echo Build amd64
dotnet publish -c Release -r osx-amd64

echo Build universal binary
lipo -create -output macos-build/SeverityBeacon bin/Release/net8.0/osx-arm64/publish/SeverityBeacon bin/Release/net8.0/osx-x64/publish/SeverityBeacon
chmod +x macos-build/SeverityBeacon

echo Sign Binary
codesign --force --verbose --timestamp --sign $sign_cert --options=runtime --entitlements ../entitlements.plist macos-build/SeverityBeacon

echo Notorise Binary
ditto -c --sequesterRsrc -k -V macos-build/SeverityBeacon macos-build/SeverityBeacon.zip
xcrun notarytool submit macos-build/SeverityBeacon.zip --wait --keychain-profile $credential_profile