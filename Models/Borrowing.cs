using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

[Table("borrowings")]
public partial class Borrowing
{
    [Key]
    [Column("borrowing_id")]
    public int BorrowingId { get; set; }

    [Column("reader_id")]
    public int ReaderId { get; set; }

    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("borrow_date")]
    public DateOnly BorrowDate { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("return_date")]
    public DateOnly? ReturnDate { get; set; }

    [Column("is_returned")]
    public bool? IsReturned { get; set; }

    [ForeignKey("CopyId")]
    [InverseProperty("Borrowings")]
    public virtual Copy Copy { get; set; } = null!;

    [ForeignKey("ReaderId")]
    [InverseProperty("Borrowings")]
    public virtual Reader Reader { get; set; } = null!;
}
