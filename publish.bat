@echo off

set SDKDIR=C:\Downloads\arise\arise\sdk
set JAVADIR=C:\Program Files\Eclipse Adoptium\jdk-21.0.8.9-hotspot
set KEYSTORE=C:\Downloads\arise\arise\arise.keystore

FOR /F "delims=" %%i IN ('op read "op://Personal/Arise KeyStore/password"') DO (
    SET KEYPASS=%%i
)

call dotnet build ^
    -t:InstallAndroidDependencies ^
    -p:AndroidSdkDirectory="%SDKDIR%" ^
    -p:JavaSdkDirectory="%JAVADIR%" ^
    -p:AcceptAndroidSdkLicenses=True

dotnet publish ^
    -o ./apks ^
    -c Release ^
    -p:AndroidSdkDirectory="%SDKDIR%" ^
    -p:JavaSdkDirectory="%JAVADIR%" ^
    -p:AndroidKeyStore=True ^
    -p:AndroidSigningKeyStore="%KEYSTORE%" ^
    -p:AndroidSigningStorePass="%KEYPASS%" ^
    -p:AndroidSigningKeyAlias="arise" ^
    -p:AndroidSigningKeyPass="%KEYPASS%"
