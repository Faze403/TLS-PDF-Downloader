# TLS PDF Downloader

TLS/Coursemos ubdoc 문서 뷰어에서 페이지 이미지를 내려받아 하나의 PDF로 저장하는 Windows용 도구입니다.

이 도구는 사용자가 접근 권한을 가진 ubdoc 문서 URL을 입력하면 문서 메타데이터를 확인하고, 각 페이지 이미지를 순서대로 다운로드한 뒤 PDF 파일로 묶어 저장합니다.

## 주요 기능

- ubdoc 문서 URL을 PDF로 변환
- Windows GUI 제공
- PDF 저장 폴더 선택 및 마지막 저장 위치 기억
- 동일한 파일명이 있을 때 `_1`, `_2` 형식으로 자동 이름 변경
- 변환 완료 후 임시 PNG 이미지 자동 삭제
- 필요하면 PNG 이미지 보관 가능
- 별도 Python 스크립트 없이 단독 EXE로 실행 가능

## 실행 방법

배포 파일에서 `UBDoc_GUI.exe`를 실행합니다.

1. `ubdoc URL` 칸에 문서 뷰어 URL을 붙여넣습니다.
2. `PDF 저장 폴더`를 확인하거나 `찾기` 버튼으로 변경합니다.
3. PNG 원본 이미지를 보관해야 하면 `PNG 이미지 남기기`를 체크합니다.
4. `PDF 저장`을 누릅니다.

기본 저장 위치는 실행 파일이 있는 폴더의 `PDF` 폴더입니다.

## 배포 파일 구성

일반 사용자는 아래 파일만 있으면 됩니다.

```text
UBDoc_GUI.exe
```

소스 코드를 함께 배포하거나 직접 빌드할 때는 아래 파일을 포함합니다.

```text
README.md
UBDocGUI.cs
```

실행 중에는 필요에 따라 아래 항목이 자동으로 생성됩니다.

```text
PDF\             변환된 PDF 저장 폴더
Logs\            실행 로그 폴더
settings.ini     마지막 PDF 저장 폴더 기록
*_images\        임시 PNG 이미지 폴더
```

`*_images` 폴더는 PDF 저장이 성공하면 기본적으로 삭제됩니다.

## 명령줄 사용

GUI 없이 변환하려면 다음 형식으로 실행할 수 있습니다.

```powershell
.\UBDoc_GUI.exe --convert "<ubdoc URL>" "<PDF 저장 폴더>"
```

PNG 이미지를 삭제하지 않고 남기려면 `--keep-images` 옵션을 추가합니다.

```powershell
.\UBDoc_GUI.exe --convert "<ubdoc URL>" "<PDF 저장 폴더>" --keep-images
```

실행 파일이 정상적으로 시작 가능한지 확인하려면 아래 명령을 사용합니다.

```powershell
.\UBDoc_GUI.exe --self-test
```

종료 코드가 `0`이면 기본 실행 파일 검사를 통과한 것입니다.

## 소스에서 빌드

Windows에 포함된 .NET Framework C# 컴파일러로 빌드할 수 있습니다.

```powershell
& "C:\WINDOWS\Microsoft.NET\Framework64\v4.0.30319\csc.exe" `
  /nologo `
  /target:winexe `
  /platform:anycpu `
  /out:UBDoc_GUI.exe `
  /reference:System.Windows.Forms.dll `
  /reference:System.Drawing.dll `
  /reference:System.Web.Extensions.dll `
  /reference:System.Xml.Linq.dll `
  UBDocGUI.cs
```

빌드 후 다음 명령으로 실행 파일을 확인합니다.

```powershell
.\UBDoc_GUI.exe --self-test
```

## 문제 해결

### PDF 저장이 실패하는 경우

- URL이 ubdoc 문서 뷰어 URL인지 확인합니다.
- 브라우저에서 해당 문서가 정상적으로 열리는지 확인합니다.
- 문서 접근 권한이 있는 계정 또는 네트워크 환경인지 확인합니다.
- 저장 폴더에 쓰기 권한이 있는지 확인합니다.

### 로그 확인

오류가 발생하면 실행 파일이 있는 폴더 아래 `Logs` 폴더를 확인합니다.

```text
Logs\last_run.log   마지막 실행 로그
Logs\gui_error.log  오류 로그
```

로그 파일은 삭제해도 됩니다. 다음 실행 때 필요하면 다시 생성됩니다.

## 주의사항

- 접근 권한이 있는 문서만 저장하세요.
- 서비스의 문서 뷰어 구조가 바뀌면 변환이 실패할 수 있습니다.
- 이 도구는 입력한 URL과 관련된 문서 이미지를 내려받아 로컬 PDF로 묶는 방식으로 동작합니다.
