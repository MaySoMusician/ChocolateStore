using System;
using System.Linq;
using ChocolateStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChocolateStoreTest
{
    [TestClass]
    public class ArgumentParserTests
    {
        [TestMethod]
        public void NoVariables()
        {
            var arguments = ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename" });
            Assert.AreEqual(".\\cache", arguments.Directory);
            Assert.AreEqual("packagename", arguments.PackageName);
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void NoPackageName()
        {
            ArgumentParser.ParseArguments(new[] { ".\\cache" });
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void NoArguments()
        {
            ArgumentParser.ParseArguments(new string[] { });
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void MalformedVariable()
        {
            ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "variablevalue" });
        }

        [TestMethod]
        public void OneSimpleVariable()
        {
            var arguments = ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "$variable=value" });
            Assert.AreEqual(1, arguments.Variables.Count());
            Assert.AreEqual("variable", arguments.Variables.ElementAt(0).Item1);
            Assert.AreEqual(1, arguments.Variables.ElementAt(0).Item2.Count());
            Assert.AreEqual("value", arguments.Variables.ElementAt(0).Item2.ElementAt(0));
        }

        [TestMethod]
        public void OneVariableWithAlternatives()
        {
            var arguments = ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "$variable=value1|value2|value3" });
            Assert.AreEqual(1, arguments.Variables.Count());
            Assert.AreEqual("variable", arguments.Variables.ElementAt(0).Item1);
            Assert.AreEqual(3, arguments.Variables.ElementAt(0).Item2.Count());
            Assert.AreEqual("value1", arguments.Variables.ElementAt(0).Item2.ElementAt(0));
            Assert.AreEqual("value2", arguments.Variables.ElementAt(0).Item2.ElementAt(1));
            Assert.AreEqual("value3", arguments.Variables.ElementAt(0).Item2.ElementAt(2));
        }

        [TestMethod]
        public void TwoSimpleVariables()
        {
            var arguments = ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "$variable1=value1", "$variable2=value2" });
            Assert.AreEqual(2, arguments.Variables.Count());
            Assert.AreEqual("variable1", arguments.Variables.ElementAt(0).Item1);
            Assert.AreEqual(1, arguments.Variables.ElementAt(0).Item2.Count());
            Assert.AreEqual("value1", arguments.Variables.ElementAt(0).Item2.ElementAt(0));
            Assert.AreEqual("variable2", arguments.Variables.ElementAt(1).Item1);
            Assert.AreEqual(1, arguments.Variables.ElementAt(1).Item2.Count());
            Assert.AreEqual("value2", arguments.Variables.ElementAt(1).Item2.ElementAt(0));
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void MalformedVariable2()
        {
            ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "$variable=" });
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void MalformedVariable3()
        {
            ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "=value" });
        }

        [ExpectedException(typeof(ArgumentException))]
        [TestMethod]
        public void MalformedVariable4()
        {
            ArgumentParser.ParseArguments(new[] { ".\\cache", "packagename", "variable=value" });
        }
    }
}
