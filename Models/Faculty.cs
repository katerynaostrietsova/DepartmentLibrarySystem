using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("faculties")]
public partial class Faculty
{
    [Key]
    [Column("faculty_id")]
    public int FacultyId { get; set; }

    [Column("faculty_name")]
    [StringLength(255)]
    public string FacultyName { get; set; } = null!;

    [InverseProperty("Faculty")]
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
}
