using System;
using System.Xml;
using DataFileReader;

namespace DataFileReader
{
	public class CardNumberRegion : Region
	{
		protected string driverIdentification;
		protected string replacementIndex;
		protected string renewalIndex;

        public string DriverIdentification => driverIdentification;

        public string ReplacementIndex => replacementIndex;

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
