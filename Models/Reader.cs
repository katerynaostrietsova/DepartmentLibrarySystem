using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("readers")]
public partial class Reader
{
    [Key]
    [Column("reader_id")]
    public int ReaderId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("faculty_id")]
    public int? FacultyId { get; set; }

    [Column("department_id")]
    public int? DepartmentId { get; set; }

    [Column("position_id")]
    public int? PositionId { get; set; }

    [Column("registration_date")]
    public DateOnly? RegistrationDate { get; set; }

    [InverseProperty("Reader")]
    public virtual ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();

    [ForeignKey("DepartmentId")]
    [InverseProperty("Readers")]
    public virtual Department? Department { get; set; }

    [ForeignKey("PositionId")]
    [InverseProperty("Readers")]
    public virtual Position? Position { get; set; }
}
