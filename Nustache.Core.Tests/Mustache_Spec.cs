﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DynamicExpresso;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using YamlDotNet.Serialization;

namespace Nustache.Core.Tests
{
    [TestFixture]
    public class Mustache_Spec
    {
        public static Int32 GlobalCalls;

        [Test]
        [TestCaseSource(nameof(Comments))]
        [TestCaseSource(nameof(Delimiters))]
        [TestCaseSource(nameof(Interpolation))]
        [TestCaseSource(nameof(Inverted))]
        [TestCaseSource(nameof(Partials))]
        [TestCaseSource(nameof(Sections))]
        [TestCaseSource(nameof(Lambdas))]
        public void AllTests(string name, Dictionary<object, object> data, string template, Dictionary<object, string> partials, string expected)
        {
            FixData(data);

            var actual = Render.StringToString(
                template,
                data,
                partial =>
                {
                    if (partials.ContainsKey(partial))
                    {
                        var t = new Template();
                        t.Load(new StringReader(partials[partial]));
                        return t;
                    }
                    else
                    {
                        return null;
                    }
                });

            Assert.AreEqual(expected, actual);
        }

        private void FixData(Dictionary<object, object> data)
        {
            FixNumbers(data);
            FixFalseValues(data);
            FixLambdas(data);
        }

        private void FixNumbers(Dictionary<object, object> data)
        {
            Visit(data,
                value => Regex.IsMatch(value, @"^\d+(\.\d+)?$"),
                value => double.Parse(value));
        }

        private void FixFalseValues(Dictionary<object, object> data)
        {
            Visit(data,
                value => value == "false",
                value => false);
        }

        private void FixLambdas(Dictionary<object, object> data)
        {
            if (data.ContainsKey("lambda"))
            {
                var res = (Dictionary<object, object>)data["lambda"];

                //Hack for Interpolation Multiple calls as it uses globals which the library doesn't support entirely.
                if (((String)res["js"]).Contains(".calls"))
                {
                    data["lambda"] = (Lambda<object>)(() =>
                    {
                        return ++Mustache_Spec.GlobalCalls;
                    });
                }
                else
                {
                    data["lambda"] = res["js"];
                }

            }

            Visit(data,
                value => (value.Contains("function()") || value.Contains("function(txt)")),
                value =>
                {
                    if (value.Contains("function()"))
                    {
                        var match = Regex.Match(value, @"function\(\)\s*{\s*return\s*([A-Za-z0-9 \"">=|{(}?+#._:;)]*)\s* }");
                        if(match.Success)
                        {
                            var body = match.Groups[1].Value;

                            return new Interpreter().ParseAsDelegate<Lambda<object>>(body);
                        }

                    }
                    else if (value.Contains("function(txt)"))
                    {
                        var match = Regex.Match(value, @"function\((\w*)\)\s*{\s*return\s*([A-Za-z0-9 \"">=|{(}?+#._:;)]*)\s* }");
                        if(match.Success)
                        {
                            var argumentName = match.Groups[1].Value;
                            var body = match.Groups[2].Value;

                            return new Interpreter().ParseAsDelegate<Lambda<string, object>>(body, argumentName);
                        }
                    }
                    return null;
                });
        }

        private void Visit(object value, Func<string, bool> pred, Func<string, object> func)
        {
            if (value is List<object>)
            {
                var list = (List<object>)value;

                for (var i = 0; i < list.Count; i++)
                {
                    var val = list[i];

                    if (val is string && pred((string)val))
                    {
                        list[i] = func((string)val);
                    }
                    else
                    {
                        Visit(val, pred, func);
                    }
                }
            }
            else if (value is Dictionary<object, object>)
            {
                var dict = (Dictionary<object, object>)value;

                foreach (var key in dict.Keys.ToArray()) // Copy the array so we can modify it while looping.
                {
                    var val = dict[key];

                    if (val is string && pred((string)val))
                    {
                        dict[key] = func((string)val);
                    }
                    else
                    {
                        Visit(val, pred, func);
                    }
                }
            }
        }

        public static IEnumerable<ITestCaseData> Comments() { return GetTestCases("comments"); }
        public static IEnumerable<ITestCaseData> Delimiters() { return GetTestCases("delimiters"); }
        public static IEnumerable<ITestCaseData> Interpolation() { return GetTestCases("interpolation"); }
        public static IEnumerable<ITestCaseData> Inverted() { return GetTestCases("inverted"); }
        public static IEnumerable<ITestCaseData> Partials() { return GetTestCases("partials"); }
        public static IEnumerable<ITestCaseData> Sections() { return GetTestCases("sections"); }
        public static IEnumerable<ITestCaseData> Lambdas() { return GetTestCases("~lambdas"); }

        public static IEnumerable<ITestCaseData> GetTestCases(string file)
        {
            var text = File.ReadAllText(string.Format("../../../../spec/specs/{0}.yml", file));
            if (file.Equals("~lambdas")) text = CleanLambdaFile(text);

            var deserializer = new Deserializer();
            var doc = deserializer.Deserialize<SpecDoc>(new StringReader(text));

            return doc.tests
                .Select(test => new TestCaseData(test.name, test.data, test.template, test.partials, test.expected)
                .SetName(file + ": " + test.name));
        }

        private static string CleanLambdaFile(String fileContents)
        {
            return fileContents.Replace("!code", "");
        }
    }

    public class SpecDoc
    {
        public string overview { get; set; }
        public SpecTest[] tests  { get; set; }
    }

    public class SpecTest
    {
        public string name { get; set; }
        public string desc { get; set; }
        public Dictionary<object, object> data { get; set; }
        public string template { get; set; }
        public Dictionary<object, string> partials { get; set; }
        public string expected { get; set; }
    }
}
