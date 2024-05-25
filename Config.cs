using System;
using System.IO;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace ReShadeDeployer;

/// <summary>
/// Handles the reading and writing of Config.ini. Writing to file is deferred so that it is only done once even if multiple changes are made at once.
/// </summary>
public class Config
{
    private readonly FileIniDataParser iniParser = new();
    private IniData? ini;

    /// <summary>
    /// An ini value was recently modified and is awaiting a save to file.
    /// </summary>
    private bool awaitingSave;
    
    /// <summary>
    /// Represents the latest online ReShade version number.
    /// </summary>
    public string LatestReShadeVersionNumber
    {
        get => Read(nameof(LatestReShadeVersionNumber));
        set => Write(nameof(LatestReShadeVersionNumber), value);
    }
    
    /// <summary>
    /// Represents the date of the last check for a new ReShade version.
    /// </summary>
    public DateTime LatestReShadeVersionNumberCheckDate
    {
        get => DateTime.TryParse(Read(nameof(LatestReShadeVersionNumberCheckDate)), out DateTime value) ? value : default;
        set => Write(nameof(LatestReShadeVersionNumberCheckDate), value.ToString("yyyy-MM-dd"));
    }
    
    /// <summary>
    /// Represents the latest online ReShade Deployer version number.
    /// </summary>
    public string LatestDeployerVersionNumber
    {
        get => Read(nameof(LatestDeployerVersionNumber));
        set => Write(nameof(LatestDeployerVersionNumber), value);
    }
    
    /// <summary>
    /// Represents the date of the last check for a new ReShade Deployer version.
    /// </summary>
    public DateTime LatestDeployerVersionNumberCheckDate
    {
        get => DateTime.TryParse(Read(nameof(LatestDeployerVersionNumberCheckDate)), out DateTime value) ? value : default;
        set => Write(nameof(LatestDeployerVersionNumberCheckDate), value.ToString("yyyy-MM-dd"));
    }
    
    /// <summary>
    /// Whether the application should automatically exit after deployment.
    /// </summary>
    public bool AlwaysExitOnDeploy
    {
        get => bool.TryParse(Read(nameof(AlwaysExitOnDeploy)), out bool value) && value;
        set => Write(nameof(AlwaysExitOnDeploy), value.ToString());
    }

    /// <summary>
    /// Load ini data from Config.ini if it exists; otherwise create a new one.
    /// </summary>
    private void InitializeIni()
    {
        if (File.Exists(Paths.ConfigIni))
            ini = iniParser.ReadFile(Paths.ConfigIni);
        else
            ini = new IniData();
    }

    /// <summary>
    /// Read a value by key from Config.ini
    /// </summary>
    private string Read(string key)
    {
        if (ini == null)
            InitializeIni();
        
        return ini!.Global[key];
    }
    
    /// <summary>
    /// Write a value by key to Config.ini
    /// </summary>
    private void Write(string key, string value)
    {
        if (ini == null)
            InitializeIni();

        if (ini!.Global[key] == value)
            return;
        
        ini!.Global[key] = value;
        DeferredSave();
    }

    /// <summary>
    /// Trigger a delayed save to Config.ini. Ignored if save is already in progress.
    /// </summary>
    private async void DeferredSave()
    {
        if (awaitingSave)
            return;
        
        awaitingSave = true;
        await Task.Delay(100);
        Save();
        awaitingSave = false;
    }
    
    /// <summary>
    /// Write ini data to Config.ini file.
    /// </summary>
    private void Save() => iniParser.WriteFile(Paths.ConfigIni, ini);
}