using System;
using System.Collections.Generic;
using JcoolDevRoom.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace JcoolDevRoom.Data
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Admin> Admins { get; set; } = null!;
        public virtual DbSet<Collaborator> Collaborators { get; set; } = null!;
        public virtual DbSet<CollaboratorImage> CollaboratorImages { get; set; } = null!;
        public virtual DbSet<District> Districts { get; set; } = null!;
        public virtual DbSet<Room> Rooms { get; set; } = null!;
        public virtual DbSet<RoomImage> RoomImages { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=JCOOL\\SQLEXPRESS;Database=JcoolDevRoom;Trusted_Connection=True;Integrated Security=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Admin>(entity =>
            {
                entity.Property(e => e.AdminId).HasColumnName("admin_id");

                entity.Property(e => e.CollaboratorId).HasColumnName("collaborator_id");

                entity.Property(e => e.PasswordAdmin)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("password_admin");

                entity.Property(e => e.UsernameAdmin)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("username_admin");

                entity.HasOne(d => d.Collaborator)
                    .WithMany(p => p.Admins)
                    .HasForeignKey(d => d.CollaboratorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK__Admins__collabor__2F10007B");
            });

            modelBuilder.Entity<Collaborator>(entity =>
            {
                entity.Property(e => e.CollaboratorId).HasColumnName("collaborator_id");

                entity.Property(e => e.AvatarCollaborator)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("avatar_collaborator");

                entity.Property(e => e.CollaboratorCode)
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .HasColumnName("collaborator_code");

                entity.Property(e => e.CommitCollaborator).HasColumnName("commit_collaborator");

                entity.Property(e => e.FacebookCollaborator)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("facebook_collaborator");

                entity.Property(e => e.FullnameCollaborator).HasColumnName("fullname_collaborator");

                entity.Property(e => e.LabelCollaborator).HasColumnName("label_collaborator");

                entity.Property(e => e.MessengerCollaborator)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("messenger_collaborator");

                entity.Property(e => e.PhoneNumberCollaborator)
                    .HasMaxLength(15)
                    .IsUnicode(false)
                    .HasColumnName("phone_number_collaborator");

                entity.Property(e => e.ServiceCollaborator).HasColumnName("service_collaborator");

                entity.Property(e => e.SloganCollaborator).HasColumnName("slogan_collaborator");

                entity.Property(e => e.TiktokCollaborator)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("tiktok_collaborator");

                entity.Property(e => e.ZaloCollaborator)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("zalo_collaborator");

                entity.HasMany(d => d.Districts)
                    .WithMany(p => p.Collaborators)
                    .UsingEntity<Dictionary<string, object>>(
                        "CollaboratorDistrict",
                        l => l.HasOne<District>().WithMany().HasForeignKey("DistrictId").HasConstraintName("FK__Collabora__distr__2C3393D0"),
                        r => r.HasOne<Collaborator>().WithMany().HasForeignKey("CollaboratorId").HasConstraintName("FK__Collabora__colla__2B3F6F97"),
                        j =>
                        {
                            j.HasKey("CollaboratorId", "DistrictId").HasName("PK__Collabor__0D528CE14E1833E7");

                            j.ToTable("CollaboratorDistricts");

                            j.IndexerProperty<int>("CollaboratorId").HasColumnName("collaborator_id");

                            j.IndexerProperty<int>("DistrictId").HasColumnName("district_id");
                        });
            });

            modelBuilder.Entity<CollaboratorImage>(entity =>
            {
                entity.HasKey(e => e.ImageId)
                    .HasName("PK__Collabor__DC9AC95514B885DF");

                entity.Property(e => e.ImageId).HasColumnName("image_id");

                entity.Property(e => e.CollaboratorId).HasColumnName("collaborator_id");

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("image_url");

                entity.HasOne(d => d.Collaborator)
                    .WithMany(p => p.CollaboratorImages)
                    .HasForeignKey(d => d.CollaboratorId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__Collabora__colla__267ABA7A");
            });

            modelBuilder.Entity<District>(entity =>
            {
                entity.Property(e => e.DistrictId).HasColumnName("district_id");

                entity.Property(e => e.DistrictName).HasColumnName("district_name");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.Property(e => e.RoomId).HasColumnName("room_id");

                entity.Property(e => e.AddressRoom).HasColumnName("address_room");

                entity.Property(e => e.CapacityRoom).HasColumnName("capacity_room");

                entity.Property(e => e.CollaboratorId).HasColumnName("collaborator_id");

                entity.Property(e => e.DescriptionRoom).HasColumnName("description_room");

                entity.Property(e => e.DistrictId).HasColumnName("district_id");

                entity.Property(e => e.LabelRoom).HasColumnName("label_room");

                entity.Property(e => e.MainImageRoom)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("main_image_room");

                entity.Property(e => e.NameRoom).HasColumnName("name_room");

                entity.Property(e => e.NoteRoom).HasColumnName("note_room");

                entity.Property(e => e.PriceRoom).HasColumnName("price_room");

                entity.Property(e => e.StatusRoom).HasColumnName("status_room");

                entity.Property(e => e.TitleRoom).HasColumnName("title_room");

                entity.Property(e => e.TypeRoom).HasColumnName("type_room");

                entity.HasOne(d => d.Collaborator)
                    .WithMany(p => p.Rooms)
                    .HasForeignKey(d => d.CollaboratorId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK__Rooms__collabora__32E0915F");

                entity.HasOne(d => d.District)
                    .WithMany(p => p.Rooms)
                    .HasForeignKey(d => d.DistrictId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK__Rooms__district___31EC6D26");
            });

            modelBuilder.Entity<RoomImage>(entity =>
            {
                entity.HasKey(e => e.ImageId)
                    .HasName("PK__RoomImag__DC9AC95550DA4409");

                entity.Property(e => e.ImageId).HasColumnName("image_id");

                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasColumnName("image_url");

                entity.Property(e => e.RoomId).HasColumnName("room_id");

                entity.HasOne(d => d.Room)
                    .WithMany(p => p.RoomImages)
                    .HasForeignKey(d => d.RoomId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK__RoomImage__room___35BCFE0A");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
