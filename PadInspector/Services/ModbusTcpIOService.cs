using System.IO;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using PadInspector.Configs;
using PadInspector.Models;

namespace PadInspector.Services;

/// <summary>
/// Modbus TCP IO 서비스 - PLC와 통신하여 IO 제어
/// 입력: Coil 읽기 (트리거 신호), 출력: Coil 쓰기 (PASS/FAIL)
/// </summary>
public class ModbusTcpIOService : IIOService
{
    public event EventHandler<IOSignal>? TriggerReceived;

    private readonly ModbusSettings _settings;
    private readonly ILogService _logService;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private Timer? _pollTimer;
    private bool _isRunning;
    private bool[] _prevInputs;
    private ushort _transactionId;
    private readonly object _lock = new();

    public bool IsRunning => _isRunning;

    public ModbusTcpIOService(IOptions<ModbusSettings> options, ILogService logService)
    {
        _settings = options.Value;
        _logService = logService;
        _prevInputs = new bool[_settings.InputCount];
    }

    public bool Connect()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_settings.IpAddress, _settings.Port);
            _client.ReceiveTimeout = 1000;
            _client.SendTimeout = 1000;
            _stream = _client.GetStream();
            _logService.Log("INFO", $"Modbus TCP 연결 성공 ({_settings.IpAddress}:{_settings.Port})");
            return true;
        }
        catch (Exception ex)
        {
            _logService.Log("ERR", $"Modbus TCP 연결 실패: {ex.Message}");
            _client?.Dispose();
            _client = null;
            _stream = null;
            return false;
        }
    }

    public void Start()
    {
        if (_stream == null && !Connect()) return;

        _isRunning = true;
        _pollTimer = new Timer(PollInputs, null, 0, _settings.PollIntervalMs);
    }

    public void Stop()
    {
        _isRunning = false;
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    public void FireTrigger(int channel)
    {
        if (!_isRunning) return;
        TriggerReceived?.Invoke(this, new IOSignal
        {
            Channel = channel,
            IsOn = true,
            Name = $"SW_TRIGGER_{channel}",
            Timestamp = DateTime.Now
        });
    }

    public void StartAutoTrigger(int intervalMs = 2000) { }
    public void StopAutoTrigger() { }

    public void SetOutput(int channel, bool value)
    {
        if (_stream == null) return;
        lock (_lock)
        {
            try
            {
                WriteSingleCoil(_settings.OutputStartAddress + channel, value);
            }
            catch (Exception ex)
            {
                _logService.Log("ERR", $"Modbus 출력 오류 (CH{channel}): {ex.Message}");
            }
        }
    }

    private void PollInputs(object? state)
    {
        if (_stream == null || !_isRunning) return;

        lock (_lock)
        {
            try
            {
                var inputs = ReadCoils(_settings.InputStartAddress, _settings.InputCount);
                if (inputs == null) return;

                for (int i = 0; i < inputs.Length && i < _prevInputs.Length; i++)
                {
                    // Rising edge 감지 → 트리거
                    if (inputs[i] && !_prevInputs[i])
                    {
                        TriggerReceived?.Invoke(this, new IOSignal
                        {
                            Channel = i,
                            IsOn = true,
                            Name = $"MODBUS_IN_{i}",
                            Timestamp = DateTime.Now
                        });
                    }
                    _prevInputs[i] = inputs[i];
                }
            }
            catch (Exception ex)
            {
                _logService.Log("ERR", $"Modbus 폴링 오류: {ex.Message}");
                TryReconnect();
            }
        }
    }

    private void TryReconnect()
    {
        try
        {
            _stream?.Dispose();
            _client?.Dispose();
        }
        catch { }
        _client = null;
        _stream = null;
        Connect();
    }

    #region Modbus TCP Protocol

    private bool[]? ReadCoils(int address, int count)
    {
        // FC01: Read Coils
        var request = BuildRequest(0x01, (ushort)address, (ushort)count);
        _stream!.Write(request, 0, request.Length);

        var header = new byte[9]; // MBAP header(7) + FC(1) + byte count(1)
        if (ReadExact(header, 9) < 9) return null;

        // FC 에러 체크 (최상위 비트 set = exception response)
        if ((header[7] & 0x80) != 0) return null;

        int byteCount = header[8];
        var data = new byte[byteCount];
        if (ReadExact(data, byteCount) < byteCount) return null;

        var result = new bool[count];
        for (int i = 0; i < count; i++)
            result[i] = (data[i / 8] & (1 << (i % 8))) != 0;

        return result;
    }

    private void WriteSingleCoil(int address, bool value)
    {
        // FC05: Write Single Coil
        var request = BuildRequest(0x05, (ushort)address, (ushort)(value ? 0xFF00 : 0x0000));
        _stream!.Write(request, 0, request.Length);

        // 응답 검증 (echo)
        var response = new byte[12];
        int bytesRead = ReadExact(response, 12);
        if (bytesRead < 12 || (response[7] & 0x80) != 0)
            throw new IOException($"Invalid Modbus response (read {bytesRead} bytes, FC=0x{response[7]:X2})");
    }

    private int ReadExact(byte[] buffer, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int read = _stream!.Read(buffer, totalRead, count - totalRead);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }

    private byte[] BuildRequest(byte functionCode, ushort address, ushort value)
    {
        var txId = _transactionId++;
        return
        [
            (byte)(txId >> 8), (byte)txId,       // Transaction ID
            0, 0,                                  // Protocol ID (Modbus)
            0, 6,                                  // Length
            _settings.SlaveId,                     // Unit ID
            functionCode,                          // Function Code
            (byte)(address >> 8), (byte)address,   // Address
            (byte)(value >> 8), (byte)value        // Value/Quantity
        ];
    }

    #endregion

    public void Dispose()
    {
        Stop();
        _stream?.Dispose();
        _client?.Dispose();
    }
}
