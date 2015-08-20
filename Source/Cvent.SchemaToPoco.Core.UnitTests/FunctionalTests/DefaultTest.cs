using NUnit.Framework;

namespace Cvent.SchemaToPoco.Core.UnitTests.FunctionalTests
{
    [TestFixture]
    public class DefaultTest : BaseTest
    {
        [Test]
        public void TestBasic()
        {
            const string schema = @"{
    'type' : 'object',
    'properties' : {
        'foo' : {
            'type': 'string',
            'default': 'hello'
        },
        'number' : {
            'type': 'integer',
            'default': 10
        }
    }
}";
            const string correctResult = @"namespace generated
{
    using System;
    
    
    public class DefaultClassName
    {
        
        public string Foo { get; set; }
        
        public int Number { get; set; }
        
        public DefaultClassName()
        {
            Foo = ""hello"";
            Number = 10;
        }
    }
}";

            TestBasicEquals(correctResult, JsonSchemaToPoco.Generate(schema));
        }
    }
}
