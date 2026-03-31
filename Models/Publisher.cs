using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("publishers")]
public partial class Publisher
{
    [Key]
    [Column("publisher_id")]
    public int PublisherId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("city")]
    [StringLength(100)]
    public string? City { get; set; }

    [InverseProperty("Publisher")]
    public virtual ICollection<Publication> Publications { get; set; } = new List<Publication>();
}
