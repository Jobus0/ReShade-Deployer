using System;
using System.IO;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace ReShadeDeployer;

public class Config
{
    private readonly FileIniDataParser iniParser;
    private IniData? ini;

    private bool awaitingSave;
    
    public string LatestVersion
    {
        get => Read("LatestVersion");
        set => Write("LatestVersion", value);
    }
    
    public DateTime LatestVersionCheckDate
    {
        get => DateTime.TryParse(Read("LatestVersionCheckDate"), out DateTime value) ? value : default;
        set => Write("LatestVersionCheckDate", value.ToString("yyyy-MM-dd"));
    }

    public Config()
    {
        iniParser = new FileIniDataParser();
    }

    private void InitializeIni()
    {
        if (File.Exists(Paths.ConfigIni))
            ini = iniParser.ReadFile(Paths.ConfigIni);
        else
            ini = new IniData();
    }

    private string Read(string key)
    {
        if (ini == null)
            InitializeIni();
        
        return ini!.Global[key];
    }
    
    private void Write(string key, string value)
    {
        if (ini == null)
            InitializeIni();

        if (ini!.Global[key] == value)
            return;
        
        ini!.Global[key] = value;
        DeferredSave();
    }

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