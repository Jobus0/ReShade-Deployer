using ReShade.Setup.Utilities;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Utilities;

public class ExecutableContextFactoryTests
{
    private readonly MockFileSystem _fileSystem;
    private readonly MockPeInfoReader _peInfoReader;
    private readonly ExecutableContextFactory _factory;

    public ExecutableContextFactoryTests()
    {
        _fileSystem = new MockFileSystem();
        _peInfoReader = new MockPeInfoReader();
        _factory = new ExecutableContextFactory(_fileSystem, _peInfoReader);
        
        // Ensure the AppContext.BaseDirectory and Paths.Lib directory exist in mock file system
        _fileSystem.Directory.CreateDirectory(AppContext.BaseDirectory);
        _fileSystem.Directory.CreateDirectory(Paths.Lib);
    }

    [Fact]
    public void Create_DefaultExecution_ExtractsBasicProperties()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            ["kernel32.dll"]
        );

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal("game.exe", context.FileName);
        Assert.Equal(@"C:\Games\Game", context.DirectoryPath);
        Assert.True(context.IsX64);
        Assert.False(context.IsD3D8);
        Assert.False(context.IsD3D9);
        Assert.False(context.IsDXGI);
        Assert.False(context.IsOpenGL);
        Assert.False(context.IsVulkan);
        Assert.Equal("0", context.DepthReversed);
    }

    [Fact]
    public void Create_UnrealEngineGame_ResolvesTargetExecutable()
    {
        // Arrange
        string bootstrapPath = @"C:\Games\Game\game.exe";
        string targetPath = @"C:\Games\Game\Binaries\Win64\Game-Win64-Shipping.exe";
        
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game\Binaries\Win64");
        _fileSystem.File.WriteAllText(bootstrapPath, "");
        _fileSystem.File.WriteAllText(targetPath, "");

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            ["kernel32.dll"]
        );
        _peInfoReader.ReadResourceStringFunc = (_, id) =>
            id == 201 ? @"Binaries\Win64\Game-Win64-Shipping.exe" : null;

        // Act
        var context = _factory.Create(bootstrapPath);

        // Assert
        Assert.Equal("Game-Win64-Shipping.exe", context.FileName);
        Assert.Equal(@"C:\Games\Game\Binaries\Win64", context.DirectoryPath);
    }

    [Theory]
    [InlineData("d3d8.dll", true, true, false, false, false)]
    [InlineData("d3d9.dll", false, true, false, false, false)]
    [InlineData("dxgi.dll", false, false, true, false, false)]
    [InlineData("d3d11.dll", false, false, true, false, false)]
    [InlineData("GFSDK.dll", false, false, true, false, false)]
    [InlineData("opengl32.dll", false, false, false, true, false)]
    [InlineData("vulkan-1.dll", false, false, false, false, true)]
    public void Create_DetectsRenderApiFromPeModules(
        string moduleName, 
        bool expectedD3D8, 
        bool expectedD3D9, 
        bool expectedDXGI, 
        bool expectedOpenGL, 
        bool expectedVulkan)
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            [moduleName]
        );

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal(expectedD3D8, context.IsD3D8);
        Assert.Equal(expectedD3D9, context.IsD3D9);
        Assert.Equal(expectedDXGI, context.IsDXGI);
        Assert.Equal(expectedOpenGL, context.IsOpenGL);
        Assert.Equal(expectedVulkan, context.IsVulkan);
    }

    [Fact]
    public void Create_ModernCopyrightYear_SetsDepthReversedToTrue()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");
        
        _fileSystem.WithFileVersionInfo(exePath, info => 
            info.SetLegalCopyright("Copyright (C) 2015 Valve Corporation"));

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            []
        );

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal("1", context.DepthReversed);
    }

    [Fact]
    public void Create_LegacyCopyrightYear_SetsDepthReversedToFalse()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");
        
        _fileSystem.WithFileVersionInfo(exePath, info => 
            info.SetLegalCopyright("Copyright (C) 2009 Valve Corporation"));

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            []
        );

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal("0", context.DepthReversed);
    }

    [Fact]
    public void Create_AppliesCompatibilityIniOverrides()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64,
            []
        );

        string compatIniContent = """
                                  [game.exe]
                                  InstallTarget=SubFolder
                                  RenderApi=D3D9
                                  DepthReversed=1
                                  DepthUpsideDown=1
                                  DepthLogarithmic=1
                                  DepthCopyBeforeClears=1
                                  DepthCopyAtClearIndex=5
                                  DrawStatsHeuristic=1
                                  UseAspectRatioHeuristics=0
                                  """;
        _fileSystem.File.WriteAllText(Paths.CompatibilityIni, compatIniContent);

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal("game.exe", context.FileName);
        Assert.Equal(@"C:\Games\Game\SubFolder", context.DirectoryPath);
        Assert.True(context.IsD3D9);
        Assert.Equal("1", context.DepthReversed);
        Assert.Equal("1", context.DepthUpsideDown);
        Assert.Equal("1", context.DepthLogarithmic);
        Assert.Equal("1", context.DepthCopyBeforeClears);
        Assert.Equal("5", context.DepthCopyAtClearIndex);
        Assert.Equal("1", context.DrawStatsHeuristic);
        Assert.Equal("0", context.UseAspectRatioHeuristics);
    }

    [Fact]
    public void Create_NvRemixBridgePresent_OverridesToVulkanAndDirectory()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        string bridgePath = @"C:\Games\Game\.trex\NvRemixBridge.exe";
        
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game\.trex");
        _fileSystem.File.WriteAllText(exePath, "");
        _fileSystem.File.WriteAllText(bridgePath, "");

        _peInfoReader.ReadFunc = _ => new PeInfoData(
            PEInfo.BinaryType.IMAGE_FILE_MACHINE_I386, // 32-bit in PE
            ["d3d9.dll"]
        );

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.True(context.IsVulkan);
        Assert.True(context.IsX64); // NV Remix forces x64
        Assert.Equal(@"C:\Games\Game\.trex", context.DirectoryPath);
    }

    [Fact]
    public void Create_PeReaderThrowsException_GracefulFallbackToDefault()
    {
        // Arrange
        string exePath = @"C:\Games\Game\game.exe";
        _fileSystem.Directory.CreateDirectory(@"C:\Games\Game");
        _fileSystem.File.WriteAllText(exePath, "");

        _peInfoReader.ReadFunc = _ => throw new Exception("Access Denied");

        // Act
        var context = _factory.Create(exePath);

        // Assert
        Assert.Equal("game.exe", context.FileName);
        Assert.Equal(@"C:\Games\Game", context.DirectoryPath);
        Assert.True(context.IsX64);
        Assert.False(context.IsVulkan);
    }
}

public class MockPeInfoReader : IPeInfoReader
{
    public Func<string, PeInfoData>? ReadFunc { get; set; }
    public Func<string, ushort, string?>? ReadResourceStringFunc { get; set; }

    public PeInfoData Read(string executablePath)
    {
        if (ReadFunc != null)
            return ReadFunc(executablePath);
        
        return new PeInfoData(PEInfo.BinaryType.IMAGE_FILE_MACHINE_AMD64, Array.Empty<string>());
    }

    public string? ReadResourceString(string executablePath, ushort id)
    {
        if (ReadResourceStringFunc != null)
            return ReadResourceStringFunc(executablePath, id);
        
        return null;
    }
}