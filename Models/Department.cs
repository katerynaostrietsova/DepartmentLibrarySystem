using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("departments")]
public partial class Department
{
    [Key]
    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Column("department_name")]
    [StringLength(255)]
    public string DepartmentName { get; set; } = null!;

    [Column("faculty_id")]
    public int? FacultyId { get; set; }

    [ForeignKey("FacultyId")]
    [InverseProperty("Departments")]
    public virtual Faculty? Faculty { get; set; }

    [InverseProperty("Department")]
    public virtual ICollection<Reader> Readers { get; set; } = new List<Reader>();
}
