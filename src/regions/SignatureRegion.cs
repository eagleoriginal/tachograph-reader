using System;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using DataFileReader;

namespace DataFileReader
{
	public class SignatureRegion : HexValueRegion
	{
		public static readonly AsyncLocal<long> signedDataOffsetBegin = new AsyncLocal<long>();
		public static readonly AsyncLocal<long> signedDataOffsetEnd  = new AsyncLocal<long>();
		public static readonly AsyncLocal<DateTimeOffset?> newestDateTime = new AsyncLocal<DateTimeOffset?>();

		public static void Reset()
		{
			SignatureRegion.signedDataOffsetBegin.Value = 0;
			SignatureRegion.signedDataOffsetEnd.Value = 0;
		}

		public static void UpdateTime(DateTime dateTime)
		{
			SignatureRegion.UpdateTime((DateTimeOffset)DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
		}

		public static void UpdateTime(DateTimeOffset dateTime)
		{

			if (SignatureRegion.newestDateTime == null ||
			    (dateTime > SignatureRegion.newestDateTime.Value &&
			     // need to compare to current time to filter out dateTime from future...
			     dateTime < DateTimeOffset.Now))
			{
				SignatureRegion.newestDateTime.Value = dateTime;
			}
		}

		public static int GetSignedDataLength()
		{
			return (int)(SignatureRegion.signedDataOffsetEnd.Value - SignatureRegion.signedDataOffsetBegin.Value);
		}

		protected override void ProcessInternal(CustomBinaryReader reader)
		{
			SignatureRegion.signedDataOffsetEnd.Value = reader.BaseStream.Position;

			base.ProcessInternal(reader);

			long currentOffset = reader.BaseStream.Position;

			reader.BaseStream.Position = SignatureRegion.signedDataOffsetBegin.Value;
			Validator.ValidateGen1(reader.ReadBytes(SignatureRegion.GetSignedDataLength()), this.ToBytes(), SignatureRegion.newestDateTime.Value);

			reader.BaseStream.Position = currentOffset;
		}
	}
}
