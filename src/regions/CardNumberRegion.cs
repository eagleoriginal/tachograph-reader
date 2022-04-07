using System;
using System.Xml;
using System.Xml.Serialization;
using DataFileReader;

namespace DataFileReader
{
	public class CardNumberRegion : Region
	{
		protected string driverIdentification;
		protected string replacementIndex;
		protected string renewalIndex;

        [XmlIgnore]
		public string DriverIdentification => driverIdentification;
		
		[XmlIgnore]
        public string ReplacementIndex => replacementIndex;

        [XmlIgnore]
		public string RenewalIndex => renewalIndex;

        protected override void ProcessInternal(CustomBinaryReader reader)
		{
			driverIdentification=reader.ReadString(14);
			replacementIndex=reader.ReadChar().ToString();
			renewalIndex=reader.ReadChar().ToString();
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}, {2}",
				DriverIdentification, ReplacementIndex, RenewalIndex);
		}

		protected override void InternalToXML(XmlWriter writer)
		{
			writer.WriteAttributeString("ReplacementIndex", ReplacementIndex);
			writer.WriteAttributeString("RenewalIndex", RenewalIndex);

			writer.WriteString(DriverIdentification);
		}
	}
}
