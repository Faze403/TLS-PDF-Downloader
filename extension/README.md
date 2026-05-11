# TLS PDF Downloader Chrome Extension

TLS/Coursemos ubdoc 페이지에 `PDF 저장` 버튼을 추가하는 Chrome 확장입니다.

## 설치

1. GitHub에서 이 저장소를 ZIP으로 내려받아 압축을 풉니다.
2. Chrome 주소창에 `chrome://extensions`를 입력합니다.
3. `개발자 모드`를 켭니다.
4. `압축해제된 확장 프로그램 로드`를 누르고 이 폴더의 `extension` 디렉터리를 선택합니다.

## 사용

1. Chrome에서 접근 권한이 있는 ubdoc 문서 페이지를 엽니다.
2. 페이지 오른쪽 위의 `PDF 저장` 버튼을 누릅니다.
3. 새 변환 탭에서 진행률을 확인합니다.
4. 완료되면 PDF가 Chrome 다운로드 폴더의 `TLS PDF Downloader` 폴더에 저장됩니다.

## 참고

- 이 확장은 `/local/ubdoc/` 경로의 페이지에서 동작합니다.
- Chrome 로그인 세션을 사용해 `worker.php`에 접근합니다.
- PNG 원본 보관, 로컬 로그 파일, 임의 로컬 폴더 저장은 지원하지 않습니다.
- 접근 권한이 있는 문서만 저장하세요.
