using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using DataFileReader;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;

namespace tachograph_reader_tests
{
    [TestFixture]
    public class TestsReadDddFiles
    {
        private ILoggerFactory m_loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog(new LoggerConfiguration().
            MinimumLevel.Debug().
            WriteTo.Console(outputTemplate:
                "{Timestamp} ({ThreadId}) [{Level}] {SourceContext} - {Message} {Properties:lj}{NewLine}{Exception}").CreateLogger(), true));

        [Test]
        public void Test_ExportToXml()
        {
            Console.SetOut(TestContext.Progress);
            var ddFile = TestHelpers.ObtainResources("M_20220218_1201__RUD00000933463.DDD");
            DataFile dcdf = DriverCardDataFile.CreateOptimized();

            dcdf.Process(new MemoryStream(ddFile));
            var textWriter = new StringWriter();
            var xmlWriter = XmlTextWriter.Create(textWriter, new XmlWriterSettings()
            {
                Indent = true
            });

            dcdf.ToXML(xmlWriter);
            xmlWriter.Flush();
            textWriter.Flush();

            File.WriteAllText("ParsedDddFile.xml", textWriter.ToString(), Encoding.UTF8);
        }


        [Test]
        public void Test_ReadFile_ValuablePart()
        {
            Console.SetOut(TestContext.Progress);
            var ddFile = TestHelpers.ObtainResources("M_20220218_1201__RUD00000933463.DDD");
            var dddFile = TachoDriverDddFile.BuildDriverFileWithValuableData(new MemoryStream(ddFile));


            {
                Console.WriteLine("------------------- " + nameof(dddFile.LastCardDownload));
                Console.WriteLine(dddFile.LastCardDownload);
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + dddFile.CardIdentification.GetType());
                Console.WriteLine(dddFile.CardIdentification);
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + dddFile.DriverLicenseInfo.GetType());
                Console.WriteLine(dddFile.DriverLicenseInfo);
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.CardCurrentUse));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.CardCurrentUse));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardFaultRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardFaultRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardEventRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardEventRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardActivityDailyRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardActivityDailyRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualVehicleUsedRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualVehicleUsedRecords));
                Console.WriteLine("-------------------");
            }
        }

        [Test]
        public void Test_ReadFile_OnlyHeader()
        {
            Console.SetOut(TestContext.Progress);
            var ddFile = TestHelpers.ObtainResources("M_20220218_1201__RUD00000933463.DDD");
            var dddFile = TachoDriverDddFile.BuildDriverFileForHeader(new MemoryStream(ddFile));

            {
                Console.WriteLine("------------------- " + nameof(dddFile.LastCardDownload));
                Console.WriteLine(dddFile.LastCardDownload);
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + dddFile.CardIdentification.GetType());
                Console.WriteLine(dddFile.CardIdentification);
                Console.WriteLine(dddFile.CardIdentification.CardNumber.ToCardNumberV2());
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + dddFile.DriverLicenseInfo.GetType());
                Console.WriteLine(dddFile.DriverLicenseInfo);
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.CardCurrentUse));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.CardCurrentUse));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardFaultRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardFaultRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardEventRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardEventRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualCardActivityDailyRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualCardActivityDailyRecords));
                Console.WriteLine("-------------------");
            }

            {
                Console.WriteLine("------------------- " + nameof(dddFile.ActualVehicleUsedRecords));
                Console.WriteLine(JsonConvert.SerializeObject(dddFile.ActualVehicleUsedRecords));
                Console.WriteLine("-------------------");
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Test_ParallelRead(bool onlyHeaders)
        {
            Console.SetOut(TestContext.Progress);
            var ddFile = TestHelpers.ObtainResources("M_20220218_1201__RUD00000933463.DDD");

            var _ = DriverCardDataFile.CreateOptimized();
            var __ = DriverCardDataFile.Create();
            
            var sw = Stopwatch.StartNew();
            Parallel.For(0, 1000, parallelOptions: new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = 4,
                TaskScheduler = null
            }, i =>
            {
                var file = onlyHeaders ? TachoDriverDddFile.BuildDriverFileForHeader(new MemoryStream(ddFile)) : 
                    TachoDriverDddFile.BuildDriverFileWithValuableData(new MemoryStream(ddFile));
            });
            sw.Stop();
            Console.WriteLine("Parallel: " + sw.Elapsed);

            sw = Stopwatch.StartNew();
            Parallel.For(0, 1000, parallelOptions: new ParallelOptions
            {
                CancellationToken = default,
                MaxDegreeOfParallelism = 4,
                TaskScheduler = null
            }, i =>
            {
                var file = onlyHeaders ? TachoDriverDddFile.BuildDriverFileForHeader(new MemoryStream(ddFile)) :
                    TachoDriverDddFile.BuildDriverFileWithValuableData(new MemoryStream(ddFile));
            });
            sw.Stop();
            Console.WriteLine("Parallel 2: " + sw.Elapsed);

            sw = Stopwatch.StartNew();

            for (var i = 0; i < 1000; i++)
            {
                var file = onlyHeaders ? TachoDriverDddFile.BuildDriverFileForHeader(new MemoryStream(ddFile)) :
                    TachoDriverDddFile.BuildDriverFileWithValuableData(new MemoryStream(ddFile));
            };
            sw.Stop();
            Console.WriteLine("One Thread: " + sw.Elapsed);
        }
    }
}
