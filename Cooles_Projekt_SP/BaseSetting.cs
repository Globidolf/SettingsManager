/********************************************
 * Author:			Silvan Pfister
 * Organisation:	Asperger-AG
 * Project:			Settings Manager
 * Version:			1.1
 * Creation-Date:	22.05.2017
 *
 *				Description:
 * Collection of Settings-Classes to serialize data dynamically
 ********************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Settings
{
	#region Base Classes
	/// <summary>
	/// Do not override this class. instead override the <see cref="BaseSetting{T}"/> class or the <see cref="StrSetting{T}"/> class.<para/>
	/// If you want to read the value, cast it to the Setting type you saved it as. Example:<para/>
	/// <see cref="Setting"/> cfg = ... ; <para/>
	/// <see cref="int"/> a = cfg as <see cref="IntSetting"/>;
	/// </summary>
	public abstract class Setting
	{
		/// <summary>
		/// Override to enter a text explaining what values are valid for this setting type, if not obvious.
		/// </summary>
		/// <returns> returns by default: value must not be null </returns>
		public virtual string ValidationDescription{ get { return "value must not be null!"; } }


		/// <summary>
		/// accessor for the basemanager class.
		/// </summary>
		internal protected abstract object __Data { get; set; }

		/// <summary>
		/// override to implement custom validation.
		/// base implementation checks null values.
		/// if false is returned an exception will be thrown.
		/// </summary>
		/// <returns>true if value is valid, false otherwise.</returns>
		/// <exception cref="ValidationFailedException">Throwed if the validation failed, obviously.</exception>
		internal protected virtual bool isValid() { return __Data != null; }

		#region character escapism

		private string[] escapeStrings = new [] { "\\", "\n", "\r" };
		private string[] replaceStrings = new [] { "\\\\", "\\n", "\\r" };
		internal byte[] SaveData() {
			if (!isValid()) throw new ValidationFailedException(this);
			string data = new string(Save().Select(b => (char)b).ToArray());
			for(int i = 0 ; i < escapeStrings.Length ; i++) {
				data = data.Replace(escapeStrings[i], replaceStrings[i]);
			}
			return data.Select(c => (byte) c).ToArray();
		}

		internal void LoadData(byte[] data) {
			string strData = new string(data.Select(b => (char)b).ToArray());
			for (int i = 0 ; i < escapeStrings.Length ; i++) {
				strData = strData.Replace(replaceStrings[i], escapeStrings[i]);
			}
			Load(strData.Select(c => (byte)c).ToArray());
			if (!isValid()) throw new ValidationFailedException(this);
		}

		#endregion

		/// <summary>
		/// Convert your object to a byte-array in this method.
		/// </summary>
		/// <returns>the byte array written to the settings file</returns>
		internal protected abstract byte[] Save();

		/// <summary>
		/// Restore your object from a byte-array in this method.
		/// </summary>
		/// <param name="data">the data defining your object</param>
		internal protected abstract void Load(byte[] data);

		/* Unused implicit operators. 
		public static implicit operator int(BaseSetting val) { return (Setting<int>) val; }
		public static implicit operator long(BaseSetting val) { return (Setting<long>) val; }
		public static implicit operator string(BaseSetting val) { return (Setting<string>) val; }
		public static implicit operator float(BaseSetting val) { return (Setting<float>) val; }
		public static implicit operator double(BaseSetting val) { return (Setting<double>) val; }
		public static implicit operator short(BaseSetting val) { return (Setting<short>) val; }
		public static implicit operator byte(BaseSetting val) { return (Setting<byte>) val; }
		public static implicit operator bool(BaseSetting val) { return (Setting<bool>) val; }
		public static implicit operator decimal(BaseSetting val) { return (Setting<decimal>) val; }
		public static implicit operator char(BaseSetting val) { return (Setting<char>) val; }
		public static implicit operator uint(BaseSetting val) { return (Setting<uint>) val; }
		public static implicit operator ushort(BaseSetting val) { return (Setting<ushort>) val; }
		public static implicit operator ulong(BaseSetting val) { return (Setting<ulong>) val; }
		*/
	}

	/// <summary>
	/// A base Settings class used to store and load your settings as byte arrays.
	/// </summary>
	/// <typeparam name="T">The type of the object you want to store.</typeparam>
	public abstract class BaseSetting<T> : Setting
	{
		internal T _val;
		/// <summary>
		/// The value of the current setting. when allocating, make sure that it passes the <see cref="Setting.isValid"/> validation.
		/// </summary>
		public T val
		{
			get {
				return _val;
			}
			set {
				_val = value;
				if (!isValid()) {
					Exception e = new ValidationFailedException(this);
					Console.WriteLine(e);
					throw e;
				}
			}
		}

		/// <summary>
		/// realisation of the accessor for the basemanager class.
		/// </summary>
		sealed internal protected override object __Data { get { return val; }  set { val = (T)value; } }
		
		/// <summary>
		/// For your convention this operator will allow you to assign instances of <see cref="T"/> directly to the <see cref="BaseSetting{T}"/> instance.
		/// </summary>
		/// <param name="setting">an instance of this type</param>
		public static implicit operator T(BaseSetting<T> setting) { return setting.val; }

		/// <summary>
		/// Enforced constructor. Sets the value to a sub-class specific value if not set.
		/// </summary>
		/// <param name="Default">The value to initialize the setting with.
		protected BaseSetting(T Default) { val = Default; }

	}

	/// <summary>
	/// Another variation of a base Settings class. This one provides methods to store and load settings as strings.
	/// </summary>
	/// <typeparam name="T">The type of the object you want to store.</typeparam>
	public abstract class StrSetting<T> : BaseSetting<T>
	{
		protected StrSetting(T Default) : base(Default) { }

		/// <summary>
		/// override the <see cref="LoadData(string)"/> method.
		/// </summary>
		/// <param name="data">in the <see cref="LoadData(string)"/> method this value is converted to a string</param>
		protected internal sealed override void Load(byte[] data) { LoadData(string.Concat(data.Select(b => (char) b))); }

		/// <summary>
		/// override the <see cref="SaveDataString"/> method.
		/// </summary>
		/// <returns>returns the byte-array representation of your result returned in <see cref="SaveDataString"/></returns>
		protected internal sealed override byte[] Save() { return SaveDataString().Select(c => (byte) c).ToArray(); }

		/// <summary>
		/// Restore your object from the string provided in this method
		/// </summary>
		/// <param name="data">The string defining your object</param>
		public abstract void LoadData(string data);

		/// <summary>
		/// Convert your object to a string to be saved to the settings file.
		/// </summary>
		/// <returns>a string representation of your object</returns>
		public abstract string SaveDataString();

		/// <summary>
		/// Same as <see cref="BaseSetting{T}.implicit operator T(BaseSetting{T})"/>
		/// </summary>
		public static implicit operator T(StrSetting<T> setting) { return setting.val; }
	}
	#endregion

	#region Basic implementations

	/// <summary>
	/// Setting to store and load a single <see cref="char"/>.
	/// </summary>
	public class CharSetting : BaseSetting<char>
	{
		public CharSetting(char Default = '\0') : base(Default) {}
		protected internal override void Load(byte[] data) { val = BitConverter.ToChar(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator CharSetting(StrSetting<char> stng) { return new CharSetting (stng.val); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="string"/>.
	/// </summary>
	public class StringSetting : BaseSetting<string>
	{
		public StringSetting(string Default = "") : base(Default) { }
		protected internal override void Load(byte[] data) { val = new string(data.Select(d => (char) d).ToArray()); }
		protected internal override byte[] Save() { return val.Select(c => (byte) c).ToArray(); }
		public static implicit operator StringSetting(StrSetting<string> stng) { return new StringSetting(stng.val); }
	}

	/// <summary>
	/// Setting to store and load a single <see cref="byte"/>.
	/// </summary>
	public class ByteSetting : BaseSetting<byte>
	{
		public ByteSetting(byte Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = data[0]; }
		protected internal override byte[] Save() { return new byte[] { val }; }
		public static implicit operator ByteSetting(StrSetting<byte> stng) { return new ByteSetting ( stng.val ); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="short"/>.
	/// </summary>
	public class ShortSetting : BaseSetting<short>
	{
		public ShortSetting(short Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToInt16(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator ShortSetting(StrSetting<short> stng) { return new ShortSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load an <see cref="ushort"/>.
	/// </summary>
	public class UShortSetting : BaseSetting<ushort>
	{
		public UShortSetting(ushort Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToUInt16(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator UShortSetting(StrSetting<ushort> stng) { return new UShortSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load an <see cref="int"/>.
	/// </summary>
	public class IntSetting : BaseSetting<int>
	{
		public IntSetting(int Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToInt32(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator IntSetting(StrSetting<int> stng) { return new IntSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load an <see cref="uint"/>.
	/// </summary>
	public class UIntSetting : BaseSetting<uint>
	{
		public UIntSetting(uint Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToUInt32(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator UIntSetting(StrSetting<uint> stng) { return new UIntSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="long"/>.
	/// </summary>
	public class LongSetting : BaseSetting<long>
	{
		public LongSetting(long Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToInt64(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator LongSetting(StrSetting<long> stng) { return new LongSetting(stng.val); }
	}

	/// <summary>
	/// Setting to store and load an <see cref="ulong"/>.
	/// </summary>
	public class ULongSetting : BaseSetting<ulong>
	{
		public ULongSetting(ulong Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToUInt64(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator ULongSetting(StrSetting<ulong> stng) { return new ULongSetting(stng.val); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="float"/>.
	/// </summary>
	public class FloatSetting : BaseSetting<float>
	{
		public FloatSetting(float Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToSingle(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator FloatSetting(StrSetting<float> stng) { return new FloatSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="double"/>.
	/// </summary>
	public class DoubleSetting : BaseSetting<double>
	{
		public DoubleSetting(double Default = 0) : base(Default) { }
		protected internal override void Load(byte[] data) { val = BitConverter.ToDouble(data, 0); }
		protected internal override byte[] Save() { return BitConverter.GetBytes(val); }
		public static implicit operator DoubleSetting(StrSetting<double> stng) { return new DoubleSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="decimal"/>.
	/// </summary>
	public class DecimalSetting : BaseSetting<decimal>
	{
		public DecimalSetting(decimal Default = 0) : base(Default) { }
		//Conversion method learned from https://social.technet.microsoft.com/wiki/contents/articles/19055.convert-system-decimal-to-and-from-byte-arrays-vb-c.aspx
		protected internal override void Load(byte[] data) {
			int[] bits = new int[] { BitConverter.ToInt32(data,0), BitConverter.ToInt32(data,4), BitConverter.ToInt32(data,8), BitConverter.ToInt32(data,12) };
			val = new decimal(bits);
		}
		protected internal override byte[] Save() {
			int[] bits = decimal.GetBits(val);
			List<byte> bytes = new List<byte>();
			bytes.AddRange(BitConverter.GetBytes(bits[0]));
			bytes.AddRange(BitConverter.GetBytes(bits[1]));
			bytes.AddRange(BitConverter.GetBytes(bits[2]));
			bytes.AddRange(BitConverter.GetBytes(bits[3]));
			return bytes.ToArray();
		}
		public static implicit operator DecimalSetting(StrSetting<decimal> stng) { return new DecimalSetting (stng.val ); }
	}

	/// <summary>
	/// Setting to store and load a <see cref="bool"/>.
	/// </summary>
	public class BoolSetting : BaseSetting<bool>
	{
		public BoolSetting(bool Default) : base(Default) { }
		protected internal override void Load(byte[] data) { val = data[0] != 0; }
		protected internal override byte[] Save() { return new[] { (byte)(val ? 255 : 0) }; }
		public static implicit operator BoolSetting(StrSetting<bool> stng) { return new BoolSetting(stng.val); }
	}

	/// <summary>
	/// Setting to store and load a normalized <see cref="float"/>.
	/// </summary>
	public class NFloatSetting : FloatSetting
	{
		/// <summary>
		/// Checks if the value is normalized.
		/// </summary>
		/// <returns></returns>
		protected internal override bool isValid() {
			if (!base.isValid())
				return false;
			return (0 <= val && val <= 1);
		}

		public override string ValidationDescription
		{
			get { return "value must be between 0 and 1 (inclusive)"; }
		}

		public NFloatSetting(float Default) : base(Default) { }
	}

	/// <summary>
	/// Setting to store and load a normalized <see cref="double"/>.
	/// </summary>
	public class NDoubleSetting : DoubleSetting
	{
		/// <summary>
		/// Checks if the value is normalized.
		/// </summary>
		/// <returns></returns>
		protected internal override bool isValid() {
			if (!base.isValid())
				return false;
			return (0 <= val && val <= 1);
		}

		public override string ValidationDescription
		{
			get { return "value must be between 0 and 1 (inclusive)"; }
		}

		public NDoubleSetting(double Default) : base(Default) { }
	}

	/// <summary>
	/// Setting to store and load a normalized <see cref="decimal"/>.
	/// </summary>
	public class NDecimalSetting : DecimalSetting
	{
		/// <summary>
		/// Checks if the value is normalized.
		/// </summary>
		/// <returns></returns>
		protected internal override bool isValid() {
			if (!base.isValid())
				return false;
			return (0 <= val && val <= 1);
		}

		public override string ValidationDescription
		{
			get { return "value must be between 0 and 1 (inclusive)"; }
		}

		public NDecimalSetting(decimal Default) : base(Default) { }
	}
	
	#endregion
}
