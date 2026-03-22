namespace PadInspector.Configs;

public class ModbusSettings
{
    public bool Enabled { get; set; } = false;
    public string IpAddress { get; set; } = "192.168.0.1";
    public int Port { get; set; } = 502;
    public byte SlaveId { get; set; } = 1;
    public int PollIntervalMs { get; set; } = 50;

    /// <summary>입력 레지스터 시작 주소 (트리거 신호 읽기)</summary>
    public int InputStartAddress { get; set; } = 0;
    /// <summary>입력 채널 수</summary>
    public int InputCount { get; set; } = 2;

    /// <summary>출력 코일 시작 주소 (PASS/FAIL 쓰기)</summary>
    public int OutputStartAddress { get; set; } = 0;
    /// <summary>출력 채널 수</summary>
    public int OutputCount { get; set; } = 4;
}
