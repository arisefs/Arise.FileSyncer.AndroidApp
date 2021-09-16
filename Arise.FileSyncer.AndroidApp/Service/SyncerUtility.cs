using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using AndroidX.DocumentFile.Provider;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.Core.FileSync;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal static class SyncerUtility
    {
        private const string LogName = "SyncerUtility";

        public static bool FileCreate(string rootPath, string relativePath)
        {
            try
            {
                return FileUtility.GetDocumentFile(rootPath, relativePath, false, true) != null;
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when creating file. {ex.Message}");
                return false;
            }
        }

        public static bool FileDelete(string rootPath, string relativePath)
        {
            try
            {
                DocumentFile file = FileUtility.GetDocumentFile(rootPath, relativePath, false, false);
                if (file == null) return true;
                return file.Delete();
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when deleting file. {ex.Message}");
                return false;
            }
        }

        public static bool FileRename(string rootPath, string relativePath, string targetName)
        {
            try
            {
                DocumentFile file = FileUtility.GetDocumentFile(rootPath, relativePath, false, false);
                if (file == null) return false;
                return file.RenameTo(targetName);
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when renaming file. {ex.Message}");
                return false;
            }
        }

        public static Stream FileCreateWriteStream(string rootPath, string relativePath, FileMode fileMode)
        {
            try
            {
                string mode;
                bool create;

                switch (fileMode)
                {
                    case FileMode.CreateNew:
                        create = false;
                        mode = "w";
                        break;
                    case FileMode.Create:
                        create = true;
                        mode = "w";
                        break;
                    case FileMode.Open:
                        create = false;
                        mode = "rw";
                        break;
                    case FileMode.OpenOrCreate:
                        create = true;
                        mode = "rw";
                        break;
                    case FileMode.Truncate:
                        create = false;
                        mode = "rwt";
                        break;
                    case FileMode.Append:
                        create = true;
                        mode = "wa";
                        break;
                    default: throw new Exception("Invalid FileMode");
                }

                DocumentFile file = FileUtility.GetDocumentFile(rootPath, relativePath, false, create);
                if (file == null) return null;
                return Application.Context.ContentResolver.OpenOutputStream(file.Uri, mode);
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when opening file for write. {ex.Message}");
                return null;
            }
        }

        public static Stream FileCreateReadStream(string rootPath, string relativePath)
        {
            try
            {
                DocumentFile file = FileUtility.GetDocumentFile(rootPath, relativePath, false, false);
                if (file == null) return null;
                return Application.Context.ContentResolver.OpenInputStream(file.Uri);
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when opening file for read. {ex.Message}");
                return null;
            }
        }

        public static bool DirectoryCreate(string rootPath, string relativePath)
        {
            try
            {
                return FileUtility.GetDocumentFile(rootPath, relativePath, true, true) != null;
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when creating directory. {ex.Message}");
                return false;
            }
        }

        public static bool DirectoryDelete(string rootPath, string relativePath)
        {
            try
            {
                DocumentFile dir = FileUtility.GetDocumentFile(rootPath, relativePath, true, false);
                if (dir == null) return true;
                return dir.Delete();
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogName}: exception when deleting directory. {ex.Message}");
                return false;
            }
        }

        public static FileSystemItem[] GenerateTreeAndroid(string rootUri, bool skipHidden)
        {
            DocumentFile rootTree;
            try
            {
                rootTree = DocumentFile.FromTreeUri(Application.Context, Uri.Parse(rootUri));
            }
            catch (Exception ex)
            {
                Log.Warning($"{nameof(GenerateTreeAndroid)}: Failed to get root document tree: {ex}");
                return null;
            }

            List<FileSystemItem> fsItems = new();

            try
            {
                GetDocumentInfoRecursive(rootTree, "", skipHidden, ref fsItems);
            }
            catch (Exception ex)
            {
                Log.Warning($"{nameof(GenerateTreeAndroid)}: Failed to list filesystem items: {ex}");
                return null;
            }

            return fsItems.ToArray();
        }

        private static void GetDocumentInfoRecursive(DocumentFile root, string relativePath, bool skipHidden, ref List<FileSystemItem> fsItems)
        {
            foreach (DocumentFile document in root.ListFiles())
            {
                if (document.Name.EndsWith(".synctmp")) continue;
                if (skipHidden && document.Name.StartsWith('.')) continue;

                string docRelativePath = Path.Combine(relativePath, document.Name);
                DateTime docLastModified = TimeStampToDateTime(document.LastModified());

                if (document.IsDirectory)
                {
                    fsItems.Add(new FileSystemItem(true, docRelativePath, 0, docLastModified));
                    GetDocumentInfoRecursive(document, docRelativePath, skipHidden, ref fsItems);
                }
                else if (document.IsFile)
                {
                    fsItems.Add(new FileSystemItem(false, docRelativePath, document.Length(), docLastModified));
                }
            }
        }

        private static DateTime TimeStampToDateTime(long timeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timeStamp);
            return dateTime.ToLocalTime();
        }
    }
}
