# 28. 전체 보스 사망 애니메이션 재생 시간 증가

- `BossPieceView`의 공통 사망 프레임 시간 `DeathFrameSeconds`를 기존 `0.055f`에서 `0.055f * 1.5f`로 변경했다.
- boss1, boss2, boss4 등 `BossPieceView`를 사용하는 모든 보스의 사망 애니메이션 재생 시간이 동일하게 1.5배 길어진다.
- 중단 중 일부 들어간 boss2 전용 사망 배율 필드는 제거했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
