# VR 발표 시뮬레이터 시스템

Unity XR Interaction Toolkit을 사용한 VR 발표 시뮬레이터 시스템입니다.

## 주요 기능

1. **슬라이드 전환**: 오른쪽 컨트롤러 트리거 버튼으로 슬라이드 넘기기
2. **대본 확인**: 왼쪽 컨트롤러 트리거 버튼으로 대본 패널 표시, A 버튼으로 대본 페이지 넘기기
3. **음성 분석**: 실시간 음성 분석을 통한 발표 품질 평가
4. **실시간 피드백**: 15초마다 분석 결과 기반 피드백 제공
5. **청중 반응**: 발표 품질에 따른 청중 반응 시뮬레이션 (소리, VFX)
6. **결과 분석**: 발표 종료 후 종합 결과 및 개선점 제공

## 스크립트 구성

### 1. TransitionManager.cs
발표 슬라이드 전환을 관리하는 메인 스크립트입니다.

**설정 방법:**
- `slideImages`: 발표 자료 이미지 리스트에 텍스처 추가
- `slideDisplay`: 슬라이드를 표시할 RawImage 컴포넌트 연결
- `rightController`: 오른쪽 VR 컨트롤러 연결

### 2. HandPresentationController.cs
왼손 대본 패널을 관리하는 스크립트입니다.

**설정 방법:**
- `scriptTexts`: 대본 텍스트 리스트에 문자열 추가
- `leftHandPanel`: 왼손에 표시될 패널 오브젝트 연결
- `scriptDisplayText`: 대본을 표시할 Text 컴포넌트 연결
- `leftController`: 왼쪽 VR 컨트롤러 연결

### 3. VoiceAnalyzer.cs
음성 분석 시스템을 관리하는 스크립트입니다.

**설정 방법:**
- `analysisInterval`: 분석 간격 (기본 15초)
- `minVolumeThreshold`: 음성 감지 최소 볼륨
- `audioSource`: 오디오 소스 컴포넌트 연결

### 4. FeedbackManager.cs
실시간 피드백 표시를 관리하는 스크립트입니다.

**설정 방법:**
- `feedbackPanel`: 피드백 패널 오브젝트 연결
- `feedbackText`: 피드백 텍스트 컴포넌트 연결
- `scoreSlider`: 점수 슬라이더 컴포넌트 연결
- `feedbackPosition`: 피드백 표시 위치 설정

### 5. AudienceReactionManager.cs
청중 반응 시스템을 관리하는 스크립트입니다.

**설정 방법:**
- `reactions`: 반응 리스트에 오디오 클립과 VFX 프리팹 추가
- `audiencePositions`: 청중 위치 Transform 배열 설정
- `audioSource`: 반응 소리 재생용 오디오 소스 연결

### 6. PresentationResultManager.cs
발표 결과 분석 및 표시를 관리하는 스크립트입니다.

**설정 방법:**
- `resultPanel`: 결과 패널 오브젝트 연결
- `overallScoreText`: 전체 점수 텍스트 컴포넌트 연결
- `gradeText`: 등급 텍스트 컴포넌트 연결
- 각종 UI 슬라이더와 텍스트 컴포넌트 연결

### 7. VRPresentationManager.cs
전체 시스템을 통합 관리하는 메인 매니저 스크립트입니다.

**설정 방법:**
- 모든 시스템 컴포넌트를 자동으로 찾아 연결
- `autoStart`: 자동 시작 여부 설정
- `leftController`, `rightController`: VR 컨트롤러 연결

## 설정 가이드

### 1. 기본 설정
1. VRPresentationManager 스크립트를 빈 GameObject에 추가
2. 각 시스템 스크립트를 별도 GameObject에 추가
3. UI 캔버스와 패널들을 World Space로 설정

### 2. 슬라이드 설정
1. 발표 자료 이미지들을 Texture2D로 임포트
2. TransitionManager의 slideImages 리스트에 추가
3. 슬라이드 표시용 RawImage 컴포넌트 연결

### 3. 대본 설정
1. HandPresentationController의 scriptTexts 리스트에 대본 텍스트 추가
2. 왼손 패널 UI 설정 (World Space Canvas)
3. 대본 표시용 Text 컴포넌트 연결

### 4. 음성 분석 설정
1. 마이크 권한 허용
2. VoiceAnalyzer의 분석 간격 설정
3. 실제 AI 모델 연동 시 `GenerateAnalysisData()` 메서드 수정

### 5. 피드백 UI 설정
1. 피드백 패널을 플레이어 정면에 배치
2. 애니메이션 설정 (페이드인/아웃 시간)
3. 색상 설정 (우수/양호/부족)

### 6. 청중 반응 설정
1. 청중 위치 Transform 배열 설정
2. 반응별 오디오 클립과 VFX 프리팹 추가
3. 점수 구간별 반응 확률 설정

### 7. 결과 UI 설정
1. 결과 패널 UI 구성
2. 점수별 슬라이더와 텍스트 컴포넌트 연결
3. 버튼 이벤트 설정

## 컨트롤러 입력

### 오른쪽 컨트롤러
- **트리거 버튼**: 다음 슬라이드로 넘기기

### 왼쪽 컨트롤러
- **트리거 버튼** (홀드): 대본 패널 표시/숨기기
- **A 버튼**: 다음 대본 페이지로 넘기기

## 이벤트 시스템

각 스크립트는 C# Action을 사용한 이벤트 시스템을 제공합니다:

- `OnSlideChanged`: 슬라이드 변경 시
- `OnScriptChanged`: 대본 변경 시
- `OnAnalysisCompleted`: 음성 분석 완료 시
- `OnFeedbackDisplayed`: 피드백 표시 시
- `OnReactionTriggered`: 청중 반응 발생 시
- `OnResultDisplayed`: 결과 표시 시

## 확장 가능성

### AI 모델 연동
`VoiceAnalyzer.cs`의 `GenerateAnalysisData()` 메서드를 수정하여 실제 AI 분석 모델과 연동할 수 있습니다.

### 네트워크 기능
멀티플레이어 발표 환경을 위한 네트워크 기능을 추가할 수 있습니다.

### 데이터 저장
발표 결과를 데이터베이스에 저장하고 진행 상황을 추적할 수 있습니다.

## 디버깅

### 콘솔 로그
모든 주요 이벤트와 상태 변화가 콘솔에 로그로 출력됩니다.

### 디버그 메서드
VRPresentationManager에서 Context Menu를 통해 디버그 메서드를 실행할 수 있습니다:
- 시스템 상태 확인
- 발표 시작/중지
- 시스템 재시작

## 문제 해결

### 마이크 권한 문제
- 유니티 에디터와 빌드된 앱에서 마이크 권한을 허용해야 합니다.
- Player Settings에서 마이크 권한 설정을 확인하세요.

### VR 컨트롤러 인식 문제
- XR Interaction Toolkit 설정을 확인하세요.
- VRPresentationManager가 컨트롤러를 자동으로 찾지 못하면 수동으로 연결하세요.

### UI 표시 문제
- 모든 UI 캔버스가 World Space로 설정되어 있는지 확인하세요.
- UI 요소들이 VR 카메라에서 보이는 위치에 있는지 확인하세요.

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다.

## 연락처

프로젝트에 대한 질문이나 제안사항이 있으시면 이슈를 등록해주세요. 