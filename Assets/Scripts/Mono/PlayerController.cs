using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [Header("Sprite Prefabs")]
    public GameObject keyA_Prefab;
    public GameObject keyD_Prefab;
    public Vector3 keyASpawnPosition;
    public Vector3 keyDSpawnPosition;

    private SpriteRenderer keyA_SpriteRenderer;
    private SpriteRenderer keyD_SpriteRenderer;

    [Header("Visual Settings")]
    public Color normalKeyColor = Color.gray;
    public Color highlightKeyColor = Color.white;
    public Color successKeyColor = Color.green;
    public Color missKeyColor = Color.red;
    public float feedbackDisplayDuration = 0.2f;

    [Header("Rhythm Settings")]
    public float beatInterval = 1.0f;           // 节拍间隔（秒）
    public float successWindow = 0.4f;          // 成功按键的时间窗口
    public float highlightDuration = 0.8f;      // 高亮显示的持续时间
    public bool infiniteLoop = true;            // 无限循环模式

    [Header("Slow Motion Settings")]
    public float slowMotionDuration = 1.5f;
    public float slowMotionTimeScale = 0.3f;
    private float normalTimeScale;
    private float slowMotionTimer = 0f;
    private bool inSlowMotion = false;

    // 简化的节拍系统
    private float gameStartTime;
    private int beatCounter = 0;
    private bool waitingForInput = false;
    private KeyCode expectedKey;
    private float currentBeatStartTime;

    // 协程引用
    private Coroutine aKeyColorCoroutine;
    private Coroutine dKeyColorCoroutine;

    void Awake()
    {
        normalTimeScale = Time.timeScale;

        // 实例化按键视觉
        if (keyA_Prefab != null)
        {
            GameObject aObj = Instantiate(keyA_Prefab, keyASpawnPosition, Quaternion.identity);
            keyA_SpriteRenderer = aObj.GetComponent<SpriteRenderer>();
            if (keyA_SpriteRenderer == null) Debug.LogError("A 键 Prefab 没有 SpriteRenderer 组件！");
        }
        if (keyD_Prefab != null)
        {
            GameObject dObj = Instantiate(keyD_Prefab, keyDSpawnPosition, Quaternion.identity);
            keyD_SpriteRenderer = dObj.GetComponent<SpriteRenderer>();
            if (keyD_SpriteRenderer == null) Debug.LogError("D 键 Prefab 没有 SpriteRenderer 组件！");
        }
    }

    void Start()
    {
        gameStartTime = Time.time;
        SetAllKeysColor(normalKeyColor);
        StartNextBeat();
        Debug.Log("节奏游戏开始！A-D交替，无限循环模式。");
    }

    void Update()
    {
        HandleSlowMotion();
        CheckBeatTiming();
        HandlePlayerInput();
    }

    /// <summary>
    /// 开始下一个节拍
    /// </summary>
    void StartNextBeat()
    {
        // A-D交替：偶数为A，奇数为D
        expectedKey = (beatCounter % 2 == 0) ? KeyCode.A : KeyCode.D;
        currentBeatStartTime = Time.time;
        waitingForInput = true;

        Debug.Log($"节拍 {beatCounter}: 期望按键 {expectedKey}，开始时间 {currentBeatStartTime:F2}s");

        // 立即高亮对应按键
        SetKeyColor(expectedKey, highlightKeyColor);

        beatCounter++;
    }

    /// <summary>
    /// 检查节拍时机
    /// </summary>
    void CheckBeatTiming()
    {
        if (!waitingForInput) return;

        float elapsed = Time.time - currentBeatStartTime;

        // 如果超过成功窗口时间，视为错过
        if (elapsed > successWindow)
        {
            Debug.LogWarning($"错过节拍！耗时: {elapsed:F2}s");
            OnBeatMissed();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    void HandlePlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            OnKeyPressed(KeyCode.A);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            OnKeyPressed(KeyCode.D);
        }
    }

    /// <summary>
    /// 按键被按下时的处理
    /// </summary>
    void OnKeyPressed(KeyCode pressedKey)
    {
        if (!waitingForInput)
        {
            Debug.LogWarning($"不在等待输入状态，按下了 {pressedKey}");
            ShowFeedback(pressedKey, missKeyColor);
            return;
        }

        float responseTime = Time.time - currentBeatStartTime;

        if (pressedKey == expectedKey)
        {
            // 按对了键
            string performance = GetPerformanceRating(responseTime);
            Debug.Log($"✅ {performance} 成功！按键: {pressedKey}, 反应时间: {responseTime:F3}s");

            OnBeatSuccess();
        }
        else
        {
            // 按错了键
            Debug.LogWarning($"❌ 按错了！期望 {expectedKey}，按下了 {pressedKey}");
            OnBeatFailed();
        }
    }

    /// <summary>
    /// 根据反应时间评价表现
    /// </summary>
    string GetPerformanceRating(float responseTime)
    {
        if (responseTime < 0.1f) return "闪电般！⚡";
        else if (responseTime < 0.2f) return "完美！⭐⭐⭐";
        else if (responseTime < 0.3f) return "很好！⭐⭐";
        else return "不错！⭐";
    }

    /// <summary>
    /// 节拍成功
    /// </summary>
    void OnBeatSuccess()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, successKeyColor);

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 节拍失败（按错键）
    /// </summary>
    void OnBeatFailed()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 错过节拍
    /// </summary>
    void OnBeatMissed()
    {
        waitingForInput = false;
        ShowFeedback(expectedKey, missKeyColor);
        StartSlowMotion();

        // 等待间隔后开始下一个节拍
        StartCoroutine(WaitForNextBeat());
    }

    /// <summary>
    /// 等待下一个节拍
    /// </summary>
    IEnumerator WaitForNextBeat()
    {
        // 重置所有按键颜色
        yield return new WaitForSeconds(0.1f);
        SetAllKeysColor(normalKeyColor);

        // 等待节拍间隔
        yield return new WaitForSeconds(beatInterval - 0.1f);

        // 开始下一个节拍（如果是无限模式）
        if (infiniteLoop)
        {
            StartNextBeat();
        }
        else
        {
            Debug.Log("节奏序列结束！");
        }
    }

    /// <summary>
    /// 显示按键反馈
    /// </summary>
    void ShowFeedback(KeyCode key, Color color)
    {
        if (key == KeyCode.A)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            aKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyA_SpriteRenderer, color));
        }
        else if (key == KeyCode.D)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            dKeyColorCoroutine = StartCoroutine(ShowColorFeedback(keyD_SpriteRenderer, color));
        }
    }

    /// <summary>
    /// 颜色反馈协程
    /// </summary>
    IEnumerator ShowColorFeedback(SpriteRenderer renderer, Color feedbackColor)
    {
        if (renderer == null) yield break;

        renderer.color = feedbackColor;
        yield return new WaitForSeconds(feedbackDisplayDuration);
        renderer.color = normalKeyColor;
    }

    /// <summary>
    /// 设置单个按键颜色
    /// </summary>
    void SetKeyColor(KeyCode key, Color color)
    {
        if (key == KeyCode.A && keyA_SpriteRenderer != null)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            keyA_SpriteRenderer.color = color;
        }
        else if (key == KeyCode.D && keyD_SpriteRenderer != null)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            keyD_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 设置所有按键颜色
    /// </summary>
    void SetAllKeysColor(Color color)
    {
        if (keyA_SpriteRenderer != null)
        {
            if (aKeyColorCoroutine != null) StopCoroutine(aKeyColorCoroutine);
            keyA_SpriteRenderer.color = color;
        }
        if (keyD_SpriteRenderer != null)
        {
            if (dKeyColorCoroutine != null) StopCoroutine(dKeyColorCoroutine);
            keyD_SpriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 处理慢动作
    /// </summary>
    void HandleSlowMotion()
    {
        if (inSlowMotion)
        {
            slowMotionTimer += Time.unscaledDeltaTime;
            if (slowMotionTimer >= slowMotionDuration)
            {
                Time.timeScale = normalTimeScale;
                inSlowMotion = false;
                slowMotionTimer = 0f;
                Debug.Log("慢动作结束");
            }
        }
    }

    /// <summary>
    /// 开始慢动作
    /// </summary>
    void StartSlowMotion()
    {
        if (!inSlowMotion)
        {
            Time.timeScale = slowMotionTimeScale;
            inSlowMotion = true;
            slowMotionTimer = 0f;
            Debug.Log("触发慢动作！");
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    [ContextMenu("重新开始")]
    public void RestartGame()
    {
        StopAllCoroutines();
        beatCounter = 0;
        waitingForInput = false;
        Time.timeScale = normalTimeScale;
        inSlowMotion = false;
        slowMotionTimer = 0f;

        SetAllKeysColor(normalKeyColor);

        gameStartTime = Time.time;
        StartNextBeat();

        Debug.Log("游戏重新开始！");
    }

    /// <summary>
    /// 暂停/继续游戏
    /// </summary>
    [ContextMenu("暂停/继续")]
    public void TogglePause()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = inSlowMotion ? slowMotionTimeScale : normalTimeScale;
            Debug.Log("游戏继续");
        }
        else
        {
            Time.timeScale = 0;
            Debug.Log("游戏暂停");
        }
    }
}