# idle-game 작업 규칙

## 프로젝트 성격

- Unity 프로젝트
- 현재는 초기 부트스트랩 단계
- Unity 프로젝트 파일이 아직 전부 커밋되지 않았을 수 있음

## 작업 원칙

- 작은 범위부터 수정
- 한 번에 한 가지 목적만 처리
- `.meta` 파일이 필요한 변경이면 같이 반영
- 기존 씬/프리팹 대량 수정은 초기 단계에서 피함

## 우선 수정 허용 범위

- `README.md`
- `Assets/Scripts/`
- `Packages/manifest.json`
- 문서와 설정 파일

## 주의 사항

- 아래 경로는 Git에 올리지 않음
  - `Library/`
  - `Temp/`
  - `Obj/`
  - `Logs/`
  - `UserSettings/`
- Unity 버전은 프로젝트 생성에 사용한 버전을 유지
- Unity 버전 변경이 필요하면 먼저 문서와 이유를 남김

## 금지 사항

- 이유 없이 렌더 파이프라인 변경
- 이유 없이 패키지 대량 추가
- 초기 기준점 없이 씬/프리팹 자동 대량 수정
- `.meta` 파일 누락

## 첫 실전 smoke 권장

- `README.md` 한 줄 추가
- `Assets/Scripts/SmokeTest.cs` 같은 작은 파일 추가

