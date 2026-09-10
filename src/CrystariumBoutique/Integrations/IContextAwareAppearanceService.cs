using CrystariumBoutique.Core.Errors;
using CrystariumBoutique.Core.Integrations;

namespace CrystariumBoutique.Integrations;

internal interface IContextAwareAppearanceService : IAppearanceService, IDisposable
{
    void PrepareForContextTransition();

    Result RebindToCurrentContext();
}
