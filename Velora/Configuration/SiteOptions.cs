namespace Velora.Configuration;

public sealed class SiteOptions
{
    public const string SectionName = "Site";

    public string PublicUrl { get; set; } = "http://velora-website.runasp.net/";
    public string ContactEmail { get; set; } = "massaging.system.manager@gmail.com";
    public string OwnerName { get; set; } = "Samir Ajaj";
    public string OwnerGitHubUrl { get; set; } = "https://github.com/samirajaj";
}
