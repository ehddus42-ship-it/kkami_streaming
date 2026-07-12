# 33. boss4 대기 애니메이션 및 잠복 출력 순서 보정

- `vfx_boss4_idle.png`를 기존 보스 리소스 네이밍 규칙에 맞춰 추가했다.
- boss4 대기 상태에서 8프레임 idle 애니메이션을 반복 재생한다.
- 잠복 시에는 땅으로 들어가는 애니메이션을 끝까지 출력한 다음 본체를 투명화하고 타격 판정을 끈다.
- 대기, 잠복, 등장, 사망 시트는 모두 동일한 512x512 프레임 셀과 고정된 보스 RectTransform을 사용해 화면상 출력 크기가 달라지지 않게 했다.
- boss5은 그림자를 제거하고 완전 투명화가 끝난 후에만 이동하도록 순서를 변경했다.

## 변경 파일

- `Assets/GameKamiStreaming/Scripts/Runtime/BossPieceView.cs`
- `Assets/GameKamiStreaming/Scripts/Runtime/KkamiPrototypeGame.cs`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/effects.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/xlsx_export/res_vfx.csv`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_idle.png`
- `Assets/GameKamiStreaming/Resources/GameKamiStreaming/Sprites/vfx_boss4_idle.png.meta`
