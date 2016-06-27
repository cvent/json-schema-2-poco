using System;
using System.Collections.Generic;
using Newtonsoft.Json.Schema;

namespace Cvent.SchemaToPoco.Core.Util
{
    //TODO
    public static class TypeUtils
    {
        /// <summary>
        ///     What primitive objects map to in C#.
        /// </summary>
        private static readonly Dictionary<string, string> Primitives = new Dictionary<string, string>
        {
            {"String", "System.String"},
            {"Float", "System.Double"},
            {"Integer", "System.Int32"},
            {"Boolean", "System.Boolean"},
            {"Object", "System.Object"}
        };

        /// <summary>
        ///     Check if a type is a primitive, or can be treated like one (ie. lowercased type).
        /// </summary>
        /// <param name="t">The type.</param>
        /// <returns>Whether or not it is a primitive type.</returns>
        public static bool IsPrimitive(Type t)
        {
            return t.IsPrimitive || t == typeof(Decimal) || t == typeof(String) || t == typeof(Object);
        }

        /// <summary>
        ///     Get the primitive name of a type if it exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The primitive type as a string, if it exists.</returns>
        public static string GetPrimitiveTypeAsString(JsonSchemaType? type)
        {
            string sType = type.ToString();
            return Primitives.ContainsKey(sType) ? Primitives[sType] : "System.Object";
        }

        /// <summary>
        ///     Get the primitive name of a type if it exists.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The primitive type, if it exists.</returns>
        public static string GetPrimitiveType(Type type)
        {
            string sType = type.ToString();
            Console.WriteLine(sType);

            return Primitives.ContainsKey(sType) ? Primitives[sType] : sType;
        }
    }
}
