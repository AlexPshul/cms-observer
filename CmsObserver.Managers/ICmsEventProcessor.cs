using CmsObserver.Managers.Models;

namespace CmsObserver.Managers;

public interface ICmsEventProcessor
{
    void Enqueue(IReadOnlyCollection<CmsEventModel> events);
}
