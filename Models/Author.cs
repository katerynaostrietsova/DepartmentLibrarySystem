using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("authors")]
public partial class Author
{
    [Key]
    [Column("author_id")]
    public int AuthorId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("birth_year")]
    public int? BirthYear { get; set; }

    [Column("country")]
    [StringLength(100)]
    public string? Country { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("Authors")]
    public virtual ICollection<Publication> Publications { get; set; } = new List<Publication>();
}
