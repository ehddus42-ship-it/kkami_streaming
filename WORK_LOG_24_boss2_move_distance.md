# 24. boss2 이동 거리 증가

- boss2의 `moveStepDistance`를 기존 `190 * 1.8`에서 `190 * 1.8 * 2`로 변경했다.
- boss1 이동 거리는 유지하고 boss2에만 적용했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
