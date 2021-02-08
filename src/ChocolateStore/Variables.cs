using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChocolateStore
{
    public static class Variables
    {
        /// <summary>
        /// Puts the value into variable markup. E.g. ${name} is replaced by value.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ResolveVariable(string text, string name, string value)
        {
            return text.Replace("${" + name + "}", value);
        }

        public static string ResolveVariables(string text, IEnumerable<Tuple<string, string>> variables)
        {
            foreach(var variable in variables)
            {
                text = ResolveVariable(text, variable.Item1, variable.Item2);
            }

            return text;
        }

        /// <summary>
        /// Returns a string that looks like ${name1}_${name2}_ The string is used to prefix the references in the ps1 file, so that Chocolatey can still resolve the variables on its own when running the installer.
        /// </summary>
        /// <param name="variableNames"></param>
        /// <returns></returns>
        public static string GetPrefixForVariables(IEnumerable<string> variableNames)
        {
            return variableNames.Aggregate("", (output, name) => output + "${" + name + "}_");
        }

        /// <summary>
        /// Resolves variables that only have one value, and not several.
        /// </summary>
        /// <param name="content">the string to work on. This string is passed as a reference AND modified by the method!</param>
        /// <param name="variables">all variables to resolve</param>
        /// <returns>The variables that have more than one value and were not resolved.</returns>
        public static List<Tuple<string, IEnumerable<string>>> ResolveVariablesWithoutAlternatives(ref string content, IEnumerable<Tuple<string, IEnumerable<string>>> variables)
        {
            var variablesWithAlternatives = variables.ToList();

            // replace variables for which there is only one value
            foreach (var variable in variables)
            {
                if (variable.Item2.Count() == 1)
                {
                    content = Variables.ResolveVariable(content, variable.Item1, variable.Item2.First());
                    variablesWithAlternatives.Remove(variable);
                }
            }

            return variablesWithAlternatives;
        }

        /// <summary>
        /// Returns a list of all possible permutations that result from combining the variable values passed into the moethod.
        /// </summary>
        /// <param name="variableOptions"></param>
        /// <returns></returns>
        public static List<List<Tuple<string, string>>> GetVariablePermutations(List<Tuple<string, IEnumerable<string>>> variableOptions)
        {
            var variableCombinations = new List<List<Tuple<string, string>>>();
            RecursiveVariableCombiner(variableOptions.ToList(), new List<Tuple<string, string>>(), variableCombinations);
            return variableCombinations;
        }

        private static void RecursiveVariableCombiner(List<Tuple<string, IEnumerable<string>>> remainingVariableOptions, List<Tuple<string, string>> alreadySetVariableValues, List<List<Tuple<string, string>>> variableCombinations)
        {
            var newVariableList = remainingVariableOptions?.ToList();
            var currentVariable = newVariableList?.FirstOrDefault();
            if (currentVariable == null)
            {
                variableCombinations.Add(alreadySetVariableValues);
                return;
            }

            newVariableList.Remove(currentVariable);

            foreach (var value in currentVariable.Item2)
            {
                var newPath = alreadySetVariableValues.ToList();
                newPath.Add(Tuple.Create(currentVariable.Item1, value));
                RecursiveVariableCombiner(newVariableList, newPath, variableCombinations);
            }
        }

    }
}
