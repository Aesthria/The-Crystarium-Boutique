namespace CrystariumBoutique.Integrations;

internal interface IDependencyAvailabilityRefresher
{
    bool IsAvailable { get; }

    bool RefreshAvailability();
}
