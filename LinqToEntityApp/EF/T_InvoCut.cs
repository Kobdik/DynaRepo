namespace LinqToEntityApp.EF
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("T_Invoice")]
    public partial class Invoice
    {
        [Key]
        public int Idn { get; set; }

        public short Org { get; set; }

        public byte Knd { get; set; }

        [Column(TypeName = "date")]
        public DateTime Dt_Invo { get; set; }

        public double Val { get; set; }

        [Required]
        [StringLength(100)]
        public string Note { get; set; }

        public byte Sdoc { get; set; }

        [Column(TypeName = "date")]
        public DateTime Dt_Sdoc { get; set; }

        public int Lic { get; set; }

        [StringLength(15)]
        public string Usr { get; set; }

        public byte Pnt { get; set; }

    }

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

    [Table("T_InvoR04")]
    public partial class InvoR04
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

    [Table("T_InvoR08")]
    public partial class InvoR08
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

    [Table("T_InvoR16")]
    public partial class InvoR16
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
