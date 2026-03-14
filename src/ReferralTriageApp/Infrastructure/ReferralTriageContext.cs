using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ReferralTriageApp.Infrastructure;

public partial class ReferralTriageContext : DbContext
{
    public ReferralTriageContext()
    {
    }

    public ReferralTriageContext(DbContextOptions<ReferralTriageContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DomainEventLog> DomainEventLogs { get; set; }

    public virtual DbSet<Referral> Referrals { get; set; }

    public virtual DbSet<SchemaVersion> SchemaVersions { get; set; }

    public virtual DbSet<TriageRecord> TriageRecords { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DomainEventLog>(entity =>
        {
            entity.HasKey(e => e.DomainEventId).HasName("PK__DomainEv__A10C7D444BB7D679");

            entity.ToTable("DomainEventLog");

            entity.HasIndex(e => e.CreatedAt, "IX_DomainEventLog_CreatedAt");

            entity.HasIndex(e => e.EventType, "IX_DomainEventLog_EventType");

            entity.HasIndex(e => e.ReferralId, "IX_DomainEventLog_ReferralId");

            entity.Property(e => e.DomainEventId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Referral>(entity =>
        {
            entity.HasKey(e => e.ReferralId).HasName("PK__Referral__A2C4A9667F644C90");

            entity.ToTable("Referral");

            entity.HasIndex(e => e.CreatedAt, "IX_Referral_CreatedAt");

            entity.HasIndex(e => e.Status, "IX_Referral_Status");

            entity.HasIndex(e => e.SubmittedBy, "IX_Referral_SubmittedBy");

            entity.Property(e => e.ReferralId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DocumentFormat)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.DocumentHash)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("pending");
            entity.Property(e => e.SubmittedBy).HasMaxLength(255);
        });

        modelBuilder.Entity<SchemaVersion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SchemaVersions_Id");

            entity.Property(e => e.Applied).HasColumnType("datetime");
            entity.Property(e => e.ScriptName).HasMaxLength(255);
        });

        modelBuilder.Entity<TriageRecord>(entity =>
        {
            entity.HasKey(e => e.TriageRecordId).HasName("PK__TriageRe__1A2EACC4674C4D66");

            entity.ToTable("TriageRecord");

            entity.HasIndex(e => e.CreatedAt, "IX_TriageRecord_CreatedAt");

            entity.HasIndex(e => e.ReferralId, "IX_TriageRecord_ReferralId");

            entity.HasIndex(e => e.Specialty, "IX_TriageRecord_Specialty");

            entity.HasIndex(e => e.Urgency, "IX_TriageRecord_Urgency");

            entity.HasIndex(e => e.ReferralId, "UQ__TriageRe__A2C4A967CDB7FB0D").IsUnique();

            entity.Property(e => e.TriageRecordId).ValueGeneratedNever();
            entity.Property(e => e.ClinicalSummary).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.ModifiedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Specialty)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Urgency)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Referral).WithOne(p => p.TriageRecord)
                .HasForeignKey<TriageRecord>(d => d.ReferralId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TriageRec__Refer__5AEE82B9");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC074A0A87F6");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4CBEB3599").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053465A87573").IsUnique();

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.ModifiedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Username).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
