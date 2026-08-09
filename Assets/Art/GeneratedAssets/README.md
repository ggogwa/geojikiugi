# 개별 스프라이트 에셋

합본 레퍼런스를 기반으로 재생성한 뒤 투명 배경 제거, 셀 분리, 여백 정규화 및 Unity Sprite 임포트를 완료한 에셋이다.

## 폴더 구성

- `Beggar/`: 거지 성장 5단계, 512×768
- `Estate/`: 부동산 6등급, 512×512
- `Heroes/`: 탱커·딜러·힐러 × 불·물·풀·무속성 12종, 512×512
- `Bosses/`: 스테이지 보스 10종, 512×512
- `Icons/`: 경제·전투·메뉴 아이콘 21종, 256×256
- `Effects/`: 전투 이펙트 8종, 512×512
- `Backgrounds/`: 골목·수중·옥상 세로 배경 3종
- `SourceSheets/`: 분리 전 원본 생성 시트

## 파일명 규칙

- 거지: `Beggar_Stage_01` ~ `Beggar_Stage_05`
- 건물: `Estate_01_Studio` ~ `Estate_06_Tower`
- 아군: `Hero_<Role>_<Element>`
  - Role: `Tank`, `Dealer`, `Healer`
  - Element: `Fire`, `Water`, `Grass`, `Neutral`
- 보스: `Boss_01` ~ `Boss_10`
- 아이콘: `Icon_<Purpose>`
- 이펙트: `Effect_<Purpose>`
- 배경: `Background_Alley`, `Background_Aquarium`, `Background_Rooftop`

## Unity Import Settings

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Single`
- Alpha Is Transparency: 캐릭터·건물·아이콘·이펙트 활성
- Generate Mip Maps: 비활성
- Filter Mode: Bilinear
- Compression: High Quality
