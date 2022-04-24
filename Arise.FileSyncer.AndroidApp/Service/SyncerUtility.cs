using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Provider;
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

            ParallelGetDocumentInfo gen = new(skipHidden);

            try
            {
                gen.Execute(rootTree);
            }
            catch (Exception ex)
            {
                Log.Warning($"{nameof(GenerateTreeAndroid)}: Failed to list filesystem items: {ex}");
                return null;
            }

            return gen.GetItems();
        }

        public static (long, DateTime, DateTime)? FileInfoAndroid(string rootPath, string relativePath)
        {
            try
            {
                DocumentFile file = FileUtility.GetDocumentFile(rootPath, relativePath, false, false);
                if (file != null && file.Exists())
                {
                    long size = file.Length();
                    DateTime lwt = TimeStampToDateTime(file.LastModified());
                    // Creation date is not available so it just uses last modified time as that
                    return (size, lwt, lwt);
                }
                else Log.Warning($"{nameof(FileInfoAndroid)}: file does not exists: {relativePath}");
            }
            catch (Exception ex)
            {
                Log.Warning($"{nameof(FileInfoAndroid)}: exception when retrieving file info. {ex.Message}");
            }

            return null;
        }

        class ParallelGetDocumentInfo
        {
            private readonly ConcurrentBag<FileSystemItem> fsItems = new();
            private readonly bool skipHidden;

            public ParallelGetDocumentInfo(bool skipHidden)
            {
                this.skipHidden = skipHidden;
            }

            public void Execute(DocumentFile rootTree)
            {
                GetDocumentInfoRecursive(rootTree, "");
            }

            public FileSystemItem[] GetItems()
            {
                return fsItems.ToArray();
            }

            private void GetDocumentInfoRecursive(DocumentFile root, string relativePath)
            {
                Parallel.ForEach(root.ListFiles(), document =>
                {
                    string docName = document.Name;

                    if (docName.EndsWith(".synctmp", StringComparison.Ordinal)) return;
                    if (skipHidden && docName.StartsWith('.')) return;

                    string docRelativePath = Path.Combine(relativePath, docName);

                    if (document.IsDirectory)
                    {
                        fsItems.Add(new FileSystemItem(true, docRelativePath, 0, new DateTime()));
                        GetDocumentInfoRecursive(document, docRelativePath);
                    }
                    else // if (document.IsFile) // Removed to increase performance
                    {
                        DateTime docLastModified = TimeStampToDateTime(document.LastModified());
                        fsItems.Add(new FileSystemItem(false, docRelativePath, document.Length(), docLastModified));
                    }
                });
            }
        }

        private static DateTime TimeStampToDateTime(long timeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(timeStamp);
            return dateTime.ToUniversalTime();
        }
    }
}
