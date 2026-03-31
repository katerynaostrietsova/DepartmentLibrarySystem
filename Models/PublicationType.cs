using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("publication_types")]
public partial class PublicationType
{
    [Key]
    [Column("publication_type_id")]
    public int PublicationTypeId { get; set; }

    [Column("type_name")]
    [StringLength(100)]
    public string TypeName { get; set; } = null!;

    [InverseProperty("PublicationType")]
    public virtual ICollection<Publication> Publications { get; set; } = new List<Publication>();
}
