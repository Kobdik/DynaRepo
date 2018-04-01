namespace LinqToEntityApp.EF
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class TestModel : DbContext
    {
        public TestModel() : base("name=TestModelConn")
        {
        }

        public virtual DbSet<Invo> Invos { get; set; }

        public virtual DbSet<InvoR04> InvoR04s { get; set; }

        public virtual DbSet<InvoR08> InvoR08s { get; set; }

        public virtual DbSet<InvoR16> InvoR16s { get; set; }

        public virtual DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invo>()
                .Property(e => e.Note)
                .IsUnicode(false);

        }
    }
}
