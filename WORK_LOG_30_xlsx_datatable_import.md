# WORK LOG 30 - XLSX 데이터테이블 반영

## 요청
- `(프로토타입까지)20260708_kkami_까미스트리밍데이터테이블_백승오_ver02.xlsx` 내용을 기준으로 게임 데이터를 갱신.
- XLSX는 CSV로 변환해서 사용.
- 기존 게임 규칙은 유지하고 세부 데이터만 갱신.
- `vfx_boss4_1_1`은 엑셀에 없지만 보스4 이동 구조상 필요한 예외 파일로 유지.
- 채팅 시트는 아직 화면 기능에 연결하지 않더라도 이후 사용을 위해 준비.
- 이미지 파일명은 XLSX 내 이름 기준으로 맞추고, 없는 이미지는 `C:\Users\user\Desktop\새 폴더`에서 보충.

## 작업 내용
1. 엑셀 원본을 `SourceData/kkami_datatable_ver02.xlsx`로 보관.
2. `Tools/export_xlsx_datatables.py`를 추가해 엑셀 전체 시트를 CSV로 변환.
   - 원본 시트 CSV: `Assets/GameKamiStreaming/Resources/GameKamiStreaming/DataTables/xlsx_export/`
   - 게임 사용 CSV: `resource.csv`, `piece.csv`, `stage.csv`, `skilltree.csv`, `chat.csv`, `effects.csv`
3. 엑셀 기준으로 기물, 자원, 스테이지, 보스, 스킬트리, 채팅 데이터를 재생성.
   - 스테이지 제한 시간은 기존 규칙대로 30초 유지.
   - 스테이지 10/20/30/40/50 보스 ID는 엑셀 기준 `30001`~`30005`로 반영.
   - boss3, boss5는 데이터만 준비하고 아직 구현/이미지가 없으므로 화면에 흰 이미지가 뜨지 않도록 이미지 참조를 비워 둠.
4. `Tools/sync_xlsx_image_names.py`를 추가해 기존 이미지와 `Desktop\새 폴더` 이미지를 XLSX 리소스명 기준으로 복사.
5. 복사된 새 PNG가 Unity에서 바로 사용할 수 있도록 기존 import 설정 기준의 `.meta` 파일을 생성.
6. `img_manager_01`, `vfx_boss1_01`, `vfx_boss1_02`, `vfx_kkami_01`, `vfx_boss4_1_1` 등 누락 리소스를 보충.
7. 런타임 데이터 모델을 확장.
   - `StageRow.imageId` 추가.
   - `SkillTreeRow.upAmount`를 float로 변경.
   - `ChatRow` 및 `KkamiTableDatabase.Chats` 추가.
8. 보스 데이터 ID를 엑셀 기준으로 맞춤.
   - boss1: `30001`
   - boss2: `30002`
   - boss4: `30004`
9. 보스4 등장 예외 애니메이션은 `vfx_boss4_1_1`을 계속 사용하도록 유지.

## 검증
- CSV 이미지 참조 누락 확인: 누락 없음.
- `dotnet build kkami_streaming.slnx`: 성공.
  - 경고 0개
  - 오류 0개
