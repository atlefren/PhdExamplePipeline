using System;
using System.Threading.Tasks;
using PhdReferenceImpl.Models;

namespace PhdReferenceImpl.ReadProjectionHandler
{
    /*
     * Interface describing how events are persisted to a Read Projection
     */
    public interface IReadProjectionHandler
    {
        Task EnsureTable(Guid datasetId);
        Task Update(Guid datasetId, Event @event);
    }
}