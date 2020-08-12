using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhdReferenceImpl.Database
{
    /*
     * Interface describing required database operations needed for the example.
     */

    public interface IDatabaseEngine
    {
        public Task CreateTable(string tableName, IEnumerable<Column> columns);
        public Task Upsert(string tableName, IEnumerable<Cell> row);
        public Task Delete(string tableName, Guid rowId);
    }
}