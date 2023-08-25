using System.Collections.Generic;

namespace OpenMine.Modding;

public class ModMetadata
{
    public string Id { get; set; }
    public string Version { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public IList<string> Authors { get; set; }
    public Dictionary<string, string> Contact = new Dictionary<string, string>
    {
        { "issues", "" },
        { "sources", "" },
    };
    public string License { get; set; }
    public string Icon { get; set; }
    public Dictionary<string, string> Dependencies;
}
