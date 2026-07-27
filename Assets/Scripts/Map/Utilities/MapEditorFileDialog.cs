using System.IO;
using System.Runtime.InteropServices;
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
        private const int MaxPath = 4096;
        private const int OfnExplorer = 0x00080000;
        private const int OfnPathMustExist = 0x00000800;
        private const int OfnFileMustExist = 0x00001000;
        private const int OfnNoChangeDir = 0x00000008;
        private const int OfnOverwritePrompt = 0x00000002;
        private const uint BifReturnOnlyFsDirs = 0x00000001;
        private const uint BifNewDialogStyle = 0x00000040;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BrowseInfo
        {
            public System.IntPtr owner;
            public System.IntPtr root;
            public System.IntPtr displayName;
            public string title;
            public uint flags;
            public System.IntPtr callback;
            public System.IntPtr parameter;
            public int image;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int structSize;
            public System.IntPtr owner;
            public System.IntPtr instance;
            public string filter;
            public string customFilter;
            public int maxCustomFilter;
            public int filterIndex;
            public StringBuilder file;
            public int maxFile;
            public StringBuilder fileTitle;
            public int maxFileTitle;
            public string initialDirectory;
            public string title;
            public int flags;
            public short fileOffset;
            public short fileExtension;
            public string defaultExtension;
            public System.IntPtr customData;
            public System.IntPtr hook;
            public string templateName;
            public System.IntPtr reserved;
            public int reserved2;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName dialog);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSaveFileName(ref OpenFileName dialog);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern System.IntPtr SHBrowseForFolder(ref BrowseInfo browseInfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SHGetPathFromIDList(System.IntPtr itemIdList, StringBuilder path);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(System.IntPtr pointer);

        public static string Open(string title, string extension)
        {
            OpenFileName dialog = Create(title, string.Empty, extension);
            dialog.flags |= OfnFileMustExist;
            string path = GetOpenFileName(ref dialog) ? dialog.file.ToString() : string.Empty;
            RememberDirectory(path);
            return path;
        }

        public static string Save(string title, string defaultFileName, string extension)
        {
            OpenFileName dialog = Create(title, EnsureExtension(defaultFileName, extension), extension);
            dialog.flags |= OfnOverwritePrompt;
            string path = GetSaveFileName(ref dialog) ? dialog.file.ToString() : string.Empty;
            RememberDirectory(path);
            return path;
        }

        public static string SelectFolder(string title, string defaultFolderName)
        {
            System.IntPtr displayName = Marshal.AllocHGlobal(512 * sizeof(char));
            System.IntPtr itemIdList = System.IntPtr.Zero;

            try
            {
                BrowseInfo browseInfo = new BrowseInfo
                {
                    displayName = displayName,
                    title = title,
                    flags = BifReturnOnlyFsDirs | BifNewDialogStyle
                };

                itemIdList = SHBrowseForFolder(ref browseInfo);
                if (itemIdList == System.IntPtr.Zero)
                {
                    return string.Empty;
                }

                StringBuilder path = new StringBuilder(MaxPath);
                if (!SHGetPathFromIDList(itemIdList, path))
                {
                    return string.Empty;
                }

                string selectedFolder = path.ToString();
                if (!string.IsNullOrWhiteSpace(defaultFolderName))
                {
                    selectedFolder = Path.Combine(selectedFolder, defaultFolderName);
                }

                RememberDirectory(selectedFolder);
                return selectedFolder;
            }
            finally
            {
                if (itemIdList != System.IntPtr.Zero)
                {
                    CoTaskMemFree(itemIdList);
                }

                Marshal.FreeHGlobal(displayName);
            }
        }

        private static OpenFileName Create(string title, string defaultFileName, string extension)
        {
            string normalizedExtension = extension.TrimStart('.');
            StringBuilder file = new StringBuilder(MaxPath);
            file.Append(defaultFileName);

            return new OpenFileName
            {
                structSize = Marshal.SizeOf(typeof(OpenFileName)),
                filter = normalizedExtension.ToUpperInvariant() + " files (*." + normalizedExtension + ")\0*." + normalizedExtension + "\0All files (*.*)\0*.*\0",
                filterIndex = 1,
                file = file,
                maxFile = MaxPath,
                fileTitle = new StringBuilder(256),
                maxFileTitle = 256,
                initialDirectory = GetInitialDirectory(),
                title = title,
                flags = OfnExplorer | OfnPathMustExist | OfnNoChangeDir,
                defaultExtension = normalizedExtension
            };
        }
    }
#endif
}
