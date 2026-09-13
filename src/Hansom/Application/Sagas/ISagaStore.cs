using Hansom.Domain.Sagas;
using Hansom.Domain.Sagas.ValueObjects;

namespace Hansom.Application.Sagas;

/// <summary>
/// Load and success-replace saga instances by type and id. Persistence owns durable rows later.
/// </summary>
public interface ISagaStore
{
    Saga? Load(Type sagaType, SagaId id);

    void Save(Type sagaType, SagaId id, Saga instance);
}
