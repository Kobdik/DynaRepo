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

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invo>()
                .Property(e => e.Note)
                .IsUnicode(false);

        }
    }
}
