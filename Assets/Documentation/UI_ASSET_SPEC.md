# 거지 키우기 UI 아트 에셋 명세

기준 화면은 390×844 세로형이며, 원화 제작 기준 해상도는 1080×1920이다. Windows 실행 파일도 같은 세로 비율과 UI 안전 영역을 사용한다.

## 적용 완료 에셋

### 개복치 성장 스프라이트

- 파일: `Resources/SunfishGrowth/Sunfish_Stage_01_Final.png` ~ `Sunfish_Stage_10_Final.png`
- 규격: 512×512 PNG, 투명 배경, 단일 개체, 사방 최소 12% 안전 여백
- 표시 규칙: 모든 레벨을 같은 RectTransform 크기와 `preserveAspect`로 표시한다.
- 임포트: Sprite/Single, Alpha Is Transparency, Mipmap Off, Wrap Clamp

### 최고 보유 부동산 배경

- 파일: `Resources/GameArt/Backgrounds/HomeProgress/Background_Home_00.png` ~ `Background_Home_06.png`
- 단계: 거리, 원룸, 소형 주택, 상가, 오피스텔, 고급 빌딩, 랜드마크 타워
- 규격: 세로형 불투명 PNG. 캐릭터와 버튼이 놓이는 화면 하단 중앙은 시각적 여백을 확보한다.
- 표시 규칙: 보유 수량이 1 이상인 최고 등급 부동산을 기준으로 환경 전체를 교체한다.
- 임포트: Sprite/Single, Mipmap Off, Wrap Clamp

### 옷차림 성장 표시

- 별도 중복 아이콘 파일을 만들지 않고 기존 `Beggar_Stage_01` ~ `05` 스프라이트를 상단 축소 아이콘으로 재사용한다.
- 상단 HUD 구성: 현재 옷차림 아이콘, 단계 이름, 누적 획득 금액, 다음 단계 목표, 달성도 바
- 기준 금액: 0 / 2,000 / 15,000 / 80,000 / 400,000

## 현재 UI에서 재사용하는 에셋

- 재화/상태 아이콘: Coin, Estate, Sunfish, Trophy, Settings
- 전투 역할 아이콘: Tank, Dealer, Healer
- 속성 아이콘과 이펙트: 기존 `Resources/GameArt/Icons`, `Effects` 사용
- 패널과 버튼: 런타임 uGUI 단색 패널을 유지해 해상도 변화에 안전하게 대응

## 추가 제작 권장 규격

- 9-slice 패널: 256×256 PNG, 테두리 32px, 밝음/어두움 2종
- 주요 버튼: 384×128 PNG, 테두리 28px, 기본/눌림/비활성 3상태
- 작은 상태 아이콘: 128×128 PNG, 투명 배경, 사방 12px 여백
- 모든 UI 텍스트는 이미지에 굽지 않고 Unity Text로 표시한다.

## 공통 UI 스킨 적용

- `Resources/GameArt/UI/UI_Panel_NavyGold.png`: 512×512, 9-slice 테두리 72px. 홈 HUD, 개복치/부동산 패널, 하단 메뉴, 고용시장, 전투장, 캐릭터 카드에 사용한다.
- `Resources/GameArt/UI/UI_Button_Ivory.png`: 512×512, 9-slice 테두리 92px. 모든 주요/보조 버튼에 색상 틴트를 적용해 재사용한다.
- 패널 팔레트: Navy `#172746`, Gold `#D9B66F`, Cream `#FAF6EB`.
- 버튼 상태: 기본 100%, 강조 92%, 누름 72%, 비활성 34% 명도 배율.
- 본문에는 1.5px 어두운 그림자를 적용하며 버튼 글자는 20px, 주요 제목은 25~30px를 기준으로 한다.
- 정보 우선순위는 제목 → 핵심 수치 → 설명 → 주의 문구 순으로 유지하고 이미지 위에는 설명문을 겹치지 않는다.

## UI 스킨 2차 개선

- `UI_Panel_NavyGold`: 상단 HUD와 보스 전투장 등 최상위 정보 영역에만 사용한다.
- `UI_Panel_SoftNavy`: 일반 콘텐츠 패널, 부동산 카드, 고용 카드, 파티 슬롯에 사용한다. 금색 장식을 반복하지 않는다.
- `UI_Button_Capsule`: 모든 행동 버튼의 공통 외곽으로 사용하며 실제 투명 모서리와 완전한 캡슐 실루엣을 가진다.
- 행동 색상은 저채도 파랑 `#42699A`, 녹색 `#408471`, 적색 `#AE504C`, 금색 `#B38F45`를 기준으로 한다.
- 전체 화면 바깥 배경에는 서로 다른 장소 이미지를 겹치지 않는다. 홈은 단색 Navy, 전투는 16% 명도의 옥상 배경만 사용한다.
- 부동산은 카드와 구매 버튼을 분리하고, 개복치 미보유 상태에서는 매각 버튼을 숨긴다.
