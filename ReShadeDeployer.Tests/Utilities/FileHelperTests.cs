using System.IO.Abstractions;
using Testably.Abstractions.Testing;

namespace ReShadeDeployer.Tests.Utilities;

public class FileHelperTests
{
    private readonly IFileSystem _fileSystem;
    private readonly FileHelper _fileHelper;

    public FileHelperTests()
    {
        _fileSystem = new MockFileSystem();
        _fileHelper = new FileHelper(_fileSystem);
    }

    [Fact]
    public void IsFileLocked_ReturnsFalse_WhenFileIsNotLocked()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string filePath = _fileSystem.Path.Combine(tempDir, "test.txt");
        _fileSystem.File.WriteAllText(filePath, "test content");

        // Act
        bool result = _fileHelper.IsFileLocked(filePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFileLocked_ReturnsTrue_WhenFileDoesNotExist()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string nonExistentPath = _fileSystem.Path.Combine(tempDir, "nonexistent.txt");

        // Act
        bool result = _fileHelper.IsFileLocked(nonExistentPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetFileSystemInfo_ReturnsFileInfo_WhenPathIsFile()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string filePath = _fileSystem.Path.Combine(tempDir, "test.txt");
        _fileSystem.File.WriteAllText(filePath, "test content");

        // Act
        var result = _fileHelper.GetFileSystemInfo(filePath);

        // Assert
        Assert.IsAssignableFrom<IFileInfo>(result);
        Assert.Equal(filePath, result.FullName);
    }

    [Fact]
    public void GetFileSystemInfo_ReturnsDirectoryInfo_WhenPathIsDirectory()
    {
        // Arrange
        string dirPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "testdir");
        _fileSystem.Directory.CreateDirectory(dirPath);

        // Act
        var result = _fileHelper.GetFileSystemInfo(dirPath);

        // Assert
        Assert.IsAssignableFrom<IDirectoryInfo>(result);
        Assert.Equal(dirPath, result.FullName);
    }

    [Fact]
    public void ResolveLinkPath_ReturnsNull_WhenPathIsNotALink()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string filePath = _fileSystem.Path.Combine(tempDir, "test.txt");
        _fileSystem.File.WriteAllText(filePath, "test content");

        // Act
        string? result = _fileHelper.ResolveLinkPath(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Copy_CopiesFile_WhenSourceIsFile()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string sourceFile = _fileSystem.Path.Combine(tempDir, "source.txt");
        string destFile = _fileSystem.Path.Combine(tempDir, "dest.txt");
        _fileSystem.File.WriteAllText(sourceFile, "test content");

        // Act
        _fileHelper.Copy(sourceFile, destFile, overwrite: false);

        // Assert
        Assert.True(_fileSystem.File.Exists(destFile));
        Assert.Equal("test content", _fileSystem.File.ReadAllText(destFile));
    }

    [Fact]
    public void Copy_CopiesDirectory_WhenSourceIsDirectory()
    {
        // Arrange
        string sourceDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "sourcedir");
        string destDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "destdir");
        _fileSystem.Directory.CreateDirectory(sourceDir);
        
        string file1 = _fileSystem.Path.Combine(sourceDir, "file1.txt");
        string file2 = _fileSystem.Path.Combine(sourceDir, "subdir", "file2.txt");
        _fileSystem.File.WriteAllText(file1, "content1");
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(sourceDir, "subdir"));
        _fileSystem.File.WriteAllText(file2, "content2");

        // Act
        _fileHelper.Copy(sourceDir, destDir, overwrite: false);

        // Assert
        Assert.True(_fileSystem.Directory.Exists(destDir));
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(destDir, "file1.txt")));
        Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(destDir, "subdir", "file2.txt")));
        Assert.Equal("content1", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(destDir, "file1.txt")));
        Assert.Equal("content2", _fileSystem.File.ReadAllText(_fileSystem.Path.Combine(destDir, "subdir", "file2.txt")));
    }

    [Fact]
    public void Copy_OverwritesFile_WhenOverwriteIsTrue()
    {
        // Arrange
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string sourceFile = _fileSystem.Path.Combine(tempDir, "source.txt");
        string destFile = _fileSystem.Path.Combine(tempDir, "dest.txt");
        _fileSystem.File.WriteAllText(sourceFile, "new content");
        _fileSystem.File.WriteAllText(destFile, "old content");

        // Act
        _fileHelper.Copy(sourceFile, destFile, overwrite: true);

        // Assert
        Assert.Equal("new content", _fileSystem.File.ReadAllText(destFile));
    }

    [Fact]
    public void AreFilesEqual_ReturnsFalse_WhenFirstFileDoesNotExist()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file2 = _fileSystem.Path.Combine(tempDir, "file2.txt");
        _fileSystem.File.WriteAllText(file2, "content");

        bool result = _fileHelper.AreFilesEqual(_fileSystem.Path.Combine(tempDir, "nonexistent.txt"), file2);

        Assert.False(result);
    }

    [Fact]
    public void AreFilesEqual_ReturnsFalse_WhenSecondFileDoesNotExist()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file1 = _fileSystem.Path.Combine(tempDir, "file1.txt");
        _fileSystem.File.WriteAllText(file1, "content");

        bool result = _fileHelper.AreFilesEqual(file1, _fileSystem.Path.Combine(tempDir, "nonexistent.txt"));

        Assert.False(result);
    }

    [Fact]
    public void AreFilesEqual_ReturnsFalse_WhenLengthsDiffer()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file1 = _fileSystem.Path.Combine(tempDir, "file1.txt");
        string file2 = _fileSystem.Path.Combine(tempDir, "file2.txt");
        _fileSystem.File.WriteAllText(file1, "short");
        _fileSystem.File.WriteAllText(file2, "longer content");

        bool result = _fileHelper.AreFilesEqual(file1, file2);

        Assert.False(result);
    }

    [Fact]
    public void AreFilesEqual_ReturnsFalse_WhenLengthsMatchButContentsDiffer()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file1 = _fileSystem.Path.Combine(tempDir, "file1.txt");
        string file2 = _fileSystem.Path.Combine(tempDir, "file2.txt");
        _fileSystem.File.WriteAllText(file1, "abcde");
        _fileSystem.File.WriteAllText(file2, "abxyz");

        bool result = _fileHelper.AreFilesEqual(file1, file2);

        Assert.False(result);
    }

    [Fact]
    public void AreFilesEqual_ReturnsTrue_WhenFilesAreIdentical()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file1 = _fileSystem.Path.Combine(tempDir, "file1.txt");
        string file2 = _fileSystem.Path.Combine(tempDir, "file2.txt");
        _fileSystem.File.WriteAllText(file1, "same content");
        _fileSystem.File.WriteAllText(file2, "same content");

        bool result = _fileHelper.AreFilesEqual(file1, file2);

        Assert.True(result);
    }

    [Fact]
    public void AreFilesEqual_ReturnsTrue_WhenPathsAreSameAndFileExists()
    {
        string tempDir = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "test");
        _fileSystem.Directory.CreateDirectory(tempDir);
        string file1 = _fileSystem.Path.Combine(tempDir, "file1.txt");
        _fileSystem.File.WriteAllText(file1, "same file");

        bool result = _fileHelper.AreFilesEqual(file1, file1);

        Assert.True(result);
    }
}
