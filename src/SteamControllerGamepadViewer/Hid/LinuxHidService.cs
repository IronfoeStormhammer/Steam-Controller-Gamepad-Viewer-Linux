using System.Runtime.InteropServices;
using SteamControllerGamepadViewer.State;

namespace SteamControllerGamepadViewer.Hid;

// Each hidraw open has its own input queue. Never write, grab, or send feature reports.
internal sealed class LinuxHidService(ControllerStateHub hub, SteamHidState state,
    ILogger<LinuxHidService> logger) : BackgroundService
{
    private sealed record Device(string Path, int Product, int Fd) { public SteamHidSnapshot? Previous { get; set; } }
    [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
    [DllImport("libc", SetLastError = true)] private static extern nint read(int fd, byte[] buffer, nuint count);
    [DllImport("libc")] private static extern int close(int fd);
    private readonly Dictionary<string, Device> devices = new();
    private readonly byte[] buffer = new byte[512];
    private string? selected;
    private DateTimeOffset lastState;
    private DateTimeOffset lastLog;
    private int selectedProduct;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Passive Linux hidraw backend; Steam may remain running. http://127.0.0.1:31337/ (diagnostics /diagnostics.html)");
        try
        {
            var nextScan = DateTimeOffset.MinValue;
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                if (now >= nextScan)
                {
                    Scan();
                    nextScan = now.AddSeconds(1);
                }
                foreach (var device in devices.Values.ToArray())
                {
                    // Bound draining so a fast endpoint cannot starve other devices or shutdown.
                    for (var packet = 0; packet < 128; packet++)
                    {
                        var length = (int)read(device.Fd, buffer, (nuint)buffer.Length);
                        if (length < 0)
                        {
                            var error = Marshal.GetLastPInvokeError();
                            if (error != 11 && error != 4) Remove(device);
                            break;
                        }
                        if (length == 0) { Remove(device); break; }
                        // Prefer the external controller over the Deck, and retain the active
                        // receiver slot until it has gone quiet; never blend two controllers.
                        if (selected != null && selected != device.Path && now - lastState < TimeSpan.FromSeconds(1.5)
                            && !(selectedProduct == 0x1205 && device.Product != 0x1205)) break;
                        if (!LinuxReportParser.TryParse(buffer.AsSpan(0, length), device.Previous, out var snapshot)) continue;
                        device.Previous = snapshot;
                        if (selected != device.Path)
                        {
                            selected = device.Path;
                            selectedProduct = device.Product;
                            state.MarkOpened(device.Path);
                            logger.LogInformation("Live physical HID selected: {Path} 28de:{Product:x4}, report 0x{Report:x2}, {Length} bytes", device.Path, device.Product, buffer[0], length);
                        }
                        state.MarkReport(length, buffer[0]);
                        state.Update(snapshot);
                        lastState = now;
                        hub.Publish(new ControllerSnapshot
                        {
                            Connected = true, Status = "connected", Name = "Steam Controller (passive HID)",
                            VendorId = 0x28de, ProductId = device.Product,
                            Buttons = snapshot.Buttons ?? new(), Axes = snapshot.Axes ?? new(),
                            LeftTouchpad = snapshot.Left, RightTouchpad = snapshot.Right,
                        });
                    }
                }
                if (now - lastState > TimeSpan.FromSeconds(1.5))
                {
                    selected = null;
                    hub.Publish(ControllerSnapshot.Starting() with { Status = "disconnected", Name = "Waiting for physical Steam Controller" });
                }
                if (now - lastLog > TimeSpan.FromSeconds(10))
                {
                    logger.LogDebug("HID devices {Count}; parsed {Parsed}; last state {Last}", devices.Count, state.Status.ReportsParsed, state.Status.LastReportAt);
                    lastLog = now;
                }
                await Task.Delay(4, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { foreach (var device in devices.Values.ToArray()) Remove(device); }
    }

    private void Remove(Device device)
    {
        close(device.Fd);
        devices.Remove(device.Path);
        if (selected == device.Path) { selected = null; lastState = DateTimeOffset.MinValue; }
    }

    private void Scan()
    {
        if (!Directory.Exists("/sys/class/hidraw")) { state.MarkScan(0); return; }
        foreach (var directory in Directory.EnumerateDirectories("/sys/class/hidraw"))
        {
            var path = "/dev/" + Path.GetFileName(directory);
            if (devices.ContainsKey(path)) continue;
            try
            {
                var lines = File.ReadAllLines(directory + "/device/uevent");
                var id = lines.FirstOrDefault(l => l.StartsWith("HID_ID="))?.Split(':');
                if (id is not { Length: 3 } || Convert.ToInt32(id[1], 16) != 0x28de) continue;
                var product = Convert.ToInt32(id[2], 16);
                if (product is not (0x1102 or 0x1142 or 0x1302 or 0x1304)) continue;
                using var descriptorStream = File.OpenRead(directory + "/device/report_descriptor");
                using var descriptorBytes = new MemoryStream();
                descriptorStream.CopyTo(descriptorBytes);
                var descriptor = descriptorBytes.ToArray();
                // Only vendor input interfaces; exclude mouse/keyboard-only interfaces.
                if (!HasVendorPage(descriptor)) continue;
                var fd = open(path, 0x800 | 0x80000); // O_RDONLY | O_NONBLOCK | O_CLOEXEC
                if (fd < 0) { state.MarkError($"Cannot read {path}: errno {Marshal.GetLastPInvokeError()}. See install-udev.sh"); continue; }
                devices.Add(path, new Device(path, product, fd));
                logger.LogInformation("Observing {Path}, 28de:{Product:x4} (read-only)", path, product);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { state.MarkError(ex.Message); }
        }
        state.MarkScan(devices.Count);
    }

    internal static bool HasVendorPage(ReadOnlySpan<byte> descriptor)
    {
        for (var i = 0; i + 2 < descriptor.Length; i++)
            if (descriptor[i] == 0x06 && descriptor[i + 2] == 0xff) return true;
        return false;
    }
}
