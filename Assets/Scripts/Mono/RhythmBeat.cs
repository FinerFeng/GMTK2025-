using UnityEngine;

[System.Serializable]
public class RhythmBeat
{
    public float time;              // 节拍时间点（秒）
    public KeyCode requiredKey;     // 需要按下的键
    public bool isHit = false;      // 是否被命中
    public bool isProcessed = false; // 是否已处理

    // 默认构造函数（Unity序列化需要）
    public RhythmBeat()
    {
        time = 0f;
        requiredKey = KeyCode.A;
        isHit = false;
        isProcessed = false;
    }

    // 带参数的构造函数
    public RhythmBeat(float beatTime, KeyCode key)
    {
        time = beatTime;
        requiredKey = key;
        isHit = false;
        isProcessed = false;
    }
}