using NUnit.Framework;

namespace Cvent.SchemaToPoco.Core.UnitTests.FunctionalTests
{
    [TestFixture]
    public class PatternTest : BaseTest
    {
        [Test]
        public void TestBasic()
        {
            const string schema = @"{
    'type' : 'object',
    'properties' : {
        'foo' : {
            'type' : 'string',
            'description' : 'Match the regex \\\""dev\""\'[a-c] ',
            'pattern' : '^\\\""dev\""\'[a-c]\\s$'
        }
    }
}";
            const string correctResult = @"namespace generated
{
    using System;
    using System.ComponentModel.DataAnnotations;


    public class DefaultClassName
    {

        // Match the regex ""dev""'[a-c]
        [RegularExpression(@""^\\\""""dev\""""\'[a-c]\s$"")]
        public string Foo { get; set; }
    }
}";

            TestBasicEquals(correctResult, JsonSchemaToPoco.Generate(schema));
        }
    }
}
