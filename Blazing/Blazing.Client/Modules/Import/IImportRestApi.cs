using Refit;

namespace Blazing.Client.Modules.Import;

public interface IImportRestApi
{
    [Get("/api/import/load")]
    Task<string> Import();
}