using Android.Content;
using Android.Net;

namespace Arise.FileSyncer.AndroidApp
{
    internal static class AppPrefs
    {
        public static void RemoveKey(Context context, string key)
        {
            var prefEditor = GetPreferences(context).Edit();
            prefEditor.Remove(key);
            prefEditor.Commit();
        }

        public static void SaveString(Context context, string key, string value)
        {
            var prefEditor = GetPreferences(context).Edit();
            prefEditor.PutString(key, value);
            prefEditor.Commit();
        }

        public static void SaveUri(Context context, string key, Uri value)
        {
            var prefEditor = GetPreferences(context).Edit();
            prefEditor.PutString(ToUriKey(key), value.ToString());
            prefEditor.Commit();
        }

        public static void SaveBoolean(Context context, string key, bool value)
        {
            var prefEditor = GetPreferences(context).Edit();
            prefEditor.PutBoolean(key, value);
            prefEditor.Commit();
        }

        public static string GetString(Context context, string key, string defValue = null)
        {
            var prefs = GetPreferences(context);
            return prefs.GetString(key, defValue);
        }

        public static Uri GetUri(Context context, string key, Uri defValue = null)
        {
            var prefs = GetPreferences(context);
            var value = prefs.GetString(ToUriKey(key), null);
            try { return Uri.Parse(value); }
            catch { return defValue; }
        }

        public static bool GetBoolean(Context context, string key, bool defValue)
        {
            var prefs = GetPreferences(context);
            return prefs.GetBoolean(key, defValue);
        }

        private static ISharedPreferences GetPreferences(Context context)
        {
            var packageName = context.PackageManager.GetPackageInfo(context.PackageName, 0).PackageName;
            return context.GetSharedPreferences(packageName, FileCreationMode.Private);
        }

        private static string ToUriKey(string key)
        {
            return $"uri:{key}";
        }
    }
}