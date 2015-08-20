using NUnit.Framework;

namespace Cvent.SchemaToPoco.Core.UnitTests.FunctionalTests
{
    [TestFixture]
    public class MinMaxTest : BaseTest
    {
        [Test]
        public void TestBasic()
        {
            const string schema = @"{
    'type' : 'object',
    'properties' : {
        'foo' : {
            'type' : 'integer',
            'minimum' : 10,
            'maximum' : 15
        }
    }
}";
            const string correctResult = @"namespace generated
{
    using System;
    using Cvent.SchemaToPoco.Core.ValidationAttributes;


    public class DefaultClassName
    {

        [MinValue(10)]
        [MaxValue(15)]
        public int Foo { get; set; }
    }
}";

            TestBasicEquals(correctResult, JsonSchemaToPoco.Generate(schema));
        }
    }
}
