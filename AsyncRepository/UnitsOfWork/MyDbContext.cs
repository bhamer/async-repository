using AsyncRepository.Models;
using System.Data.Entity;

namespace AsyncRepository.UnitsOfWork
{
    public class MyDbContext : DbContext
    {
        private DbContextTransaction transaction;
    
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Trade> Trades { get; set; }

        public MyDbContext(string connectionString) 
            : base(connectionString) { }
            
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<MyDbContext>(null);
        }
        
        public virtual int SaveChanges(string contextInfo)
        {
            if (contextInfo == null) throw new ArgumentNullException(nameof(contextInfo));
            if (transaction == null) Database.Connection.Open();
            int changes;
            try
            {
                SetContextInfo(contextInfo);
                changes = base.SaveChanges();
            }
            finally
            {
                if (transaction == null) Database.Connection.Close();
            }
            return changes;
        }

        public virtual async Task<int> SaveChangesAsync(string contextInfo)
        {
            if (contextInfo == null) throw new ArgumentNullException(nameof(contextInfo));
            if (transaction == null) await Database.Connection.OpenAsync();
            int changes;
            try
            {
                SetContextInfo(contextInfo);
                changes = await base.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                if (transaction == null) Database.Connection.Close();
            }
            return changes;
        }

        public virtual IDisposable BeginTransaction()
        {
            if (transaction != null) throw new Exception("A transaction is already in progress.");
            transaction = Database.BeginTransaction();
            return transaction;
        }

        public virtual void CommitTransaction()
        {
            transaction.Commit();
            transaction = null;
        }

        public virtual void RollbackTransaction()
        {
            transaction.Rollback();
            transaction = null;
        }

        /// <summary>
        /// Sets the CONTEXT_INFO on the sql server to the 'contextInfo' parameter. The AuditUser_fn() function on sql server looks in the CONTEXT_INFO variable to get the audit user.
        /// </summary>
        private void SetContextInfo(string contextInfo)
        {
            var command = Database.Connection.CreateCommand();
            command.CommandText = String.Format("DECLARE @temp varbinary(128); SET @temp = CONVERT(varbinary(128), '{0}'); SET CONTEXT_INFO @temp;", contextInfo);
            command.CommandType = System.Data.CommandType.Text;
            if (transaction != null) command.Transaction = transaction.UnderlyingTransaction;
            command.ExecuteNonQuery();
        }
    }

}
