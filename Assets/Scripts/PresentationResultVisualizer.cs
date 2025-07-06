using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class GraphData
{
    public string title;
    public Color color;
    public float upperLimit;
    public float lowerLimit;
    public List<float> dataPoints = new List<float>();
    public string yAxisLabel;
}

[System.Serializable]
public class DataPoint
{
    public float wpm;
    public float volume;
    public float clarityScore;
    public float timestamp;
    
    public DataPoint(float wpm, float volume, float clarityScore, float timestamp)
    {
        this.wpm = wpm;
        this.volume = volume;
        this.clarityScore = clarityScore;
        this.timestamp = timestamp;
    }
}

public class PresentationResultVisualizer : MonoBehaviour
{
    [Header("UI 패널 설정")]
    public GameObject graphPanel; // 그래프 패널
    public TextMeshProUGUI titleText; // 그래프 제목
    public TextMeshProUGUI instructionText; // 안내 텍스트
    
    [Header("그래프 렌더링 설정")]
    public RectTransform graphContainer; // 그래프 컨테이너
    public GameObject pointPrefab; // 점 프리팹
    public GameObject linePrefab; // 선 프리팹 (UI Image 기반)
    
    [Header("축 설정")]
    public TextMeshProUGUI xAxisLabel; // X축 라벨
    public TextMeshProUGUI yAxisLabel; // Y축 라벨
    public Transform xAxisContainer; // X축 눈금 컨테이너
    public Transform yAxisContainer; // Y축 눈금 컨테이너
    public GameObject axisLabelPrefab; // 축 라벨 프리팹
    
    [Header("VR 입력 설정")]
    public bool useVRInput = true; // VR 입력 사용 여부
    
    [Header("그래프 데이터 설정")]
    public GraphData wpmGraph = new GraphData { 
        title = "말하기 속도 (WPM)", 
        color = Color.blue, 
        upperLimit = 115f, 
        lowerLimit = 85f, 
        yAxisLabel = "단어/분" 
    };
    public GraphData volumeGraph = new GraphData { 
        title = "음성 볼륨", 
        color = Color.green, 
        upperLimit = 0.07f, 
        lowerLimit = 0.03f, 
        yAxisLabel = "볼륨 레벨" 
    };
    public GraphData clarityGraph = new GraphData { 
        title = "발음 명확도", 
        color = Color.red, 
        upperLimit = 1.0f, 
        lowerLimit = 0.85f, 
        yAxisLabel = "명확도 점수" 
    };
    
    [Header("데이터 수집 설정")]
    public float dataCollectionInterval = 10f; // 데이터 수집 간격 (10초)
    public int pointsPerGraph = 3; // 그래프 점당 데이터 개수 (30초 = 10초 × 3)
    
    private List<DataPoint> collectedData = new List<DataPoint>(); // 수집된 데이터
    private List<DataPoint> tempDataBuffer = new List<DataPoint>(); // 임시 데이터 버퍼
    private VoiceAnalyzer voiceAnalyzer;
    private FeedbackManager feedbackManager;
    private TransitionManager transitionManager;
    
    private enum VisualizationState
    {
        Hidden,
        ShowingInstructions,
        ShowingWPM,
        ShowingVolume,
        ShowingClarity,
        Complete
    }
    
    private VisualizationState currentState = VisualizationState.Hidden;
    private bool isWaitingForInput = false;
    private bool isDataCollectionActive = false;
    private float lastDataCollectionTime = 0f;
    private bool wasTriggerPressed = false; // 이전 프레임 트리거 상태
    
    void Start()
    {
        // 컴포넌트 찾기
        voiceAnalyzer = FindObjectOfType<VoiceAnalyzer>();
        feedbackManager = FindObjectOfType<FeedbackManager>();
        transitionManager = FindObjectOfType<TransitionManager>();
        
        // 이벤트 연결
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnVoiceDataReceived += OnVoiceDataReceived;
        }
        
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart += StartDataCollection;
            // ShowResults는 FeedbackManager에서 호출하므로 중복 방지
        }
        
        // 초기 상태 설정
        SetPanelsActive(false);
        InitializeGraphComponents();
        
        Debug.Log("📊 PresentationResultVisualizer 초기화 완료");
    }
    
    void Update()
    {
        // VR 입력 처리
        if (useVRInput && isWaitingForInput)
        {
            CheckVRInput();
        }
        
        // 데이터 수집 처리
        if (isDataCollectionActive)
        {
            ProcessDataCollection();
        }
    }
    
    /// <summary>
    /// 그래프 컴포넌트 초기화
    /// </summary>
    private void InitializeGraphComponents()
    {
        // UI 기반 그래프이므로 별도 초기화 불필요
        Debug.Log("📊 UI 기반 그래프 컴포넌트 초기화 완료");
    }
    
    /// <summary>
    /// 데이터 수집 시작
    /// </summary>
    public void StartDataCollection()
    {
        isDataCollectionActive = true;
        lastDataCollectionTime = Time.time;
        collectedData.Clear();
        tempDataBuffer.Clear();
        
        Debug.Log("📊 데이터 수집 시작");
    }
    
    /// <summary>
    /// 음성 데이터 수신 이벤트 핸들러
    /// </summary>
    private void OnVoiceDataReceived(VoiceAnalysisData data)
    {
        if (!isDataCollectionActive) return;
        
        // 데이터 포인트 생성
        DataPoint newPoint = new DataPoint(
            data.wpm,
            data.volume,
            data.clarityScore,
            Time.time
        );
        
        tempDataBuffer.Add(newPoint);
        Debug.Log($"📊 데이터 수집: WPM={data.wpm:F1}, Volume={data.volume:F2}, Clarity={data.clarityScore:F2}");
    }
    
    /// <summary>
    /// 데이터 수집 처리
    /// </summary>
    private void ProcessDataCollection()
    {
        if (Time.time - lastDataCollectionTime >= dataCollectionInterval)
        {
            // 30초(3개 데이터) 마다 평균값 계산하여 그래프 점 생성
            if (tempDataBuffer.Count >= pointsPerGraph)
            {
                CreateGraphPoint();
                tempDataBuffer.Clear();
            }
            
            lastDataCollectionTime = Time.time;
        }
    }
    
    /// <summary>
    /// 그래프 점 생성 (30초 평균값)
    /// </summary>
    private void CreateGraphPoint()
    {
        if (tempDataBuffer.Count == 0) return;
        
        float avgWpm = 0f;
        float avgVolume = 0f;
        float avgClarity = 0f;
        
        // 평균값 계산
        foreach (DataPoint point in tempDataBuffer)
        {
            avgWpm += point.wpm;
            avgVolume += point.volume;
            avgClarity += point.clarityScore;
        }
        
        int count = tempDataBuffer.Count;
        avgWpm /= count;
        avgVolume /= count;
        avgClarity /= count;
        
        // 그래프 데이터에 추가
        wpmGraph.dataPoints.Add(avgWpm);
        volumeGraph.dataPoints.Add(avgVolume);
        clarityGraph.dataPoints.Add(avgClarity);
        
        Debug.Log($"📊 그래프 점 생성: WPM={avgWpm:F1}, Volume={avgVolume:F2}, Clarity={avgClarity:F2}");
    }
    
    /// <summary>
    /// 결과 표시 시작
    /// </summary>
    public void ShowResults()
    {
        isDataCollectionActive = false;
        
        // 마지막 데이터 처리
        if (tempDataBuffer.Count > 0)
        {
            CreateGraphPoint();
        }
        
        // 데이터가 없으면 테스트 데이터 생성
        if (wpmGraph.dataPoints.Count == 0)
        {
            GenerateTestData();
        }
        
        // FeedbackManager의 피드백 패널 비활성화
        if (feedbackManager != null)
        {
            feedbackManager.SetFeedbackPanelActive(false);
        }
        
        // 결과 시각화 시작
        StartCoroutine(ShowResultsSequence());
    }
    
    /// <summary>
    /// 결과 표시 시퀀스
    /// </summary>
    private IEnumerator ShowResultsSequence()
    {
        // 1. 안내 패널 표시
        yield return StartCoroutine(ShowInstructionPanel());
        
        // 2. WPM 그래프 표시
        yield return StartCoroutine(ShowGraphAndWaitForInput(wpmGraph));
        
        // 3. Volume 그래프 표시
        yield return StartCoroutine(ShowGraphAndWaitForInput(volumeGraph));
        
        // 4. Clarity 그래프 표시
        yield return StartCoroutine(ShowGraphAndWaitForInput(clarityGraph));
        
        // 5. 완료
        currentState = VisualizationState.Complete;
        SetPanelsActive(false);
        
        // FeedbackManager의 피드백 패널 다시 활성화
        if (feedbackManager != null)
        {
            feedbackManager.SetFeedbackPanelActive(true);
        }
        
        Debug.Log("📊 결과 시각화 완료");
    }
    
    /// <summary>
    /// 안내 패널 표시
    /// </summary>
    private IEnumerator ShowInstructionPanel()
    {
        currentState = VisualizationState.ShowingInstructions;
        SetPanelsActive(true);
        
        // 안내 텍스트 표시, 나머지는 숨김
        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "🎉 발표 완료!\n\n발표 분석 결과를 확인하세요.\n\n오른손 트리거를 눌러 시작하세요.";
        }
        
        // 그래프 관련 요소들 숨김
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (xAxisLabel != null) xAxisLabel.gameObject.SetActive(false);
        if (yAxisLabel != null) yAxisLabel.gameObject.SetActive(false);
        if (graphContainer != null) graphContainer.gameObject.SetActive(false);
        
        // 입력 대기
        isWaitingForInput = true;
        yield return new WaitUntil(() => !isWaitingForInput);
    }
    
    /// <summary>
    /// 그래프 표시 및 입력 대기
    /// </summary>
    private IEnumerator ShowGraphAndWaitForInput(GraphData graphData)
    {
        // 그래프 렌더링
        RenderGraph(graphData);
        
        // 안내 텍스트 업데이트
        if (instructionText != null)
        {
            instructionText.text = "오른손 트리거를 눌러 다음 그래프를 확인하세요.";
        }
        
        // 입력 대기
        isWaitingForInput = true;
        yield return new WaitUntil(() => !isWaitingForInput);
    }
    
    /// <summary>
    /// 그래프 렌더링
    /// </summary>
    private void RenderGraph(GraphData graphData)
    {
        Debug.Log($"📊 === 그래프 렌더링 시작: {graphData.title} ===");
        Debug.Log($"📊 데이터 포인트 개수: {graphData.dataPoints.Count}");
        Debug.Log($"📊 데이터 값들: [{string.Join(", ", graphData.dataPoints.ConvertAll(x => x.ToString("F2")))}]");
        Debug.Log($"📊 데이터 범위: {graphData.lowerLimit:F2} ~ {graphData.upperLimit:F2}");
        
        // 안내 텍스트 숨기고 그래프 요소들 표시
        if (instructionText != null) instructionText.gameObject.SetActive(false);
        if (titleText != null) titleText.gameObject.SetActive(true);
        if (xAxisLabel != null) xAxisLabel.gameObject.SetActive(true);
        if (yAxisLabel != null) yAxisLabel.gameObject.SetActive(true);
        if (graphContainer != null) graphContainer.gameObject.SetActive(true);
        
        // UI 강제 업데이트 (Rect 크기 갱신)
        if (graphContainer != null)
        {
            Canvas.ForceUpdateCanvases();
            Debug.Log($"📊 UI 강제 업데이트 후 Container 크기: {graphContainer.rect}");
        }
        
        // 제목 설정
        if (titleText != null)
        {
            titleText.text = graphData.title;
        }
        
        // Y축 라벨 설정
        if (yAxisLabel != null)
        {
            yAxisLabel.text = graphData.yAxisLabel;
        }
        
        // X축 라벨 설정
        if (xAxisLabel != null)
        {
            xAxisLabel.text = "시간 (30초 단위)";
        }
        
        // 이전 그래프 요소들 정리
        ClearPreviousGraphElements();
        
        // 데이터 포인트 렌더링
        RenderDataPoints(graphData);
        
        // 연결선 렌더링 (UI 기반)
        RenderConnectingLinesUI(graphData);
        
        // 기준선 렌더링 (UI 기반)
        RenderLimitLinesUI(graphData);
        
        // 축 눈금 렌더링
        RenderAxisLabels(graphData);
        
        Debug.Log($"📊 === 그래프 렌더링 완료: {graphData.title} ===");
    }
    
    /// <summary>
    /// 데이터 포인트 렌더링
    /// </summary>
    private void RenderDataPoints(GraphData graphData)
    {
        if (graphContainer == null || pointPrefab == null) 
        {
            Debug.LogError($"📊 RenderDataPoints 실패: graphContainer={graphContainer}, pointPrefab={pointPrefab}");
            return;
        }
        
        Debug.Log($"📊 GraphContainer 크기: {graphContainer.rect}");
        Debug.Log($"📊 데이터 포인트 개수: {graphData.dataPoints.Count}");
        
        // 새 점들 생성
        for (int i = 0; i < graphData.dataPoints.Count; i++)
        {
            float dataValue = graphData.dataPoints[i];
            Vector2 position = CalculatePointPosition(i, dataValue, graphData);
            
            Debug.Log($"📊 점 {i}: 데이터값={dataValue:F2}, 위치={position}");
            
            GameObject point = Instantiate(pointPrefab, graphContainer);
            point.name = $"DataPoint_{i}";
            point.GetComponent<RectTransform>().anchoredPosition = position;
            
            // 점 색상 설정
            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = graphData.color;
            }
        }
    }
    
    /// <summary>
    /// 연결선 렌더링 (UI 기반)
    /// </summary>
    private void RenderConnectingLinesUI(GraphData graphData)
    {
        if (linePrefab == null || graphData.dataPoints.Count < 2) return;
        
        // 새 선들 생성
        for (int i = 0; i < graphData.dataPoints.Count - 1; i++)
        {
            Vector2 pos1 = CalculatePointPosition(i, graphData.dataPoints[i], graphData);
            Vector2 pos2 = CalculatePointPosition(i + 1, graphData.dataPoints[i + 1], graphData);
            
            GameObject line = Instantiate(linePrefab, graphContainer);
            line.name = $"Line_{i}";
            RectTransform lineRect = line.GetComponent<RectTransform>();
            if (lineRect != null)
            {
                // 선의 중심점 계산
                Vector2 center = (pos1 + pos2) / 2f;
                lineRect.anchoredPosition = center;
                
                // 선의 길이와 회전 계산
                float distance = Vector2.Distance(pos1, pos2);
                float angle = Mathf.Atan2(pos2.y - pos1.y, pos2.x - pos1.x) * Mathf.Rad2Deg;
                
                lineRect.sizeDelta = new Vector2(distance, 2f);
                lineRect.rotation = Quaternion.Euler(0, 0, angle);
            }
            
            // 선 색상 설정
            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = graphData.color;
            }
        }
    }
    
    /// <summary>
    /// 기준선 렌더링 (UI 기반)
    /// </summary>
    private void RenderLimitLinesUI(GraphData graphData)
    {
        if (linePrefab == null || graphContainer == null) return;
        
        Rect containerRect = graphContainer.rect;
        float graphWidth = containerRect.width;
        float padding = graphWidth * 0.1f;
        float usableWidth = graphWidth - (padding * 2);
        
        // 상한선 생성
        float upperY = CalculateYPosition(graphData.upperLimit, graphData);
        float upperOffsetY = upperY - containerRect.height / 2f;
        
        GameObject upperLine = Instantiate(linePrefab, graphContainer);
        upperLine.name = "UpperLimitLine";
        RectTransform upperRect = upperLine.GetComponent<RectTransform>();
        if (upperRect != null)
        {
            upperRect.anchoredPosition = new Vector2(0, upperOffsetY);
            upperRect.sizeDelta = new Vector2(usableWidth, 1f);
            upperRect.rotation = Quaternion.identity;
        }
        
        Image upperImage = upperLine.GetComponent<Image>();
        if (upperImage != null)
        {
            upperImage.color = Color.red;
        }
        
        // 하한선 생성
        float lowerY = CalculateYPosition(graphData.lowerLimit, graphData);
        float lowerOffsetY = lowerY - containerRect.height / 2f;
        
        GameObject lowerLine = Instantiate(linePrefab, graphContainer);
        lowerLine.name = "LowerLimitLine";
        RectTransform lowerRect = lowerLine.GetComponent<RectTransform>();
        if (lowerRect != null)
        {
            lowerRect.anchoredPosition = new Vector2(0, lowerOffsetY);
            lowerRect.sizeDelta = new Vector2(usableWidth, 1f);
            lowerRect.rotation = Quaternion.identity;
        }
        
        Image lowerImage = lowerLine.GetComponent<Image>();
        if (lowerImage != null)
        {
            lowerImage.color = Color.red;
        }
    }
    
    /// <summary>
    /// 이전 그래프 요소들 정리
    /// </summary>
    private void ClearPreviousGraphElements()
    {
        if (graphContainer == null) return;
        
        // 기존 그래프 요소들 제거
        foreach (Transform child in graphContainer)
        {
            if (child.name.StartsWith("DataPoint") || 
                child.name.StartsWith("Line") || 
                child.name.StartsWith("UpperLimitLine") || 
                child.name.StartsWith("LowerLimitLine"))
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 축 눈금 렌더링
    /// </summary>
    private void RenderAxisLabels(GraphData graphData)
    {
        if (axisLabelPrefab == null || graphContainer == null) return;
        
        Rect containerRect = graphContainer.rect;
        
        // X축 눈금 (시간)
        if (xAxisContainer != null)
        {
            // 기존 라벨 제거
            foreach (Transform child in xAxisContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 새 라벨 생성
            for (int i = 0; i < graphData.dataPoints.Count; i++)
            {
                GameObject label = Instantiate(axisLabelPrefab, xAxisContainer);
                TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.text = $"{(i + 1) * 30}s";
                }
                
                // X 축 라벨 위치 계산
                float xPos = CalculateXPosition(i, graphData) - containerRect.width / 2f;
                label.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0);
            }
        }
        
        // Y축 눈금 (값)
        if (yAxisContainer != null)
        {
            // 기존 라벨 제거
            foreach (Transform child in yAxisContainer)
            {
                Destroy(child.gameObject);
            }
            
            // 새 라벨 생성 (5단계)
            for (int i = 0; i <= 4; i++)
            {
                GameObject label = Instantiate(axisLabelPrefab, yAxisContainer);
                TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    float minValue = graphData.lowerLimit - (graphData.upperLimit - graphData.lowerLimit) * 0.1f;
                    float maxValue = graphData.upperLimit + (graphData.upperLimit - graphData.lowerLimit) * 0.1f;
                    float value = Mathf.Lerp(minValue, maxValue, i / 4f);
                    labelText.text = value.ToString("F0");
                }
                
                // Y 축 라벨 위치 계산
                float padding = containerRect.height * 0.1f;
                float usableHeight = containerRect.height - (padding * 2);
                float yPos = (padding + (i / 4f) * usableHeight) - containerRect.height / 2f;
                
                label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos);
            }
        }
    }
    
    /// <summary>
    /// 점 위치 계산 (graphContainer 기준)
    /// </summary>
    private Vector2 CalculatePointPosition(int index, float value, GraphData graphData)
    {
        if (graphContainer == null) 
        {
            Debug.LogError("📊 CalculatePointPosition: graphContainer가 null입니다!");
            return Vector2.zero;
        }
        
        float x = CalculateXPosition(index, graphData);
        float y = CalculateYPosition(value, graphData);
        
        // graphContainer의 실제 크기를 기준으로 계산
        Rect containerRect = graphContainer.rect;
        
        // 중심점을 기준으로 오프셋 계산 (-width/2 ~ +width/2, -height/2 ~ +height/2)
        float offsetX = x - containerRect.width / 2f;
        float offsetY = y - containerRect.height / 2f;
        
        Debug.Log($"📊 위치 계산 - Index: {index}, Value: {value:F2}, X: {x:F2}, Y: {y:F2}, 최종 위치: ({offsetX:F2}, {offsetY:F2})");
        Debug.Log($"📊 Container Rect: {containerRect}, 데이터 범위: {graphData.lowerLimit:F2} ~ {graphData.upperLimit:F2}");
        
        return new Vector2(offsetX, offsetY);
    }
    
    /// <summary>
    /// X 위치 계산
    /// </summary>
    private float CalculateXPosition(int index, GraphData graphData)
    {
        if (graphContainer == null || graphData.dataPoints.Count <= 1) 
        {
            Debug.LogWarning($"📊 CalculateXPosition: graphContainer={graphContainer}, dataPoints.Count={graphData.dataPoints.Count}");
            return 0;
        }
        
        float graphWidth = graphContainer.rect.width;
        float padding = graphWidth * 0.1f; // 좌우 10% 여백
        float usableWidth = graphWidth - (padding * 2);
        
        if (graphData.dataPoints.Count == 1)
        {
            float centerX = padding + usableWidth / 2f;
            Debug.Log($"📊 X 위치 (단일 점): {centerX:F2}");
            return centerX;
        }
        
        float normalizedX = index / (float)(graphData.dataPoints.Count - 1);
        float finalX = padding + (normalizedX * usableWidth);
        
        Debug.Log($"📊 X 위치 계산: index={index}, 총 점 개수={graphData.dataPoints.Count}, normalizedX={normalizedX:F3}, finalX={finalX:F2}");
        
        return finalX;
    }
    
    /// <summary>
    /// Y 위치 계산
    /// </summary>
    private float CalculateYPosition(float value, GraphData graphData)
    {
        if (graphContainer == null) 
        {
            Debug.LogWarning("📊 CalculateYPosition: graphContainer가 null입니다!");
            return 0;
        }
        
        float graphHeight = graphContainer.rect.height;
        float padding = graphHeight * 0.1f; // 상하 10% 여백
        float usableHeight = graphHeight - (padding * 2);
        
        // 값의 범위 계산 (여유 공간 포함)
        float minValue = graphData.lowerLimit - (graphData.upperLimit - graphData.lowerLimit) * 0.1f;
        float maxValue = graphData.upperLimit + (graphData.upperLimit - graphData.lowerLimit) * 0.1f;
        
        // 정규화된 값 계산 (0~1)
        float normalizedValue = (value - minValue) / (maxValue - minValue);
        normalizedValue = Mathf.Clamp01(normalizedValue);
        
        // Y 좌표 계산 (하단부터 시작)
        float finalY = padding + (normalizedValue * usableHeight);
        
        Debug.Log($"📊 Y 위치 계산: value={value:F2}, 범위=[{minValue:F2}, {maxValue:F2}], 정규화={normalizedValue:F3}, finalY={finalY:F2}");
        
        return finalY;
    }
    
    /// <summary>
    /// VR 입력 확인
    /// </summary>
    private void CheckVRInput()
    {
        bool triggerPressed = false;
        
        // VR 컨트롤러 트리거 입력 확인
        if (useVRInput)
        {
            // Unity XR Input System 사용
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand, rightHandDevices);
            
            if (rightHandDevices.Count > 0)
            {
                var device = rightHandDevices[0];
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerPressed);
            }
        }
        
        // 키보드 입력 (테스트용)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            triggerPressed = true;
        }
        
        // 트리거가 눌렸을 때 (이전 프레임에서는 안 눌렸고 현재 프레임에서 눌림)
        if (triggerPressed && !wasTriggerPressed)
        {
            OnTriggerPressed();
        }
        
        // 이전 프레임 상태 저장
        wasTriggerPressed = triggerPressed;
    }
    
    /// <summary>
    /// 트리거 입력 처리
    /// </summary>
    private void OnTriggerPressed()
    {
        if (isWaitingForInput)
        {
            isWaitingForInput = false;
            Debug.Log("📊 트리거 입력 감지 - 다음 단계로 진행");
        }
    }
    
    /// <summary>
    /// 패널 활성화/비활성화
    /// </summary>
    private void SetPanelsActive(bool active)
    {
        if (graphPanel != null)
        {
            graphPanel.SetActive(active);
        }
    }
    
    /// <summary>
    /// 수동으로 결과 표시 (테스트용)
    /// </summary>
    [ContextMenu("Show Test Results")]
    public void ShowTestResults()
    {
        // 테스트 데이터 생성
        GenerateTestData();
        ShowResults();
    }
    
    /// <summary>
    /// 테스트 데이터 생성
    /// </summary>
    private void GenerateTestData()
    {
        wpmGraph.dataPoints.Clear();
        volumeGraph.dataPoints.Clear();
        clarityGraph.dataPoints.Clear();
        
        // 새로운 정상 범위 기반 랜덤 테스트 데이터 생성
        // WPM: 85-115 (정상), Volume: 0.03-0.07 (정상), Clarity: 0.85-1.0 (정상)
        for (int i = 0; i < 5; i++)
        {
            float wpm = Random.Range(80f, 120f);    // 정상 범위 85-115를 포함하는 넓은 범위
            float volume = Random.Range(0.02f, 0.08f);  // 정상 범위 0.03-0.07을 포함하는 넓은 범위
            float clarity = Random.Range(0.8f, 1.0f);   // 정상 범위 0.85-1.0을 포함하는 넓은 범위
            
            wpmGraph.dataPoints.Add(wpm);
            volumeGraph.dataPoints.Add(volume);
            clarityGraph.dataPoints.Add(clarity);
            
            Debug.Log($"📊 테스트 데이터 {i}: WPM={wpm:F1}, Volume={volume:F3}, Clarity={clarity:F3}");
        }
        
        Debug.Log("📊 테스트 데이터 생성 완료");
        Debug.Log($"📊 WPM 데이터: [{string.Join(", ", wpmGraph.dataPoints)}]");
        Debug.Log($"📊 Volume 데이터: [{string.Join(", ", volumeGraph.dataPoints)}]");
        Debug.Log($"📊 Clarity 데이터: [{string.Join(", ", clarityGraph.dataPoints)}]");
    }
    
    void OnDestroy()
    {
        // 이벤트 해제
        if (voiceAnalyzer != null)
        {
            voiceAnalyzer.OnVoiceDataReceived -= OnVoiceDataReceived;
        }
        
        if (transitionManager != null)
        {
            transitionManager.OnPresentationStart -= StartDataCollection;
        }
        
        Debug.Log("📊 PresentationResultVisualizer 리소스 정리 완료");
    }
} 