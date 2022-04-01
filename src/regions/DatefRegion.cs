using System;
using System.Xml;
using DataFileReader;

namespace DataFileReader
{
	// see page 56 (BCDString) and page 69 (Datef) - 2 byte encoded date in yyyy mm dd format
	public class DatefRegion : Region
	{
		private DateTime? dateTime;

        public DateTime? Time => dateTime;

		protected override void ProcessInternal(CustomBinaryReader reader)
		{
			uint year = reader.ReadBCDString(2);
			uint month = reader.ReadBCDString(1);
			uint day = reader.ReadBCDString(1);

			// year 0, month 0, day 0 means date isn't set
			if (year > 0 || month > 0 || day > 0)
			{
				try
				{
					dateTime = new DateTime((int)year, (int)month, (int)day, 0,  0, 0, DateTimeKind.Utc);
				}
				catch (Exception ex)
                {
					Console.Error.WriteLine($"Y:{year}, M:{month}, D:{day}");
					throw new Exception($"Failed to convert to DateTime with {ex.Message}");
                }
			}
		}

		public override string ToString()
		{
			return string.Format("{0}", dateTime);
		}

		protected override void InternalToXML(XmlWriter writer)
		{
			string dateTimeString = null;
			if (this.dateTime != null)
			{
				dateTimeString = dateTime.Value.ToString("u");
			};
			writer.WriteAttributeString("Datef", dateTimeString);
		}
	}
}
