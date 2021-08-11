using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using AndroidX.DocumentFile.Provider;
using Arise.FileSyncer.AndroidApp.Helpers;
using Arise.FileSyncer.Core;
using Arise.FileSyncer.Core.FileSync;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Service
{
    internal static class SyncerUtility
    {
        private const string LogName = "SyncerUtility";

        public static bool FileCreate(Guid profileId, string _, string relativePath)
        {
            try
            {
                if (FileUtility.GetDocumentFile(profileId, relativePath, false, true) == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when creating file. MSG: " + ex.Message);
                return false;
            }

            return true;
        }

        public static bool FileDelete(Guid profileId, string _, string relativePath)
        {
            try
            {
                var file = FileUtility.GetDocumentFile(profileId, relativePath, false, false);
                if (file == null) return true;
                return file.Delete();
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when deleting file. MSG: " + ex.Message);
                return false;
            }
        }

        public static bool FileRename(Guid profileId, string _, string relativePath, string targetName)
        {
            try
            {
                var file = FileUtility.GetDocumentFile(profileId, relativePath, false, false);
                if (file == null) return false;
                return file.RenameTo(targetName);
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when renaming file. MSG: " + ex.Message);
                return false;
            }
        }

        public static bool FileCreateWriteStream(Guid profileId, string _, string relativePath, out Stream fileStream, FileMode fileMode)
        {
            fileStream = null;

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

                var file = FileUtility.GetDocumentFile(profileId, relativePath, false, create);
                if (file == null) return false;
                fileStream = MainApplication.AppContext.ContentResolver.OpenOutputStream(file.Uri, mode);
                if (fileStream == null) return false;
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when opening file for write. MSG: " + ex.Message);
                return false;
            }

            return true;
        }

        public static bool FileCreateReadStream(Guid profileId, string _, string relativePath, out Stream fileStream)
        {
            fileStream = null;

            try
            {
                var file = FileUtility.GetDocumentFile(profileId, relativePath, false, false);
                if (file == null) return false;
                fileStream = MainApplication.AppContext.ContentResolver.OpenInputStream(file.Uri);
                if (fileStream == null) return false;
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when opening file for read. MSG: " + ex.Message);
                return false;
            }

            return true;
        }

        public static bool DirectoryCreate(Guid profileId, string _, string relativePath)
        {
            try
            {
                if (FileUtility.GetDocumentFile(profileId, relativePath, true, true) == null)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when creating directory. MSG: " + ex.Message);
                return false;
            }

            return true;
        }

        public static bool DirectoryDelete(Guid profileId, string _, string relativePath)
        {
            try
            {
                var dir = FileUtility.GetDocumentFile(profileId, relativePath, true, false);
                if (dir == null) return true;
                return dir.Delete();
            }
            catch (Exception ex)
            {
                Log.Warning(LogName + ": exception when deleting directory. MSG: " + ex.Message);
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
