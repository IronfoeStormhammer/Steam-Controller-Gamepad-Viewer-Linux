using Xunit;
using System.Buffers.Binary;
using SteamControllerGamepadViewer.Hid;

public class ReportTests
{
    static byte[] Triton(uint flags = 0) { var p = new byte[54]; p[0] = 0x42; BinaryPrimitives.WriteUInt32LittleEndian(p.AsSpan(2), flags); return p; }
    [Fact] public void RejectsEveryTruncatedPacket()
    {
        var packet = Triton(uint.MaxValue);
        for (var size = 0; size < 30; size++) Assert.False(LinuxReportParser.TryParse(packet.AsSpan(0, size), null, out _));
        Assert.False(LinuxReportParser.TryParse(new byte[64], null, out _));
        Assert.False(LinuxReportParser.TryParse(new byte[] {0x43,1,0,0}, null, out _));
    }
    [Theory]
    [InlineData(0x00020000u, "LeftGripUpper")][InlineData(0x00040000u, "LeftGripLower")]
    [InlineData(0x00000080u, "RightGripUpper")][InlineData(0x00000100u, "RightGripLower")]
    [InlineData(0x20000000u, "LeftGripTouch")][InlineData(0x10000000u, "RightGripTouch")]
    [InlineData(0x01000000u, "LeftStickTouch")][InlineData(0x00100000u, "RightStickTouch")]
    [InlineData(1u, "A")][InlineData(2u, "B")][InlineData(4u, "X")][InlineData(8u, "Y")]
    [InlineData(0x10000u, "Steam")][InlineData(0x4000u, "View")][InlineData(0x40u, "Menu")]
    [InlineData(0x80000u, "LeftBumper")][InlineData(0x200u, "RightBumper")]
    public void ButtonPressAndRelease(uint flag, string property)
    {
        Assert.True(LinuxReportParser.TryParse(Triton(flag), null, out var pressed));
        Assert.True((bool)pressed.Buttons!.GetType().GetProperty(property)!.GetValue(pressed.Buttons)!);
        Assert.True(LinuxReportParser.TryParse(Triton(), pressed, out var released));
        Assert.False((bool)released.Buttons!.GetType().GetProperty(property)!.GetValue(released.Buttons)!);
    }
    [Theory][InlineData(0x42)][InlineData(0x45)]
    public void PadsTriggersAndSignedExtremes(int type)
    {
        var p = Triton(0x06600000); p[0] = (byte)type;
        foreach (var offset in new[]{6,8,22,28}) BinaryPrimitives.WriteUInt16LittleEndian(p.AsSpan(offset), 32767);
        foreach (var offset in new[]{10,12,18,20,24,26}) BinaryPrimitives.WriteInt16LittleEndian(p.AsSpan(offset), -32768);
        Assert.True(LinuxReportParser.TryParse(p, null, out var s));
        Assert.True(s.Left.Touched && s.Left.Clicked && s.Right.Touched && s.Right.Clicked);
        Assert.Equal(1, s.Left.Pressure); Assert.Equal(1, s.Right.Pressure);
        Assert.Equal(1, s.Axes!.LeftTrigger); Assert.Equal(-1, s.Axes.LeftStickX);
        Assert.Equal(1, s.Axes.LeftStickY); Assert.Equal(1, s.Left.Y);
        Assert.InRange(s.Left.X, 0, 0.0001);
    }
    [Fact] public void ClassicMultiplexKeepsIndependentCoordinatesAndReleasesButtons()
    {
        var p = new byte[64]; p[0]=1; p[2]=1; p[8]=128; p[9]=128; p[11]=128;
        BinaryPrimitives.WriteInt16LittleEndian(p.AsSpan(16), 16384);
        Assert.True(LinuxReportParser.TryParse(p, null, out var stick));
        Assert.True(stick.Buttons!.A && stick.Buttons.LeftGripUpper);
        Assert.InRange(stick.Axes!.LeftTrigger, .50, .51);
        p[8]=0; p[9]=0; p[10]=0x88;
        BinaryPrimitives.WriteInt16LittleEndian(p.AsSpan(16), -16384);
        Assert.True(LinuxReportParser.TryParse(p, stick, out var pad));
        Assert.Equal(stick.Axes.LeftStickX, pad.Axes!.LeftStickX);
        Assert.True(pad.Left.Touched); Assert.False(pad.Buttons!.A);
        p[10]=0x80;
        Assert.True(LinuxReportParser.TryParse(p, pad, out var nextStick));
        Assert.Equal(pad.Left.X, nextStick.Left.X);
    }
    [Fact] public void VendorDescriptorFilterExcludesMouse()
    {
        Assert.True(LinuxHidService.HasVendorPage(new byte[]{6,0,255,9,1}));
        Assert.False(LinuxHidService.HasVendorPage(new byte[]{5,1,9,2}));
    }
    [Fact] public void StateExpiresAndCannotLeaveStuckButtons()
    {
        var state = new SteamHidState();
        Assert.False(state.TryGetCurrent(TimeSpan.FromSeconds(1), out _));
        LinuxReportParser.TryParse(Triton(1), null, out var pressed); state.Update(pressed);
        Assert.True(state.TryGetCurrent(TimeSpan.FromSeconds(1), out _));
        Assert.False(state.TryGetCurrent(TimeSpan.FromTicks(-1), out _));
    }
}
