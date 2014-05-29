using MDLFileReaderWriter.MDLFile;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Diagnostics;

namespace TestMDLFileLoad
{


    /// <summary>
    ///This is a test class for MDLFileTest and is intended
    ///to contain all MDLFileTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MDLFileTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Load
        ///</summary>
        //[TestMethod()] // need to fix these test so they can run on MyGet
        public void LoadTest()
        {
            DirectoryInfo di = new DirectoryInfo(@"C:\Program Files (x86)\Microsoft Games\Allegiance\Artwork\");
            var goodFiles = 0;
            var nonBinary = 0;
            foreach (var item in di.EnumerateFiles("*.mdl"))
            {
                MDLFile target = new MDLFile(); // TODO: Initialize to an appropriate value
                try
                {
                    var readToEnd = target.Load(item);
                    if (readToEnd == false && target.Head.magic == 0xDEBADF00)
                    {
                        Console.WriteLine(string.Format("File: {0} - Did not read the whole file", item.Name));
                    }
                    else if (target.Head.magic == 0xDEBADF00)
                    {
                        goodFiles++;
                    }
                    else
                    {
                        nonBinary++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("File: {0} Threw Exception {1} {2}", item.Name, ex.Message, ex.StackTrace));
                }
            }
            Console.WriteLine(string.Format("Successfully read {0} Binary MDL files", goodFiles));
            Console.WriteLine(string.Format("Skipped reading {0} text MDL files", nonBinary));
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }

        /// <summary>
        ///A test for Load
        ///</summary>
        //[TestMethod()] // need to fix these test so they can run on MyGet
        public void LoadTest2()
        {

            MDLFile target = new MDLFile(); // TODO: Initialize to an appropriate value
            try
            {
                var readToEnd = target.Load(new FileInfo(@"C:\Program Files (x86)\Microsoft Games\Allegiance\Artwork\animlaunch.mdl"));
                var text = target.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("File: Threw Exception {0} {1}", ex.Message, ex.StackTrace));
            }
            Assert.Inconclusive("A method that does not return a value cannot be verified.");
        }
    }
}
