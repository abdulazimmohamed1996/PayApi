namespace PayArabic.Core.Interfaces;

public interface IConfigurationDao
{
    string GetConfigurationByName(string name);
    List<ConfigurationDTO> GetByIntegrationType(string type);
    List<ConfigurationDTO> GetByIntegrationCode(string code);
}

