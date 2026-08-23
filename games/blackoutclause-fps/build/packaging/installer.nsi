; BlackoutClause Windows Installer (NSIS)
; Usage: makensis /DVERSION=1.0.0 installer.nsi

!include "MUI2.nsh"
!include "x64.nsh"

Name "BlackoutClause"
OutFile "BlackoutClause-Setup-${VERSION}.exe"
InstallDir "$PROGRAMFILES64\BlackoutClause"
InstallDirRegKey HKLM "Software\BlackoutClause" "Install_Dir"

RequestExecutionLevel admin
ShowInstDetails show

!define MUI_ABORTWARNING
!define MUI_ICON "${NSISDIR}\Contrib\Graphics\Icons\modern-install.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

Section "MainSection" SEC01
  SetOutPath "$INSTDIR"
  
  ; Copy all files from build output
  File /r "artifacts\client\win-x64\*.*"
  
  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  
  ; Registry entries
  WriteRegStr HKLM "Software\BlackoutClause" "Install_Dir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "DisplayName" "BlackoutClause"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "DisplayVersion" "${VERSION}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "Publisher" "BlackoutClause Team"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "NoModify" "1"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "NoRepair" "1"
  
  ; Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\BlackoutClause"
  CreateShortCut "$SMPROGRAMS\BlackoutClause\BlackoutClause.lnk" "$INSTDIR\BlackoutClause.Client.exe"
  CreateShortCut "$SMPROGRAMS\BlackoutClause\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
  
  ; Desktop shortcut
  CreateShortCut "$DESKTOP\BlackoutClause.lnk" "$INSTDIR\BlackoutClause.Client.exe"
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\*.*"
  RMDir /r "$INSTDIR"
  
  Delete "$SMPROGRAMS\BlackoutClause\*.*"
  RMDir "$SMPROGRAMS\BlackoutClause"
  
  Delete "$DESKTOP\BlackoutClause.lnk"
  
  DeleteRegKey HKLM "Software\BlackoutClause"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause"
SectionEnd

Function .onInit
  ; Check for existing installation
  ReadRegStr $0 HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\BlackoutClause" "UninstallString"
  StrCmp $0 "" 0 +3
  MessageBox MB_YESNO "BlackoutClause is already installed. Do you want to uninstall it first?" IDYES +2
  Abort
  ExecWait '"$0" _?=$INSTDIR'
FunctionEnd