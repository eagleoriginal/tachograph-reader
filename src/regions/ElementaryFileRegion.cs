using System;
using System.Xml;
using System.Xml.Serialization;
using DataFileReader;

namespace DataFileReader
{
	public class ElementaryFileRegion : IdentifiedObjectRegion
    {
        public const byte GostSignature = 0x81;

		[XmlAttribute]
		public bool Unsigned = false;

		[XmlIgnore]
		public byte[] signature { get; private set; } = null;

		public bool IsSignature(CustomBinaryReader reader)
		{
			int type = reader.PeekByte();
			WriteLine(LogLevel.DEBUG, "- type: {0}", type);
			
            return type == 0x01 || type == GostSignature;
		}

		protected override bool SuppressElement(CustomBinaryReader reader)
		{
			return this.IsSignature(reader);
		}

		protected override void ProcessInternal(CustomBinaryReader reader)
		{
			// read the type
			byte type = reader.ReadByte();

			regionLength = reader.ReadSInt16();
			long fileLength = regionLength;

			long start = reader.BaseStream.Position;
			WriteLine(LogLevel.DEBUG, "- location: {0:X4}-{1:X4}/{2:X4}", start, start + regionLength, regionLength);
			if (DataFile.StrictProcessing && start + regionLength > reader.BaseStream.Length)
			{
				throw new InvalidOperationException(string.Format("{0}: Would try to read more than length of stream! Position 0x{1:X4} + RegionLength 0x{2:X4} > Length 0x{3:X4}", Name, start, regionLength, reader.BaseStream.Length));
			}

			if (type == 0x01)
			{
				// this is just the signature
				this.signature = reader.ReadBytes((int)fileLength);
				fileLength = 0;

				long currentOffset = reader.BaseStream.Position;

				reader.BaseStream.Position = SignatureRegion.signedDataOffsetBegin.Value;
				Validator.ValidateDelayedGen1(reader.ReadBytes(SignatureRegion.GetSignedDataLength()), this.signature, () => { return SignatureRegion.newestDateTime.Value; });

				reader.BaseStream.Position = currentOffset;
			}
			else if (type == 0)
			{
				base.ProcessInternal(reader);

				long amountProcessed = reader.BaseStream.Position - start;
				fileLength -= amountProcessed;

				if (this.Name == "CardCertificate")
				{
					Validator.SetCertificate(this);
				}
				else if (this.Name == "CACertificate")
				{
					Validator.SetCACertificate(this);
				};
			} else if (type == GostSignature)
            {
				// this is just the Gost signature. Skip it
				signature = reader.ReadBytes((int)fileLength);
                fileLength = 0;
            }
			else
			{
				// this is some unknown section and should be skipped
				WriteLine(LogLevel.INFO, "- skipping matched type {0:X2} for {1}", type, Name);
			}

			if (DataFile.StrictProcessing && fileLength != 0)
			{
				throw new InvalidOperationException(string.Format("{0}: Read wrong number of bytes! Position 0x{1:X4} and end of region 0x{2:X4} but bytes left 0x{3:X4}", Name, reader.BaseStream.Position, start + regionLength, fileLength));
			}

			if (DataFile.StrictProcessing && reader.BaseStream.Position > start + regionLength)
			{
				throw new InvalidOperationException(string.Format("{0}: Read past end of region! Position 0x{1:X4} > end of region 0x{2:X4}", Name, reader.BaseStream.Position, start + regionLength));
			}

			if (fileLength > 0)
			{
				WriteLine(LogLevel.INFO, "Unread bytes in the region: {0}", fileLength);
				// deal with a remaining fileLength that is greater than int
				while (fileLength > int.MaxValue)
				{
					reader.ReadBytes(int.MaxValue);
					fileLength -= int.MaxValue;
				}
				reader.ReadBytes((int)fileLength);
			}
		}
	}
}
