namespace LinqToEntityApp.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("T_InvoCut")]
    public partial class Invo
    {
        [Key]
        public int Idn { get; set; }

        [Column(TypeName = "date")]
        public DateTime Dt_Invo { get; set; }

        public double Val { get; set; }

        [Required]
        [StringLength(100)]
        public string Note { get; set; }

    }
}
