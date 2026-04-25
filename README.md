# TLS PDF Downloader

TLS/Coursemos ubdoc 문서 뷰어의 페이지 이미지를 하나의 PDF로 저장하는 Windows용 도구입니다.

## 소개

TLS PDF Downloader는 사용자가 접근 권한을 가진 ubdoc 문서 URL을 입력하면 문서 메타데이터를 확인하고, 각 페이지 이미지를 순서대로 다운로드한 뒤 PDF 파일로 묶어 저장합니다.

## 사용 방법

1. [GitHub 릴리즈 페이지](https://github.com/Faze403/TLS-PDF-Downloader/releases/latest)에서 `UBDoc_GUI.exe`를 다운로드합니다.
2. `UBDoc_GUI.exe`를 실행합니다.
3. `ubdoc URL` 칸에 문서 뷰어 URL을 붙여넣습니다.
4. `PDF 저장 폴더`를 확인하거나 `찾기` 버튼으로 변경합니다.
5. PNG 원본 이미지를 보관해야 하면 `PNG 이미지 남기기`를 체크합니다.
6. `PDF 저장`을 누릅니다.

> 기본 저장 위치는 실행 파일이 있는 폴더의 `PDF` 폴더입니다.

## 주요 특징

- ubdoc 문서 URL을 PDF로 변환
- 동일한 파일명이 있을 때 `_1`, `_2` 형식으로 자동 이름 변경
- 변환 완료 후 임시 PNG 이미지 자동 삭제
- 필요시 PNG 이미지 보관 가능

## 실행 방법

```text
# 실행 파일 사용
최신 릴리즈에서 UBDoc_GUI.exe 다운로드 후 실행

# 소스에서 직접 빌드
UBDocGUI.cs 수정 후 아래 "소스에서 빌드" 명령 실행
```

실행 중에는 필요에 따라 아래 항목이 자동으로 생성됩니다.

```text
PDF\             변환된 PDF 저장 폴더
Logs\            실행 로그 폴더
settings.ini     마지막 PDF 저장 폴더 기록
*_images\        임시 PNG 이미지 폴더
```

`*_images` 폴더는 PDF 저장이 성공하면 기본적으로 삭제됩니다.

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

종료 코드가 `0`이면 기본 실행 파일 검사를 통과한 것입니다.

## FAQ

### **Q 1. 파이썬이나 별도 프로그램 설치가 필요한가요?**

A. 릴리즈 페이지에서 받은 `UBDoc_GUI.exe`를 실행하는 경우 별도 설치가 필요하지 않습니다. 소스에서 직접 빌드할 때는 Windows의 .NET Framework C# 컴파일러를 사용합니다.

### **Q 2. PDF는 어디에 저장되나요?**

A. 기본값은 실행 파일이 있는 폴더의 `PDF` 폴더입니다. 프로그램 화면에서 `찾기` 버튼을 눌러 다른 저장 폴더를 선택할 수 있습니다.

### **Q 3. 같은 이름의 PDF가 이미 있으면 덮어쓰나요?**

A. 덮어쓰지 않습니다. 같은 이름의 PDF가 있으면 `_1`, `_2`처럼 번호를 붙여 새 파일로 저장합니다.

### **Q 4. PNG 이미지도 따로 남길 수 있나요?**

A. 가능합니다. 실행 화면에서 `PNG 이미지 남기기`를 체크하면 변환에 사용한 PNG 이미지가 삭제되지 않습니다.

### **Q 5. 문서가 브라우저에서는 열리는데 저장이 실패해요.**

A. URL이 ubdoc 문서 뷰어 URL인지 확인하고, 저장 폴더에 쓰기 권한이 있는지 확인하세요. 서비스의 문서 뷰어 구조가 바뀐 경우에도 변환이 실패할 수 있습니다.

### **Q 6. 로그는 어디에서 확인하나요?**

A. 오류가 발생하면 실행 파일이 있는 폴더 아래 `Logs` 폴더를 확인하세요.

```text
Logs\last_run.log   마지막 실행 로그
Logs\gui_error.log  오류 로그
```

로그 파일은 삭제해도 됩니다. 다음 실행 때 필요하면 다시 생성됩니다.

## 주의사항

- 접근 권한이 있는 문서만 저장하세요.
- 저장 전 충분한 디스크 공간을 확보하세요.
- 서비스의 문서 뷰어 구조가 바뀌면 변환이 실패할 수 있습니다.
- 이 도구는 입력한 URL과 관련된 문서 이미지를 내려받아 로컬 PDF로 묶는 방식으로 동작합니다.
