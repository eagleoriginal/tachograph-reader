using System;
using System.Xml;
using DataFileReader;

namespace DataFileReader
{
	public class CountryRegion : Region
	{
		private string countryName;
		private byte byteValue;

        public static string ObtainCountryName(byte countryCode)
        {
            if (countryCode < countries.Length)
                return countries[countryCode];
            else if (countryCode == 0xFD)
                return "European Community";
            else if (countryCode == 0xFE)
                return "Europe";
            else if (countryCode == 0xFF)
                return "World";
            else
                return "UNKNOWN";
        }

		protected override void ProcessInternal(CustomBinaryReader reader)
		{
			this.byteValue = reader.ReadByte();
			countryName = ObtainCountryName(byteValue);
        }

		public override string ToString()
		{
			return countryName;
		}

		public byte GetId()
		{
			return this.byteValue;
		}

		protected override void InternalToXML(XmlWriter writer)
		{
			writer.WriteAttributeString("Name", this.ToString());
			writer.WriteString(this.byteValue.ToString());
		}
	}
}
