using System.Text.RegularExpressions;

namespace HautSphere.BuildingBlocks.Neo4J.Configuration
{
    public static class IdentifierValidator
    {
        private static readonly Regex _regex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public static bool IsValid(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return false;
            return _regex.IsMatch(identifier);
        }

        public static void ThrowIfInvalid(string identifier, string paramName)
        {
            if (!IsValid(identifier)) throw new System.ArgumentException($"Invalid identifier: {identifier}", paramName);
        }
    }
}
