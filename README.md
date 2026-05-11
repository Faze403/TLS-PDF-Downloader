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
아래 "소스에서 빌드" 명령 실행
```

## Chrome 확장 설치/사용 방법

Chrome 확장은 이 저장소의 `extension` 폴더에 들어 있습니다.

```text
# GitHub ZIP 배포 사용
저장소 ZIP 다운로드 후 압축 해제
chrome://extensions 접속
개발자 모드 활성화
압축해제된 확장 프로그램 로드
압축을 푼 폴더 안의 extension 폴더 선택
```

사용 방법은 다음과 같습니다.

1. Chrome에서 접근 권한이 있는 ubdoc 문서 페이지를 엽니다.
2. 페이지 오른쪽 위의 `PDF 저장` 버튼을 누릅니다.
3. 새 변환 탭에서 진행률을 확인합니다.
4. 완료되면 PDF가 Chrome 다운로드 폴더의 `TLS PDF Downloader` 폴더에 저장됩니다.

버튼은 `/local/ubdoc/` 경로의 페이지에서 표시됩니다. 버튼이 보이지 않으면 현재 문서 URL에 `/local/ubdoc/`가 포함되어 있는지 확인하세요.

Chrome 확장 버전은 PDF 저장만 지원합니다. PNG 원본 보관, 로컬 로그 파일, 임의 로컬 폴더 저장 기능은 Windows 실행 파일 버전에서만 제공합니다.

Windows 실행 파일 실행 중에는 필요에 따라 아래 항목이 자동으로 생성됩니다.

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
  /win32icon:Assets\TlsPdfDownloader.ico `
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

## 주의사항

- 접근 권한이 있는 문서만 저장하세요.
- 저장 전 충분한 디스크 공간을 확보하세요.
- 서비스의 문서 뷰어 구조가 바뀌면 변환이 실패할 수 있습니다.
- 이 도구는 입력한 URL과 관련된 문서 이미지를 내려받아 로컬 PDF로 묶는 방식으로 동작합니다.
