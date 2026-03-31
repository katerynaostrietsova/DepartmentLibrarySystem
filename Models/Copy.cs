using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("copies")]
[Index("InventoryNumber", Name = "copies_inventory_number_key", IsUnique = true)]
public partial class Copy
{
    [Key]
    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("publication_id")]
    public int? PublicationId { get; set; }

    [Column("inventory_number")]
    [StringLength(50)]
    public string InventoryNumber { get; set; } = null!;

    [Column("acquisition_date")]
    public DateOnly? AcquisitionDate { get; set; }

    [Column("is_available")]
    public bool? IsAvailable { get; set; }

    [InverseProperty("Copy")]
    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    [ForeignKey("PublicationId")]
    [InverseProperty("Copies")]
    public virtual Publication? Publication { get; set; }
}
