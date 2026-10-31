using System.Text.Json;
using Xunit;
using SteamControllerGamepadViewer;

public sealed class PressColorTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "viewer-colors-" + Guid.NewGuid().ToString("N"));
    private string ConfigPath => Path.Combine(directory, "colors.json");
    public void Dispose() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    [Fact] public void MissingFileUsesExactOriginalBlue()
    {
        var value = new PressColorStore(ConfigPath).Current;
        Assert.Equal("global", value.Mode); Assert.Equal("#25a7ff", value.GlobalColor); Assert.Empty(value.Controls);
        Assert.Contains("a", PressColorStore.ControlNames); Assert.Contains("left-pad-click", PressColorStore.ControlNames);
    }
    [Theory]
    [InlineData("{")][InlineData("null")][InlineData("[]")]
    [InlineData("{\"mode\":\"invalid\"}")]
    [InlineData("{\"globalColor\":\"red\"}")]
    [InlineData("{\"controls\":null}")]
    [InlineData("{\"controls\":{\"a\":\"javascript:bad\"}}")]
    public void MalformedConfigurationFallsBack(string json)
    {
        Directory.CreateDirectory(directory); File.WriteAllText(ConfigPath,json);
        Assert.Equal(new PressColorConfig().GlobalColor,new PressColorStore(ConfigPath).Current.GlobalColor);
        Assert.Equal("global",new PressColorStore(ConfigPath).Current.Mode);
    }
    [Fact] public void GlobalModeIgnoresPerControlOverrides()
    {
        var value=new PressColorConfig {GlobalColor="#ff0000",Controls=new(){{"a","#00ff00"}}};
        Assert.Equal("#ff0000",value.Resolve("a")); Assert.Equal("#ff0000",value.Resolve("rt"));
    }
    [Fact] public void PerControlModeUsesOverridesAndGlobalFallback()
    {
        var value=new PressColorConfig {Mode="per-control",GlobalColor="#ff0000",Controls=new(){{"a","#00ff00"},{"b","#800080"}}};
        Assert.Equal("#00ff00",value.Resolve("a")); Assert.Equal("#800080",value.Resolve("b")); Assert.Equal("#ff0000",value.Resolve("rt"));
    }
    [Fact] public void SerializationAndRestartRetainBothModesAndColors()
    {
        var store=new PressColorStore(ConfigPath);
        var value=new PressColorConfig {Mode="per-control",GlobalColor="#FF0000",Controls=new(){{"a","#00FF00"},{"b","#800080"}}};
        store.Save(value);
        var loaded=new PressColorStore(ConfigPath).Current;
        Assert.Equal("per-control",loaded.Mode); Assert.Equal("#00ff00",loaded.Resolve("a")); Assert.Equal("#800080",loaded.Resolve("b"));
        Assert.Equal("#ff0000",loaded.GlobalColor);
        Assert.Equal("#00ff00",PressColorStore.Parse(JsonSerializer.Serialize(loaded,PressColorStore.JsonOptions)).Resolve("a"));
        store.Save(loaded with {Mode="global"});Assert.Equal("global",new PressColorStore(ConfigPath).Current.Mode);
    }
    [Fact] public void ResetIsPersistedAndRemovesOverrides()
    {
        var store=new PressColorStore(ConfigPath);store.Save(new(){Mode="per-control",Controls=new(){{"a","#00ff00"}}});store.Reset();
        var reset=new PressColorStore(ConfigPath).Current;
        Assert.Equal("global",reset.Mode);Assert.Equal("#25a7ff",reset.GlobalColor);Assert.Empty(reset.Controls);
    }
    [Fact] public void InvalidSaveDoesNotReplaceExistingFileOrState()
    {
        var store=new PressColorStore(ConfigPath);store.Save(new(){GlobalColor="#ff0000"});
        Assert.Throws<ArgumentException>(()=>store.Save(new(){GlobalColor="invalid"}));
        Assert.Equal("#ff0000",store.Current.GlobalColor);Assert.Equal("#ff0000",new PressColorStore(ConfigPath).Current.GlobalColor);
    }
    [Fact] public void ExplicitSaveAndResetOverrideLegacyUrlDefaultsAcrossRestart()
    {
        var store=new PressColorStore(ConfigPath);Assert.False(store.HasSavedSettings);
        store.Save(new());Assert.True(store.HasSavedSettings);
        Assert.True(new PressColorStore(ConfigPath).HasSavedSettings);
        store.Reset();Assert.True(store.HasSavedSettings);
    }
    [Fact] public void CallerCannotMutateSavedState()
    {
        var store=new PressColorStore(ConfigPath);var value=new PressColorConfig {Mode="per-control",Controls=new(){{"a","#00ff00"}}};store.Save(value);
        value.Controls["a"]="#ff0000";store.Current.Controls["a"]="#ff0000";Assert.Equal("#00ff00",store.Current.Resolve("a"));
    }
}
