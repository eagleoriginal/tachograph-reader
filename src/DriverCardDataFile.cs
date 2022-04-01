using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using DataFileReader;

namespace DataFileReader
{
	/// <summary>
	/// Wrapper for DataFile that initialises with driver card config
	/// </summary>
	public class DriverCardDataFile : DataFile
	{
		public static DataFile Create()
		{
			// construct using embedded config
			Assembly a = typeof(DriverCardDataFile).GetTypeInfo().Assembly;
			string name = a.FullName.Split(',')[0]+".DriverCardData.config";
			Stream stm = a.GetManifestResourceStream(name);
			XmlReader xtr = XmlReader.Create(stm);

			return Create(xtr);
		}

        private static XmlDocument m_xmlDocument;

		public static DataFile CreateOptimized()
        {
            Assembly a = typeof(DriverCardDataFile).GetTypeInfo().Assembly;
            string name = a.FullName.Split(',')[0] + ".DriverCardData.config";
            Stream stm = a.GetManifestResourceStream(name);
			if(m_xmlDocument == null)
            {
				var xmlDocument = new XmlDocument();
                using var textReader = new StreamReader(stm, Encoding.UTF8);
                xmlDocument.Load(textReader);

                m_xmlDocument ??= xmlDocument;
            }
            
			XmlReader xtr = new XmlNodeReader(m_xmlDocument.DocumentElement);
            
			return Create(xtr);
        }
	}
}
