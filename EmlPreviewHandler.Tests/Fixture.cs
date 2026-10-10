using System;
using System.IO;

namespace EmlPreviewHandler.Tests
{
    internal static class Fixture
    {
        public static string Path(string fileName) =>
            System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
    }
}
