# Phantom:Makina (Team 재조합)

### 프로젝트 소개

팀 단위 개발 경험을 쌓기 위해 진행하는 싱글 TPS 게임 개발 팀 프로젝트.  
실제 프로젝트는 SVN을 사용하여 팀원과 협업하며, Github는 프로젝트 백업 용도로 사용하였습니다.

### 클론 방법

외부 에셋(DOTween, Legs Animator 등)이 `Branch/Assets/_ExAssets` **서브모듈**에 들어 있습니다.
일반 `git clone`은 서브모듈 내용을 가져오지 않으므로, 아래와 같이 클론해야 합니다.

```bash
git clone --recurse-submodules <저장소 주소>
```

이미 클론한 경우에는 프로젝트 루트에서 다음을 실행합니다.

```bash
git submodule update --init --recursive
```

> 서브모듈을 받지 않은 상태로 Unity를 열면 `DG`, `FIMSpace`, `LegsAnimator` 등의
> 네임스페이스를 찾을 수 없다는 `CS0246` 컴파일 에러가 발생하며 Safe Mode로 진입합니다.
> 서브모듈을 받은 뒤 **Unity를 완전히 종료하고 다시 열어야** 스크립트가 재컴파일됩니다.
>
> 서브모듈 저장소는 Private이므로 접근 권한(콜라보레이터 등록)이 필요합니다.
> 권한이 없으면 인증 실패로 받아지지 않습니다.

### 개발 기간

개발 중 (2025.06.16 ~)

### 개발 환경

#### 사용 언어 및 프레임워크
- C#

#### 엔진 및 개발 도구
- Unity
- Visual Studio 2022
- JetBrain Rider

#### 협업 툴
- Git
- SVN

### 팀 규모

- 기획 1명
- 원화 2명
- 3D 모델러 3명
- 프로그래머 2명

### 다운로드 링크

추후 추가 예정

### 게임 플레이 영상

추후 추가 예정

### 게임 주요 스크린샷

추후 추가 예정
