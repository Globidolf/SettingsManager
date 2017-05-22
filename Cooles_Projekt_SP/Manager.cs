/********************************************
 * Author:			Silvan Pfister
 * Organisation:	Asperger-AG
 * Project:			Settings Manager
 * Version:			1.0
 * Creation-Date:	22.05.2017
 *
 *				Description:
 * Settings Manager Base Class. Allocates, structurizes and saves settings.
 ********************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Settings
{

	#region Exceptions

	/// <summary>
	/// represents a failed validation of a settings instance
	/// </summary>
	public class ValidationFailedException : Exception
	{
		private Setting cause;
		/// <summary>
		/// The Exception Message.
		/// </summary>
		public override string Message
		{
			get {
				return "Validation failed: " + (cause.__Data == null ? "[NULL]" : cause.__Data.ToString()) + " does not match the requirement: " + cause.ValidationDescription;
			}
		}

		internal ValidationFailedException(Setting causedBy) { cause = causedBy; }
	}

	/// <summary>
	/// Occurs when the filename of the settings-file is set to an invalid value.
	/// </summary>
	public class InvalidFileNameException : Exception
	{
		public string Filename { get; }
		public InvalidFileNameException(string FileName) { Filename = Filename; }
		public override string Message
		{
			get {
				return Filename == null || Filename == "" ?
					"The string for a filename must not be null or empty." :
					"The string '" + Filename + "is not valid for a filename. Prevent the usage of the following characters: \n\\, /, ?, *, <, >, |";
			}
		}
	}

	#endregion

	/// <summary>
	/// Base Class for Settings managers. Provides save-, load- and data-allocation methods to ease the handling of the individual settings.
	/// </summary>
	public abstract class BaseManager
	{
		private readonly EventHandler saveaction;

		/// <summary>
		/// Initializes the Manager. Keep a reference to change settings if you cannot bind them.
		/// </summary>
		public BaseManager() { saveaction = (obj, e) => save(); init(); }

		private bool _SaveOnExit;
		/// <summary>
		/// If set to true, binds the <see cref="save"/> method to the <see cref="AppDomain.CurrentDomain"/>.ProcessExit event.
		/// When set to false, it will unbind it again.
		/// </summary>
		public bool SaveOnExit
		{
			get { return _SaveOnExit; }
			set {
				if (_SaveOnExit != value) {
					_SaveOnExit = value;
					if (value)
						AppDomain.CurrentDomain.ProcessExit += saveaction;
					else
						AppDomain.CurrentDomain.ProcessExit -= saveaction;
				}
			}
		}


		/// <summary>
		/// Chacks a single character wether it is valid for use in a Filename
		/// </summary>
		private Func<char, bool> isNonFileChar = c => {
			switch (c) {
				case '\\':
				case '/':
				case ':':
				case '*':
				case '?':
				case '<':
				case '>':
				case '|':
					return true;
				default:
					return false;
			}
		};

		/// <summary>
		/// 'Setting'-files will be saved with this suffix.
		/// </summary>
		public const string FileType = ".cfg";

		/// <summary>
		/// data storage of the FileName property.
		/// </summary>
		private string _FileName = "main";

		/// <summary>
		/// The name of the file holding the settings-data.<br/>
		/// Can be altered but the suffix won't change.<br/>
		/// Throws an <see cref="InvalidFileNameException"/> if the new name is not valid for a file.
		/// </summary>
		public string FileName { get { return _FileName + FileType; } set {
				//Validates the filename
				if (value != null && value != "" && !value.Any(isNonFileChar)) {
					_FileName = value.TrimEnd(FileType.ToArray());
				} else throw new InvalidFileNameException(value);
			} }

		/// <summary>
		/// Contains the settings defined in <see cref="initData"/>.
		/// </summary>
		public Dictionary<string, Setting> Settings = new Dictionary<string, Setting>();
		
		/// <summary>
		/// Override this method to generate your settings-Dictionary. It will be used by the <see cref="init()"/> Method to generate a settings file.<br/>
		/// The Key of the dictionary is the name of the setting, the Value is the setting-type.
		/// See <see cref="allocateData(Dictionary{string, Setting})"/> for more information.
		/// </summary>
		/// <returns>The dictionary the library will use to manage the settings.</returns>
		internal protected abstract Dictionary<string, Setting> initData();

		/// <summary>
		/// Override this method to allocate the data loaded from the data file. <para/>
		/// To control what data is available here you need to override the <see cref="initData"/> method.
		/// </summary>
		/// <param name="settings">The dictionary holding all settings-data.</param>
		internal protected abstract void allocateData(Dictionary<string, Setting> settings);

		/// <summary>
		/// Call this method upon starting your application.<br/>
		/// It will generate the settings-dictionary and load the data from the settings file or create it if it doesn't exist yet.
		/// </summary>
		private void init() {
				Settings = initData();
				if (File.Exists(FileName)) {
					//loads all available settings from the data-file
					loadData();
				} else {
					File.Create(FileName).Close();
					save();
				}
				allocateData(Settings);
		}

		/// <summary>
		/// Attempts to create or load the settings file.
		/// </summary>
		private void loadData() {
			
			FileStream fs = File.OpenRead(FileName);
			StreamReader sr = new StreamReader(fs);
			while (!sr.EndOfStream) {
				string[] setting = sr.ReadLine().Split(new string[] { ": " }, 2, StringSplitOptions.None);
				if (setting.Length != 2 || !Settings.ContainsKey(setting[0]))
					continue;
				byte[] data = setting[1].Select(c => (byte)c).ToArray();
				Settings[setting[0]].LoadData(data);
			}
			sr.Close();
			sr.Dispose();
			fs.Dispose();
		}

		/// <summary>
		/// Saves the current <see cref="Settings"/> Dictionary to a file.
		/// </summary>
		public void save() {
			List<string> data = new List<string>();
			foreach (KeyValuePair<string, Setting> stng in Settings) {
				data.Add(stng.Key + ": " + new string(stng.Value.SaveData().Select(b => (char) b).ToArray()));
			}
			File.WriteAllLines(FileName, data);
		}
	}
}
