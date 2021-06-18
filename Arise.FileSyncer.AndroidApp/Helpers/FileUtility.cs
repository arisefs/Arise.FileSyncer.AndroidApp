using System;
using Android.Content;
using Android.OS;
using Android.OS.Storage;
using Android.Provider;
using Android.Webkit;
using AndroidX.DocumentFile.Provider;
using Java.IO;
using Array = Java.Lang.Reflect.Array;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Helpers
{
    /// <summary>
    /// https://stackoverflow.com/questions/34927748/android-5-0-documentfile-from-tree-uri
    /// </summary>
    internal static class FileUtility
    {
        private const string PRIMARY_VOLUME_NAME = "primary";

        public static DocumentFile GetDocumentFile(Guid profileId, string relativePath, bool isDirectory, bool createIfNonExistent)
        {
            Uri treeUri = AppPrefs.GetUri(MainApplication.AppContext, profileId.ToString());
            if (treeUri == null) return null;

            // start with root of SD card and then parse through document tree.
            DocumentFile document = DocumentFile.FromTreeUri(MainApplication.AppContext, treeUri);

            string[] parts = relativePath.Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                DocumentFile nextDocument = document.FindFile(parts[i]);

                if (nextDocument == null)
                {
                    if (createIfNonExistent)
                    {
                        if ((i < parts.Length - 1) || isDirectory)
                        {
                            nextDocument = document.CreateDirectory(parts[i]);
                        }
                        else
                        {
                            string mimeType = GetMimeType(parts[i]);
                            nextDocument = document.CreateFile(mimeType, parts[i]);
                        }
                    }
                    else
                    {
                        document = null;
                        break;
                    }
                }

                document = nextDocument;
            }

            return document;
        }

        private static string GetMimeType(string url)
        {
            string mimeType = null;
            string extension = MimeTypeMap.GetFileExtensionFromUrl(url);

            if (extension != null)
            {
                mimeType = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension.ToLower());
            }

            return mimeType ?? "*/*";
        }

        public static string GetFullPathFromTreeUri(Uri treeUri)
        {
            if (treeUri == null) return null;

            string volumePath = GetVolumePath(GetVolumeIdFromTreeUri(treeUri), MainApplication.AppContext);
            if (volumePath == null) return File.Separator;
            if (volumePath.EndsWith(File.Separator, StringComparison.Ordinal))
            {
                volumePath = volumePath.Substring(0, volumePath.Length - 1);
            }

            string documentPath = GetDocumentPathFromTreeUri(treeUri);
            if (documentPath.EndsWith(File.Separator, StringComparison.Ordinal))
            {
                documentPath = documentPath.Substring(0, documentPath.Length - 1);
            }

            if (documentPath.Length > 0)
            {
                if (documentPath.StartsWith(File.Separator, StringComparison.Ordinal))
                {
                    return volumePath + documentPath;
                }
                else
                {
                    return volumePath + File.Separator + documentPath;
                }
            }
            else return volumePath;
        }

        private static string GetVolumePath(string volumeId, Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop) return null;

            try
            {
                StorageManager mStorageManager = (StorageManager)context.GetSystemService(Context.StorageService);
                var storageVolumeClass = Java.Lang.Class.ForName("android.os.storage.StorageVolume");
                var getVolumeList = mStorageManager.Class.GetMethod("getVolumeList");
                var getUuid = storageVolumeClass.GetMethod("getUuid");
                var getPath = storageVolumeClass.GetMethod("getPath");
                var isPrimary = storageVolumeClass.GetMethod("isPrimary");
                var result = getVolumeList.Invoke(mStorageManager);

                int length = Array.GetLength(result);
                for (int i = 0; i < length; i++)
                {
                    var storageVolumeElement = Array.Get(result, i);
                    var uuid = (Java.Lang.String)getUuid.Invoke(storageVolumeElement);
                    var primary = (Java.Lang.Boolean)isPrimary.Invoke(storageVolumeElement);

                    // primary volume?
                    if (primary.Equals(Java.Lang.Boolean.True) && PRIMARY_VOLUME_NAME.Equals(volumeId, StringComparison.Ordinal))
                    {
                        return ((Java.Lang.String)getPath.Invoke(storageVolumeElement)).ToString();
                    }

                    // other volumes?
                    if (uuid != null && uuid.ToString().Equals(volumeId, StringComparison.Ordinal))
                    {
                        return ((Java.Lang.String)getPath.Invoke(storageVolumeElement)).ToString();
                    }
                }

                // not found.
                return null;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Debug(Constants.TAG, $"FileUtility: GetVolumePath exception: " + ex.Message);
                return null;
            }
        }

        private static string GetVolumeIdFromTreeUri(Uri treeUri)
        {
            string docId = DocumentsContract.GetTreeDocumentId(treeUri);
            string[] split = docId.Split(':');
            if (split.Length > 0) return split[0];
            else return null;
        }

        private static string GetDocumentPathFromTreeUri(Uri treeUri)
        {
            string docId = DocumentsContract.GetTreeDocumentId(treeUri);
            string[] split = docId.Split(':');
            if ((split.Length >= 2) && (!string.IsNullOrEmpty(split[1]))) return split[1];
            else return File.Separator;
        }
    }
}
