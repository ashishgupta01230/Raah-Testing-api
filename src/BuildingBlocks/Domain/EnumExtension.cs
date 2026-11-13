using System.ComponentModel;

namespace HautSphere.BuildingBlocks.Domain.Extensions
{
    public static class EnumExtension
    {
        public static string GetDescription(this System.Enum enumToUse)
        {
            var memberInfo = enumToUse.GetType().GetMember(enumToUse.ToString());
            if (memberInfo.Length < 1) return enumToUse.ToString();

            var descriptionAttribute = (DescriptionAttribute)memberInfo.FirstOrDefault().GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();

            if (descriptionAttribute == null) return enumToUse.ToString();
            return descriptionAttribute.Description;
        }


    }
}
