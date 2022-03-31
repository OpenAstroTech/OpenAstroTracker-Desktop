using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OATTest
{
	public class TestManager
	{
		List<CommandTest> _tests;
		public TestManager()
		{
			_tests = new List<CommandTest>();
		}

		public void LoadTestSuite(string testXmlFile)
		{
			_tests.Clear();
			XDocument doc = XDocument.Load(testXmlFile);
			foreach (var testXml in doc.Element("TestSuites").Element("TestSuite").Elements("Test"))
			{
				var test = new CommandTest(testXml);
				_tests.Add(test);
			}
		}

		public IList<CommandTest> Tests { get { return _tests; } }

		internal void ResetAllTests()
		{
			foreach (var test in _tests)
			{
				test.Reset();
			}
		}
	}
}
