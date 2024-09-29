using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace Dockery.Playnite.AutomaticDriveDetection {
    public class Settings : ObservableObject {
		public string VolumeLabelText { get => volumeLabelText; set => SetValue(ref volumeLabelText, value); }
		private string volumeLabelText = string.Empty;

		public bool VolumeLabelTextCaseSensitive { get => volumeLabelTextCaseSensitive; set => SetValue(ref volumeLabelTextCaseSensitive, value); }
		private bool volumeLabelTextCaseSensitive = true;

		public bool VolumeLabelTextEntireString { get => volumeLabelTextEntireString; set => SetValue(ref volumeLabelTextEntireString, value); }
		private bool volumeLabelTextEntireString = true;

		public int DriveSelectionBehavior { get => driveSelectionBehavior; set => SetValue(ref driveSelectionBehavior, value); }
		private int driveSelectionBehavior = 0;

		public int PathReplacementBehavior { get => pathReplacementBehavior; set => SetValue(ref pathReplacementBehavior, value); }
        private int pathReplacementBehavior = 0;

		public bool OnlyChangeIfNewPathExists { get => onlyChangeIfNewPathExists; set => SetValue(ref onlyChangeIfNewPathExists, value); }
		private bool onlyChangeIfNewPathExists = true;


		// Playnite serializes settings object to a JSON object and saves it as text file.
		// If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
		/*
		private bool optionThatWontBeSaved = false;
        
		[DontSerialize]
		public bool OptionThatWontBeSaved { get => optionThatWontBeSaved; set => SetValue(ref optionThatWontBeSaved, value); }
        */
	}

	public class SettingsViewModel : ObservableObject, ISettings {
        private readonly Program plugin;
        private Settings editingClone { get; set; }

        private Settings settings;
        public Settings Settings {
            get => settings;
            set {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SettingsViewModel(Program plugin) {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<Settings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null) {
                Settings = savedSettings;
            } else {
                Settings = new Settings();
            }
        }

        public void BeginEdit() {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit() {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit() {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors) {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}