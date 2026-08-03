using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class MapEditorFileDialog
{
    private const string LastDirectoryKey = "MapEditor.LastFileDialogDirectory";

    public static string OpenFile(string title, string extension)
    {
#if UNITY_EDITOR
        return EditorUtility.OpenFilePanel(title, GetInitialDirectory(), extension);
#elif UNITY_STANDALONE_WIN
        return WindowsFileDialog.Open(title, extension);
#else
        Debug.LogWarning("This platform does not provide a native file picker.");
        return string.Empty;
#endif
    }

    public static string SaveFile(string title, string defaultFileName, string extension)
    {
#if UNITY_EDITOR
        return EditorUtility.SaveFilePanel(title, GetInitialDirectory(), defaultFileName, extension);
#elif UNITY_STANDALONE_WIN
        return WindowsFileDialog.Save(title, defaultFileName, extension);
#else
        return Path.Combine(Application.persistentDataPath, EnsureExtension(defaultFileName, extension));
#endif
    }

    public static string SelectFolder(string title, string defaultFolderName)
    {
#if UNITY_EDITOR
        return EditorUtility.SaveFolderPanel(title, GetInitialDirectory(), defaultFolderName);
#elif UNITY_STANDALONE_WIN
        return WindowsFileDialog.SelectFolder(title, defaultFolderName);
#else
        string folderName = string.IsNullOrWhiteSpace(defaultFolderName) ? "MapPackage" : defaultFolderName;
        return Path.Combine(Application.persistentDataPath, folderName);
#endif
    }

    public static void RememberDirectory(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        PlayerPrefs.SetString(LastDirectoryKey, directory);
        PlayerPrefs.Save();
    }

    private static string GetInitialDirectory()
    {
        string saved = PlayerPrefs.GetString(LastDirectoryKey, string.Empty);
        return Directory.Exists(saved) ? saved : string.Empty;
    }

    private static string EnsureExtension(string fileName, string extension)
    {
        string normalizedExtension = extension.TrimStart('.');
        return Path.HasExtension(fileName) ? fileName : fileName + "." + normalizedExtension;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private static class WindowsFileDialog
    {
        public static string Open(string title, string extension)
        {
            string path = RunPowerShellDialog("Open", title, string.Empty, extension);
            RememberDirectory(path);
            return path;
        }

        public static string Save(string title, string defaultFileName, string extension)
        {
            string path = RunPowerShellDialog("Save", title, EnsureExtension(defaultFileName, extension), extension);
            RememberDirectory(path);
            return path;
        }

        public static string SelectFolder(string title, string defaultFolderName)
        {
            string selectedFolder = RunPowerShellDialog("Folder", title, string.Empty, string.Empty);
            if (string.IsNullOrEmpty(selectedFolder))
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(defaultFolderName))
            {
                selectedFolder = Path.Combine(selectedFolder, defaultFolderName);
            }

            RememberDirectory(selectedFolder);
            return selectedFolder;
        }

        private const string PowerShellDialogScript = @"
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$owner = New-Object System.Windows.Forms.Form
$owner.TopMost = $true
$owner.ShowInTaskbar = $false
$owner.Opacity = 0
$owner.Width = 1
$owner.Height = 1
$owner.StartPosition = 'CenterScreen'
$owner.Show()
$owner.Activate()
try {
    $mode = $env:MAPEDITOR_DIALOG_MODE
    $title = $env:MAPEDITOR_DIALOG_TITLE
    $initial = $env:MAPEDITOR_DIALOG_INITIAL
    $extension = $env:MAPEDITOR_DIALOG_EXTENSION
    $defaultName = $env:MAPEDITOR_DIALOG_DEFAULT
    if ($mode -eq 'Folder') {
        $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
        $dialog.Description = $title
        $dialog.ShowNewFolderButton = $true
        if ($dialog.ShowDialog($owner) -eq [System.Windows.Forms.DialogResult]::OK) {
            [Console]::Out.Write($dialog.SelectedPath)
        }
    } else {
        if ($mode -eq 'Save') {
            $dialog = New-Object System.Windows.Forms.SaveFileDialog
            $dialog.FileName = $defaultName
            $dialog.OverwritePrompt = $true
        } else {
            $dialog = New-Object System.Windows.Forms.OpenFileDialog
            $dialog.CheckFileExists = $true
        }
        $dialog.Title = $title
        $dialog.InitialDirectory = $initial
        $dialog.DefaultExt = $extension
        $dialog.AddExtension = $true
        $dialog.Filter = $extension.ToUpperInvariant() + ' files (*.' + $extension + ')|*.' + $extension + '|All files (*.*)|*.*'
        if ($dialog.ShowDialog($owner) -eq [System.Windows.Forms.DialogResult]::OK) {
            [Console]::Out.Write($dialog.FileName)
        }
    }
} finally {
    $owner.Close()
    $owner.Dispose()
}
";

        private static string RunPowerShellDialog(
            string mode,
            string title,
            string defaultFileName,
            string extension)
        {
            try
            {
                byte[] scriptBytes = Encoding.Unicode.GetBytes(PowerShellDialogScript);
                string encodedScript = System.Convert.ToBase64String(scriptBytes);
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoLogo -NoProfile -STA -NonInteractive -EncodedCommand " + encodedScript,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                startInfo.EnvironmentVariables["MAPEDITOR_DIALOG_MODE"] = mode;
                startInfo.EnvironmentVariables["MAPEDITOR_DIALOG_TITLE"] = title ?? string.Empty;
                startInfo.EnvironmentVariables["MAPEDITOR_DIALOG_INITIAL"] = GetInitialDirectory();
                startInfo.EnvironmentVariables["MAPEDITOR_DIALOG_EXTENSION"] = extension.TrimStart('.');
                startInfo.EnvironmentVariables["MAPEDITOR_DIALOG_DEFAULT"] = defaultFileName ?? string.Empty;

                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Debug.LogError("Windows file dialog process could not be started.");
                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogError("Windows file dialog failed: " + error.Trim());
                        return string.Empty;
                    }

                    return output.Trim();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return string.Empty;
            }
        }

    }
#endif
}
