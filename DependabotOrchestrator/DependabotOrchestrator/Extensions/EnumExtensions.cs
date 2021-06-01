using System;

namespace DependabotOrchestrator.Extensions
{
    public static class EnumExtensions
    {
        public static string Name (this Enum obj)
            => Enum.GetName(obj.GetType(), obj);

    }
}
