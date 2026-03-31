using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("publications")]
public partial class Publication
{
    [Key]
    [Column("publication_id")]
    public int PublicationId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("annotation")]
    public string? Annotation { get; set; }

    [Column("year")]
    public int? Year { get; set; }

    [Column("language")]
    [StringLength(100)]
    public string? Language { get; set; }

    [Column("publisher_id")]
    public int? PublisherId { get; set; }

    [Column("publication_type_id")]
    public int? PublicationTypeId { get; set; }

    [InverseProperty("Publication")]
    public virtual ICollection<Copy> Copies { get; set; } = new List<Copy>();

    [ForeignKey("PublicationTypeId")]
    [InverseProperty("Publications")]
    public virtual PublicationType? PublicationType { get; set; }

    [ForeignKey("PublisherId")]
    [InverseProperty("Publications")]
    public virtual Publisher? Publisher { get; set; }

    [ForeignKey("PublicationId")]
    [InverseProperty("Publications")]
    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();
}
