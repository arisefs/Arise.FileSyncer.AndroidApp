using System;
using Android.Content;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Helpers
{
    internal static class UriHelper
    {
        /// <summary>
        /// Save URI and permissions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="uri"></param>
        /// <param name="path"></param>
        /// <param name="isReceive"></param>
        public static void SaveUriWithPermissions(Context context, Uri uri, Guid key, bool isReceive)
        {
            ActivityFlags flags = ActivityFlags.GrantReadUriPermission;
            if (isReceive) flags |= ActivityFlags.GrantWriteUriPermission;

            try { context.ContentResolver.TakePersistableUriPermission(uri, flags); }
            catch (Exception ex) { Android.Util.Log.Error(Constants.TAG, "SaveUriWithPermissions: " + ex.Message); }

            AppPrefs.SaveUri(context, key.ToString(), uri);
        }

        /// <summary>
        /// Removes a URI and permissions
        /// </summary>
        /// <param name="context"></param>
        /// <param name="path"></param>
        public static void RemoveUriWithPermissions(Context context, Guid key)
        {
            const ActivityFlags flags = ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission;
            var uri = AppPrefs.GetUri(context, key.ToString());

            if (uri != null)
            {
                try { context.ContentResolver.ReleasePersistableUriPermission(uri, flags); }
                catch (Exception ex) { Android.Util.Log.Error(Constants.TAG, "RemoveUriWithPermissions: " + ex.Message); }

                AppPrefs.RemoveKey(context, key.ToString());
            }
        }

        /// <summary>
        /// Checks if the uri has the permissions necessary
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <param name="isReceive"></param>
        /// <returns></returns>
        public static bool CheckUriPermissions(Context context, Guid key, bool isReceive)
        {
            var uri = AppPrefs.GetUri(context, key.ToString());

            if (uri != null)
            {
                foreach (var uriPermission in context.ContentResolver.PersistedUriPermissions)
                {
                    if (uriPermission.Uri.Path == uri.Path)
                    {
                        if (!uriPermission.IsReadPermission) return false;
                        if (isReceive && !uriPermission.IsWritePermission) return false;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
