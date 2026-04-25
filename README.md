# KKU ubdoc PNG to PDF

KKU TLS ubdoc 문서 뷰어에서 불러오는 `1.png`, `2.png` 형태의 페이지 이미지를 내려받아 하나의 PDF로 저장하는 Windows EXE 도구입니다.

## 사용법

`UBDoc_GUI.exe`를 더블클릭합니다.

1. `ubdoc URL` 칸에 문서 URL을 붙여넣습니다.
2. `PDF 저장 폴더`를 확인하거나 `찾기`로 변경합니다.
3. PNG 이미지를 남기고 싶으면 `PNG 이미지 남기기`를 체크합니다.
4. `PDF 저장`을 누릅니다.

기본 동작:

- PDF는 `PDF` 폴더에 저장됩니다.
- 저장 폴더는 마지막으로 사용한 위치를 기억하고 다음 실행 때 다시 표시합니다.
- 처음 실행할 때 URL 칸은 비어 있습니다.
- 같은 이름의 PDF가 이미 있으면 `_1`, `_2`처럼 번호를 붙여 새 파일로 저장합니다.
- PDF 저장이 성공하면 변환에 사용한 PNG 이미지와 임시 이미지 폴더는 자동 삭제됩니다.
- `PNG 이미지 남기기`를 체크하면 임시 PNG 이미지가 삭제되지 않습니다.

## 필요한 파일

실행만 할 때:

- `UBDoc_GUI.exe`

다시 수정하거나 빌드할 때:

- `UBDoc_GUI.exe`
- `UBDocGUI.cs`

`UBDoc_GUI.exe`는 단독 실행형이므로 Python 파일이나 별도 스크립트가 필요 없습니다.

## 다시 빌드

`UBDocGUI.cs`를 수정한 뒤 현재 폴더에서 아래 명령을 실행합니다.

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

빌드 확인:

```powershell
.\UBDoc_GUI.exe --self-test
```

`ExitCode=0`이면 기본 실행 파일 검사에 성공한 것입니다.

## 로그

오류가 발생하면 `Logs` 폴더에 로그 파일이 생길 수 있습니다.

- `Logs\last_run.log`: 마지막 실행 로그
- `Logs\gui_error.log`: 오류 로그

로그 파일은 삭제해도 됩니다. 다음 실행 때 필요하면 다시 생성됩니다.
