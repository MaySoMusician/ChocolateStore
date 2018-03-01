using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ChocolateStore
{
    public static class ArgumentParser
    {
        private const string ValueSeparator = ",";
        private const string UsageHint = "USAGE: ChocolateStore <directory> <package> ${variable1}=value1,value2 ${variable2}=value3 ...";

        public static Arguments ParseArguments(string[] args)
        {
            Arguments arguments = new Arguments();

            if (args.Length < 2)
            {
                throw new ArgumentException("USAGE: ChocolateStore <directory> <package>");
            }

            arguments.Directory = args[0];
            arguments.PackageName = args[1];
            var variableList = new List<Tuple<string, IEnumerable<string>>>();
            for (var argNr = 2; argNr < args.Length; argNr++)
            {
                var parsedVariable = ParseVariable(args[argNr]);
                if (parsedVariable == null)
                    throw new ArgumentException(UsageHint);
                variableList.Add(parsedVariable);
            }

            arguments.Variables = variableList;

            return arguments;
        }

        private static Tuple<string, IEnumerable<string>> ParseVariable(string expression)
        {
            var regex = new Regex("\\$\\{(.+?)\\}=(.+)");
            var match = regex.Match(expression);
            if (match.Success == false)
                return null;

            return Tuple.Create(match.Groups[1].Value, match.Groups[2].Value.Split(ValueSeparator.ToCharArray()).AsEnumerable());
        }
    }
}
