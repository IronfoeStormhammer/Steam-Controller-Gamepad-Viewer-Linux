using System.Buffers.Binary;
using SteamControllerGamepadViewer.State;
namespace SteamControllerGamepadViewer.Hid;

// Original 2015 controller: complete its portable upstream pad parser with buttons,
// 8-bit trigger depth and multiplexed stick/pad state. Triton stays upstream-owned.
internal static class LinuxReportParser
{
    public static bool TryParse(ReadOnlySpan<byte> report, SteamHidSnapshot? previous, out SteamHidSnapshot snapshot)
    {
        if (!SteamHidTouchpadService.TryParseSnapshot(report, out snapshot)) return false;
        if (report.Length < 24 || report[0] != 1 || report[1] != 0 || report[2] != 1) return true;
        var buttons = (uint)(report[8] | report[9] << 8 | report[10] << 16);
        bool B(int bit) => (buttons & (1u << bit)) != 0;
        var padFrame = B(19);
        var multiplex = B(23);
        var axes = new ControllerAxes
        {
            LeftTrigger = report[11] / 255d, RightTrigger = report[12] / 255d,
            LeftStickX = padFrame ? (multiplex ? previous?.Axes?.LeftStickX ?? 0 : 0) : Axis(report, 16),
            LeftStickY = padFrame ? (multiplex ? previous?.Axes?.LeftStickY ?? 0 : 0) : -Axis(report, 18),
        };
        var left = snapshot.Left;
        if (!padFrame && multiplex && previous is { } last)
            left = left with { X = last.Left.X, Y = last.Left.Y };
        else if (!padFrame)
            left = left with { X = 0.5, Y = 0.5 };
        // Classic hardware has click switches, not measured pad pressure.
        left = left with { Pressure = left.Clicked ? 1 : 0 };
        snapshot = snapshot with
        {
            RawButtons = buttons, Axes = axes, Left = left,
            Right = snapshot.Right with { Pressure = snapshot.Right.Clicked ? 1 : 0 },
            Buttons = new ControllerButtons
            {
                A = B(7), B = B(5), X = B(6), Y = B(4),
                LeftBumper = B(3), RightBumper = B(2),
                DpadUp = B(8), DpadRight = B(9), DpadLeft = B(10), DpadDown = B(11),
                View = B(12), Steam = B(13), Menu = B(14),
                LeftGripUpper = B(15), RightGripUpper = B(16), LeftStick = B(22),
            }
        };
        return true;
    }
    private static double Axis(ReadOnlySpan<byte> bytes, int offset)
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]);
        return value / (value < 0 ? 32768d : 32767d);
    }
}
