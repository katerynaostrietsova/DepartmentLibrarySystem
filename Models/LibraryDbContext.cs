using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LibraryWebMvc.Models;

public partial class LibraryDbContext : DbContext
{
    public LibraryDbContext()
    {
    }

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Author> Authors { get; set; }

    public virtual DbSet<Borrowing> Borrowings { get; set; }

    public virtual DbSet<Copy> Copies { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Publication> Publications { get; set; }

    public virtual DbSet<PublicationType> PublicationTypes { get; set; }

    public virtual DbSet<Publisher> Publishers { get; set; }

    public virtual DbSet<Reader> Readers { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=librarydb;Username=ostrietsovakateryna;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Author>(entity =>
        {
            entity.HasKey(e => e.AuthorId).HasName("authors_pkey");
        });

        modelBuilder.Entity<Borrowing>(entity =>
        {
            entity.HasKey(e => e.BorrowingId).HasName("borrowings_pkey");

            entity.Property(e => e.BorrowDate).HasDefaultValueSql("CURRENT_DATE");
            entity.Property(e => e.IsReturned).HasDefaultValue(false);

            entity.HasOne(d => d.Copy).WithMany(p => p.Borrowings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("borrowings_copy_id_fkey");

            entity.HasOne(d => d.Reader).WithMany(p => p.Borrowings)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("borrowings_reader_id_fkey");
        });

        modelBuilder.Entity<Copy>(entity =>
        {
            entity.HasKey(e => e.CopyId).HasName("copies_pkey");

            entity.Property(e => e.IsAvailable).HasDefaultValue(true);

            entity.HasOne(d => d.Publication).WithMany(p => p.Copies)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("copies_publication_id_fkey");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.DepartmentId).HasName("departments_pkey");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Departments)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_faculty");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.FacultyId).HasName("faculties_pkey");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.PositionId).HasName("positions_pkey");

            entity.Property(e => e.PositionId).ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<Publication>(entity =>
        {
            entity.HasKey(e => e.PublicationId).HasName("publications_pkey");

            entity.HasOne(d => d.PublicationType).WithMany(p => p.Publications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_publication_type");

            entity.HasOne(d => d.Publisher).WithMany(p => p.Publications)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_publisher");

            entity.HasMany(d => d.Authors).WithMany(p => p.Publications)
                .UsingEntity<Dictionary<string, object>>(
                    "PublicationAuthor",
                    r => r.HasOne<Author>().WithMany()
                        .HasForeignKey("AuthorId")
                        .HasConstraintName("publication_authors_author_id_fkey"),
                    l => l.HasOne<Publication>().WithMany()
                        .HasForeignKey("PublicationId")
                        .HasConstraintName("publication_authors_publication_id_fkey"),
                    j =>
                    {
                        j.HasKey("PublicationId", "AuthorId").HasName("publication_authors_pkey");
                        j.ToTable("publication_authors");
                        j.IndexerProperty<int>("PublicationId").HasColumnName("publication_id");
                        j.IndexerProperty<int>("AuthorId").HasColumnName("author_id");
                    });
        });

        modelBuilder.Entity<PublicationType>(entity =>
        {
            entity.HasKey(e => e.PublicationTypeId).HasName("publication_types_pkey");
        });

        modelBuilder.Entity<Publisher>(entity =>
        {
            entity.HasKey(e => e.PublisherId).HasName("publishers_pkey");
        });

        modelBuilder.Entity<Reader>(entity =>
        {
            entity.HasKey(e => e.ReaderId).HasName("readers_pkey");

            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Department).WithMany(p => p.Readers).HasConstraintName("readers_department_id_fkey");

            entity.HasOne(d => d.Position).WithMany(p => p.Readers).HasConstraintName("readers_position_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
