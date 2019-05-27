using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Arise.FileSyncer.AndroidApp.Helpers;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace Arise.FileSyncer.AndroidApp.Activities
{
    public abstract class ProfileEditorActivity : AppCompatActivity
    {
        private const int DirectorySelectRC = 42;

        protected abstract void OnEditDone();

        protected TextInputEditText editName;
        protected TextInputEditText editDirectory;
        protected CheckBox cbAllowSend;
        protected CheckBox cbAllowReceive;
        protected CheckBox cbAllowDelete;
        protected CheckBox cbSkipHidden;
        protected Uri selectedUri;

        private TextInputLayout editNameLayout;
        private TextInputLayout editDirectoryLayout;
        private bool dirClicked;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load layout
            SetContentView(Resource.Layout.activity_profile_editor);

            // Reset values
            dirClicked = false;
            selectedUri = null;

            // Load saved state
            if (savedInstanceState != null)
            {
                dirClicked = savedInstanceState.GetBoolean("dirClicked", dirClicked);
                selectedUri = savedInstanceState.GetParcelable("selectedUri") as Android.Net.Uri;
            }

            // Toolbar setup
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowTitleEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);

            // Name
            editNameLayout = FindViewById<TextInputLayout>(Resource.Id.edit_name_layout);
            editName = FindViewById<TextInputEditText>(Resource.Id.edit_name);
            editName.AfterTextChanged += EditName_AfterTextChanged;

            // Directory
            editDirectoryLayout = FindViewById<TextInputLayout>(Resource.Id.edit_directory_layout);
            editDirectory = FindViewById<TextInputEditText>(Resource.Id.edit_directory);
            editDirectory.KeyListener = null;
            editDirectory.AfterTextChanged += EditDirectory_AfterTextChanged;
            editDirectory.Click += (sender, e) => { SelectDirectry(); };
            editDirectory.FocusChange += (sender, e) =>
            {
                if (e.HasFocus && !dirClicked) SelectDirectry();
                dirClicked = e.HasFocus;
            };

            // Checkboxes
            cbAllowSend = FindViewById<CheckBox>(Resource.Id.cb_allow_send);
            cbAllowReceive = FindViewById<CheckBox>(Resource.Id.cb_allow_receive);
            cbAllowDelete = FindViewById<CheckBox>(Resource.Id.cb_allow_delete);
            cbSkipHidden = FindViewById<CheckBox>(Resource.Id.cb_skip_hidden);

            // Checkboxes setup
            cbAllowSend.CheckedChange += CbAllowSend_CheckedChange;
            cbAllowReceive.CheckedChange += CbAllowReceive_CheckedChange;
            cbAllowDelete.Enabled = false;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean("dirClicked", dirClicked);
            outState.PutParcelable("selectedUri", selectedUri);

            base.OnSaveInstanceState(outState);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.profile_editor_menu, menu);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    OnBackPressed();
                    return true;

                case Resource.Id.profile_done:
                    if (CheckValues()) OnEditDone();
                    return true;

                default: return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == DirectorySelectRC)
            {
                if (resultCode == Result.Ok)
                {
                    selectedUri = data.Data;
                    string dirPath = FileUtility.GetFullPathFromTreeUri(data.Data);

                    if (!string.IsNullOrWhiteSpace(dirPath) && !dirPath.EndsWith(Path.DirectorySeparatorChar))
                    {
                        dirPath += Path.DirectorySeparatorChar;
                    }

                    editDirectory.Text = dirPath;
                }
            }
            else base.OnActivityResult(requestCode, resultCode, data);
        }

        protected virtual void OnError(int resId)
        {
            string error = Resources.GetString(resId);
            Android.Util.Log.Error(Constants.TAG, $"{this}: {error}");
            var view = FindViewById(Resource.Id.root_view);
            Snackbar.Make(view, error, Snackbar.LengthLong).Show();
        }

        private void EditName_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            if (editName.Text.Length > 0) editNameLayout.ErrorEnabled = false;
        }

        private void EditDirectory_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs e)
        {
            if (editDirectory.Text.Length > 0) editDirectoryLayout.ErrorEnabled = false;
        }

        private void CbAllowSend_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            cbAllowSend.Error = null;
            cbAllowReceive.Error = null;
        }

        private void CbAllowReceive_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            cbAllowDelete.Enabled = e.IsChecked;

            cbAllowSend.Error = null;
            cbAllowReceive.Error = null;
        }

        private void SelectDirectry()
        {
            dirClicked = true;

            Intent directorySelectIntent = new Intent(Intent.ActionOpenDocumentTree);
            directorySelectIntent.SetFlags(
                ActivityFlags.GrantReadUriPermission |
                ActivityFlags.GrantWriteUriPermission |
                ActivityFlags.GrantPersistableUriPermission |
                ActivityFlags.GrantPrefixUriPermission);

            if (selectedUri != null)
            {
                directorySelectIntent.PutExtra(DocumentsContract.ExtraInitialUri, selectedUri);
            }

            StartActivityForResult(directorySelectIntent, DirectorySelectRC);
        }

        private bool CheckValues()
        {
            bool success = true;

            if (string.IsNullOrWhiteSpace(editName.Text))
            {
                string error = Resources.GetString(Resource.String.error_pnew_name_empty);
                editNameLayout.ErrorEnabled = true;
                editNameLayout.ErrorFormatted = new Java.Lang.String(error);
                success = false;
            }

            if (string.IsNullOrWhiteSpace(editDirectory.Text))
            {
                string error = Resources.GetString(Resource.String.error_pnew_directory_empty);
                editDirectoryLayout.ErrorEnabled = true;
                editDirectoryLayout.ErrorFormatted = new Java.Lang.String(error);
                success = false;
            }
            else if (!Directory.Exists(editDirectory.Text))
            {
                string error = Resources.GetString(Resource.String.error_pnew_directory_not_exist);
                editDirectoryLayout.ErrorEnabled = true;
                editDirectoryLayout.ErrorFormatted = new Java.Lang.String(error);
                success = false;
            }

            if (!(cbAllowSend.Checked || cbAllowReceive.Checked))
            {
                string error = Resources.GetString(Resource.String.error_pnew_no_sync_type);
                cbAllowSend.Error = error;
                cbAllowReceive.Error = error;
                success = false;
            }

            return success;
        }
    }
}
