using System;
using System.ComponentModel;
using System.Reflection;

namespace Cvent.SchemaToPoco.Core.Types
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum element)
        {
            Type type = element.GetType();

            MemberInfo[] memberInfo = type.GetMember(element.ToString());

            if (memberInfo.Length > 0)
            {
                object[] attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                {
                    return ((DescriptionAttribute) attributes[0]).Description;
                }
            }

            return element.ToString();
        }
    }
}


