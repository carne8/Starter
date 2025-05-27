// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
namespace Starter.UrlSearchEngine.Regex;

using System.Text.RegularExpressions;

public partial class UriRegex
{
    [GeneratedRegex(
        """(?:(?<scheme>[a-z][a-z0-9+.-]+)://)?(?:(?<user>[^@]+@)?(?<host>(?:[a-z0-9.\-_~]+\.+[a-z0-9.\-_~]{2,})|localhost)(?::(?<port>\d+))?)(?<path>(?:[a-z0-9-._~]|%[a-f0-9]|[!$&'()*+,;=:@])+(?:\/(?:[a-z0-9-._~]|%[a-f0-9]|[!$&'()*+,;=:@])*)*|(?:\/(?:[a-z0-9-._~]|%[a-f0-9]|[!$&'()*+,;=:@])+)*)?(?<query>\?(?:[a-z0-9-._~]|%[a-f0-9]|[!$&'()*+,;=:@]|[/?])+)?(?<fragment>\#(?:[a-z0-9-._~]|%[a-f0-9]|[!$&'()*+,;=:@]|[/?])+)?""",
        RegexOptions.None,
        1000
    )]
    public static partial Regex Regex();
}
