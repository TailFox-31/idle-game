# idle-game

새 Unity 프로젝트 레포입니다.

## 현재 상태

- Git 초기 템플릿과 Unity 초기 프로젝트 파일이 반영된 상태
- 현재 추적 대상 경로
  - `Assets/`
  - `Packages/`
  - `ProjectSettings/`
  - `ProjectSettings/ProjectVersion.txt`
  - `*.meta`

## Unity 버전

- `6000.4.1f1`
- 기준 파일: `ProjectSettings/ProjectVersion.txt`

## Git 규칙

- 커밋 포함
  - `Assets/`
  - `Packages/`
  - `ProjectSettings/`
  - `*.meta`
- 커밋 제외
  - `Library/`
  - `Temp/`
  - `Obj/`
  - `Logs/`
  - `UserSettings/`

## 추천 초기 구조

```text
Assets/
  Scenes/
  Scripts/
  Art/
  Prefabs/
Packages/
ProjectSettings/
```

## 원격 작업자 첫 smoke 권장 범위

- `README.md` 1줄 수정
- `Assets/Scripts/` 아래 단일 스크립트 추가 또는 수정

## 메모

- 씬, 프리팹, 대형 바이너리 수정은 초반 smoke 단계에서는 피하는 편이 안전함
- 원격 작업 규칙은 `AGENTS.md` 참고
