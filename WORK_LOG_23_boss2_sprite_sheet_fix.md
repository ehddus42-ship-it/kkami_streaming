# 23. boss2 시트 적용 및 흰 화면 출력 수정

- 전달받은 `vfx_boss2_01.png`, `vfx_boss2_02.png`를 `Resources/GameKamiStreaming/Sprites` 경로에 추가했다.
- boss2 이동 애니메이션 `vfx_boss2_01`의 프레임 수를 실제 가로 시트에 맞춰 12프레임으로 수정했다.
- 넓은 가로형 스프라이트 시트는 한 줄 프레임으로 분할되도록 시트 분할 로직을 보정했다.
- boss2 idle 이미지가 없을 때 이동 시트 첫 프레임을 사용하고, 그래도 없으면 boss1 이미지를 임시 폴백으로 쓰도록 안전장치를 추가했다.
- 시트가 없을 때 기존 프레임 폴더와 fallback 프레임 폴더를 순서대로 찾도록 보강했다.

## 검증

- `dotnet build kkami_streaming.slnx`
- 경고 0개 / 오류 0개

## 관련 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss2_01.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss2_02.png`
