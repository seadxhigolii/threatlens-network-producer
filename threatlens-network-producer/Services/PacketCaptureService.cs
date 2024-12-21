using SharpPcap;
using PacketDotNet;
using threatlens_network_producer.Services;

public class PacketCaptureService
{
    private readonly KafkaProducerService _kafkaProducer;
    private ICaptureDevice _device;
    private bool _isCapturing;

    public PacketCaptureService(KafkaProducerService kafkaProducer)
    {
        _kafkaProducer = kafkaProducer;
    }

    public async Task StartCaptureAsync(CancellationToken cancellationToken)
    {
        if (_isCapturing)
        {
            return;
        }

        var devices = CaptureDeviceList.Instance;
        if (devices.Count < 1)
        {
            return;
        }

        _device = devices[5];
        Console.WriteLine($"Listening on device: {_device.Description}");

        ConfigureDevice();
        _device.StartCapture();
        _isCapturing = true;

        await Task.Run(() => WaitUntilStopped(cancellationToken), cancellationToken);
    }

    public void StopCapture()
    {
        if (!_isCapturing)
        {
            return;
        }

        _device.StopCapture();
        _device.Close();
        _isCapturing = false;
    }

    private void ConfigureDevice()
    {
        _device.OnPacketArrival += OnPacketArrival;

        var config = new DeviceConfiguration
        {
            Mode = DeviceModes.Promiscuous,
            ReadTimeout = 1000,
            Snaplen = 65536
        };

        _device.Open(config);
    }

    private void WaitUntilStopped(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isCapturing)
        {
            Thread.Sleep(500);
        }
    }

    private void OnPacketArrival(object sender, PacketCapture e)
    {
        var rawPacket = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
        var tcpPacket = rawPacket.Extract<TcpPacket>();
        var ipPacket = rawPacket.Extract<IPPacket>();

        if (tcpPacket != null && ipPacket != null)
        {
            var inputData = new
            {
                SrcIp = ipPacket.SourceAddress.ToString(),
                DstIp = ipPacket.DestinationAddress.ToString(),
                Sport = tcpPacket.SourcePort,
                Dsport = tcpPacket.DestinationPort,
                Proto = ipPacket.Protocol.ToString(),
                Sbytes = tcpPacket.PayloadData.Length,
                Dbytes = tcpPacket.TotalPacketLength,
                Sttl = ipPacket.TimeToLive
            };

            _kafkaProducer.ProduceAsync(inputData).Wait();
        }
    }
}
