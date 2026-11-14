namespace CryptoSimulator.Configuration
{
    public static class DotEnv
    {
        // Looks for a ".env" file in the start folder and its parents.
        public static Dictionary<string, string> Load(string startDirectory)
        {
            var values = new Dictionary<string, string>();

            for (var dir = new DirectoryInfo(startDirectory); dir != null; dir = dir.Parent)
            {
                var path = Path.Combine(dir.FullName, ".env");
                if (!File.Exists(path))
                {
                    continue;
                }

                foreach (var line in File.ReadAllLines(path))
                {
                    var trimmed = line.Trim();
                    var separator = trimmed.IndexOf('=');
                    if (trimmed.StartsWith('#') || separator <= 0)
                    {
                        continue;
                    }

                    values[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim();
                }

                break;
            }

            return values;
        }
    }
}
