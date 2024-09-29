using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Dockery.Playnite.AutomaticDriveDetection {
    public class Program : GenericPlugin {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("bb3f1f23-6dc6-4136-bd7e-1a2cd4f6e9a6");

        public Program(IPlayniteAPI api) : base(api) {
            settings = new SettingsViewModel(this);
            Properties = new GenericPluginProperties {
                HasSettings = true
            };
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args) {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args) {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args) {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args) {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args) {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args) {
            // Add code to be executed when Playnite is initialized.

			if (string.IsNullOrWhiteSpace(settings.Settings.VolumeLabelText)) {
                return;
            }

            string LocatedDriveName = string.Empty;

			DriveInfo[] LocalMachineLogicalDrives = DriveInfo.GetDrives();

            for (int i = 0; i < LocalMachineLogicalDrives.Length; i++) {
                bool Located = false;

                if (settings.Settings.VolumeLabelTextEntireString) {
                    if (settings.Settings.VolumeLabelTextCaseSensitive) {
                        if (LocalMachineLogicalDrives[i].VolumeLabel.Equals(settings.Settings.VolumeLabelText)) {
							Located = true;
                        }
                    } else {
						if (LocalMachineLogicalDrives[i].VolumeLabel.ToLowerInvariant().Equals(settings.Settings.VolumeLabelText.ToLowerInvariant())) {
							Located = true;
						}
					}
                } else {
					if (settings.Settings.VolumeLabelTextCaseSensitive) {
                        if (LocalMachineLogicalDrives[i].VolumeLabel.Contains(settings.Settings.VolumeLabelText)) {
							Located = true;
                        }
					} else {
						if (LocalMachineLogicalDrives[i].VolumeLabel.ToLowerInvariant().Contains(settings.Settings.VolumeLabelText.ToLowerInvariant())) {
							Located = true;
						}
					}
				}

                if (Located == true) {
                    if (settings.Settings.DriveSelectionBehavior < 2) {
						LocatedDriveName = LocalMachineLogicalDrives[i].Name;

                        if (settings.Settings.DriveSelectionBehavior == 0) {
                            break;
                        }
                    } else /*if (settings.Settings.DriveSelectionBehavior >= 2)*/ {
                        if (string.IsNullOrWhiteSpace(LocatedDriveName)) {
							LocatedDriveName = LocalMachineLogicalDrives[i].Name;
						} else {
                            if (String.Compare(ParseDriveName(LocatedDriveName).Item1, ParseDriveName(LocalMachineLogicalDrives[i].Name).Item1) == 1) {
								if (settings.Settings.DriveSelectionBehavior == 2) {
									LocatedDriveName = LocalMachineLogicalDrives[i].Name;
								}
							} else {
								if (settings.Settings.DriveSelectionBehavior == 3) {
									LocatedDriveName = LocalMachineLogicalDrives[i].Name;
								}
							}
                        }
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(LocatedDriveName)) {
                return;
            }

            ICollection<Game> GamesList = PlayniteApi.Database.Games;

            for (int i = 0; i < GamesList.Count; i++) {
                if (GamesList.ElementAt(i).InstallDirectory.ToLowerInvariant().Contains("{PlayniteDir}".ToLowerInvariant())) {
                    continue;
                }

                Tuple<string, int> ExistingPathDrive = this.ParseDriveName(GamesList.ElementAt(i).InstallDirectory);

                if (string.IsNullOrWhiteSpace(ExistingPathDrive.Item1) || ExistingPathDrive.Item2 == -1) {
                    continue;
                }

				string NewPath = string.Empty;

                if (settings.Settings.PathReplacementBehavior == 2) {
					NewPath = this.GenerateNewPath(LocatedDriveName, GamesList.ElementAt(i).InstallDirectory, ExistingPathDrive);
				} else if (!Directory.Exists(GamesList.ElementAt(i).InstallDirectory)) {
                    if (settings.Settings.PathReplacementBehavior == 1) {
                        NewPath = this.GenerateNewPath(LocatedDriveName, GamesList.ElementAt(i).InstallDirectory, ExistingPathDrive);
                    } else if (settings.Settings.PathReplacementBehavior == 0) {
						for (int o = 0; o < LocalMachineLogicalDrives.Length; o++) {
							if (ExistingPathDrive.Item1.ToLowerInvariant() == this.ParseDriveName(LocalMachineLogicalDrives[o].Name).Item1.ToLowerInvariant()) {
								goto SkipNewPath;
                            }
                        }

                        NewPath = this.GenerateNewPath(LocatedDriveName, GamesList.ElementAt(i).InstallDirectory, ExistingPathDrive);
                        goto ExitLoop;

					    SkipNewPath:/*;*/
                        continue;
                    }
                }

                ExitLoop:

                if (string.IsNullOrWhiteSpace(NewPath)) {
                    continue;
                }

                if (settings.Settings.OnlyChangeIfNewPathExists == true && !Directory.Exists(NewPath)) {
                    continue;
                }

                GamesList.ElementAt(i).InstallDirectory = NewPath;
                PlayniteApi.Database.Games.Update(GamesList.ElementAt(i));
			}
		}

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args) {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args) {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings) {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings) {
            return new SettingsView();
		}

		private string GenerateNewPath(string locatedDriveName, string existingPath, Tuple<string, int> existingPathDrive) {
			if (string.IsNullOrWhiteSpace(locatedDriveName) || string.IsNullOrWhiteSpace(existingPath) || existingPathDrive == null || string.IsNullOrWhiteSpace(existingPathDrive.Item1) || existingPathDrive.Item2 == -1) {
				return null;
			}

			//Will have already been checked by nature of the existingPathDrive being passed, as long as ParseDriveName was called on said existingPath.
			/*if (existingPath[existingPathDrive.Item2 + 1] != '\\' && existingPath[existingPathDrive.Item2 + 1] != '/') {
                return null;
            }*/

			return new StringBuilder(locatedDriveName).Append(existingPath.Substring(existingPathDrive.Item2 + 2)).ToString();
		}

		private Tuple<string, int> ParseDriveName(string driveName) {
			if (string.IsNullOrWhiteSpace(driveName)) {
				return null;
			}

			List<int> ColonIndices = new List<int>();

			for (int o = 0; o < driveName.Length; o++) {
				if (driveName[o] == ':') {
					ColonIndices.Add(o);
				}
			}

			if (ColonIndices.Count != 1) {
				return null;
			}

			if ((driveName.Length - 1) <= ColonIndices[0] || ColonIndices[0] == 0) {
				return null;
			}

			if (driveName[ColonIndices[0] + 1] != '\\' && driveName[ColonIndices[0] + 1] != '/') {
				return null;
			}

			if (this.IsAllowedCharacter(driveName[ColonIndices[0] - 1]) == false) {
				return null;
			}

			return new Tuple<string, int>(driveName.Substring(0, ColonIndices[0]), ColonIndices[0]);
		}

		private bool IsAllowedCharacter(char character) {
            char[] AllowedCharacters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

            for (int i = 0; i < AllowedCharacters.Length; i++) {
                if (character == AllowedCharacters[i]) {
                    return true;
                }
            }

            return false;
        }
    }
}