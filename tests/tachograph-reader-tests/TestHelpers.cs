using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace tachograph_reader_tests
{
    public static class TestHelpers
    {
        public static void OutToConsole(ReadOnlyMemory<byte> memory)
        {
            Console.WriteLine(memory.Length);
            var packageStr = new StringBuilder();
            for (int i = 0; i < memory.Length; i++)
            {
                packageStr.Append(memory.Span[i].ToString("x2") + " ");
            }

            Console.WriteLine(packageStr);
        }

        public static byte[] ObtainResources(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var existsFullResourceName = assembly.GetManifestResourceNames()
                .SingleOrDefault(s => s.EndsWith(resourceName, StringComparison.CurrentCultureIgnoreCase));

            if (existsFullResourceName == null)
            {
                throw new InvalidOperationException($"Отсутсвует ресурс '{resourceName}'");
            }

            using var stream = assembly.GetManifestResourceStream(existsFullResourceName);
            if (stream == null)
            {
                throw new InvalidOperationException($"Отсутсвует ресурс '{existsFullResourceName}'");
            }

            var result = new byte[stream.Length];
            _ = stream.Read(result, 0, result.Length);

            return result;
        }
    }
}