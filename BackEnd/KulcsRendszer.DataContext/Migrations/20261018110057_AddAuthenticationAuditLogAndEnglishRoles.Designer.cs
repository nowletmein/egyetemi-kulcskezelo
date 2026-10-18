using System;
using KulcsRendszer.DataContext.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace KulcsRendszer.DataContext.Migrations
{
    [DbContext(typeof(KulcsRendszerDbContext))]
    [Migration("20261018110057_AddAuthenticationAuditLogAndEnglishRoles")]
    partial class AddAuthenticationAuditLogAndEnglishRoles
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.20")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ClassRoomMasterKey", b =>
                {
                    b.Property<string>("AccessibleRoomsId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("MasterKeyId")
                        .HasColumnType("int");

                    b.HasKey("AccessibleRoomsId", "MasterKeyId");

                    b.HasIndex("MasterKeyId");

                    b.ToTable("MasterKeyRooms", (string)null);
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.AuthAuditLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("EmailIdentifier")
                        .IsRequired()
                        .HasMaxLength(320)
                        .HasColumnType("nvarchar(320)");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("IpAddress")
                        .HasMaxLength(45)
                        .HasColumnType("nvarchar(45)");

                    b.HasKey("Id");

                    b.ToTable("AuthAuditLogs");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Booking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("SpecialRequest")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RoomId");

                    b.HasIndex("UserId");

                    b.ToTable("Bookings");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.ClassRoom", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Building")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Capacity")
                        .HasColumnType("int");

                    b.Property<string>("Equipment")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FloorId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("FloorId");

                    b.ToTable("ClassRooms");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Floor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Number")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Floors");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.IssueTicket", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("KeyId")
                        .HasColumnType("int");

                    b.Property<int>("ReporterId")
                        .HasColumnType("int");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("KeyId");

                    b.HasIndex("ReporterId");

                    b.HasIndex("RoomId");

                    b.ToTable("IssueTickets");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Key", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("KeyType")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("nvarchar(8)");

                    b.Property<string>("RoomId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoomId");

                    b.ToTable("Keys");

                    b.HasDiscriminator<string>("KeyType").HasValue("Standard");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.KeyMovement", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("BookingId")
                        .HasColumnType("int");

                    b.Property<string>("IdentificationMethod")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("KeyId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("BookingId");

                    b.HasIndex("KeyId");

                    b.HasIndex("UserId");

                    b.ToTable("KeyMovements");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Maintenance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("AdminId")
                        .HasColumnType("int");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("RoomId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("AdminId");

                    b.HasIndex("RoomId");

                    b.ToTable("Maintenances");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.MasterKeyPermission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("GrantedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("GrantedByAdminId")
                        .HasColumnType("int");

                    b.Property<int>("KeyId")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("GrantedByAdminId");

                    b.HasIndex("KeyId");

                    b.HasIndex("UserId");

                    b.ToTable("MasterKeyPermissions");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Roles");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            Name = "Administrator"
                        },
                        new
                        {
                            Id = 2,
                            Name = "Operator"
                        },
                        new
                        {
                            Id = 3,
                            Name = "OperationsDirector"
                        },
                        new
                        {
                            Id = 4,
                            Name = "Instructor"
                        });
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("RoleId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("RoleId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.MasterKey", b =>
                {
                    b.HasBaseType("KulcsRendszer.DataContext.Entities.Key");

                    b.Property<string>("MasterKeyName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasDiscriminator().HasValue("Master");
                });

            modelBuilder.Entity("ClassRoomMasterKey", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.ClassRoom", null)
                        .WithMany()
                        .HasForeignKey("AccessibleRoomsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.MasterKey", null)
                        .WithMany()
                        .HasForeignKey("MasterKeyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Booking", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.ClassRoom", "Room")
                        .WithMany("Bookings")
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "User")
                        .WithMany("Bookings")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Room");

                    b.Navigation("User");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.ClassRoom", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.Floor", "Floor")
                        .WithMany("Rooms")
                        .HasForeignKey("FloorId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Floor");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.IssueTicket", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.Key", "Key")
                        .WithMany("IssueTickets")
                        .HasForeignKey("KeyId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "Reporter")
                        .WithMany("ReportedTickets")
                        .HasForeignKey("ReporterId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.ClassRoom", "Room")
                        .WithMany("IssueTickets")
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Key");

                    b.Navigation("Reporter");

                    b.Navigation("Room");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Key", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.ClassRoom", "ClassRoom")
                        .WithMany("Keys")
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("ClassRoom");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.KeyMovement", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.Booking", "Booking")
                        .WithMany("KeyMovements")
                        .HasForeignKey("BookingId");

                    b.HasOne("KulcsRendszer.DataContext.Entities.Key", "Key")
                        .WithMany("KeyMovements")
                        .HasForeignKey("KeyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "User")
                        .WithMany("KeyMovements")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Booking");

                    b.Navigation("Key");

                    b.Navigation("User");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Maintenance", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "Admin")
                        .WithMany("OrderedMaintenances")
                        .HasForeignKey("AdminId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.ClassRoom", "Room")
                        .WithMany("Maintenances")
                        .HasForeignKey("RoomId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Admin");

                    b.Navigation("Room");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.MasterKeyPermission", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "GrantedByAdmin")
                        .WithMany("GrantedPermissions")
                        .HasForeignKey("GrantedByAdminId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.Key", "Key")
                        .WithMany("MasterKeyPermissions")
                        .HasForeignKey("KeyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("KulcsRendszer.DataContext.Entities.User", "User")
                        .WithMany("MasterKeyPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("GrantedByAdmin");

                    b.Navigation("Key");

                    b.Navigation("User");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.User", b =>
                {
                    b.HasOne("KulcsRendszer.DataContext.Entities.Role", "Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Role");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Booking", b =>
                {
                    b.Navigation("KeyMovements");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.ClassRoom", b =>
                {
                    b.Navigation("Bookings");

                    b.Navigation("IssueTickets");

                    b.Navigation("Keys");

                    b.Navigation("Maintenances");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Floor", b =>
                {
                    b.Navigation("Rooms");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Key", b =>
                {
                    b.Navigation("IssueTickets");

                    b.Navigation("KeyMovements");

                    b.Navigation("MasterKeyPermissions");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("KulcsRendszer.DataContext.Entities.User", b =>
                {
                    b.Navigation("Bookings");

                    b.Navigation("GrantedPermissions");

                    b.Navigation("KeyMovements");

                    b.Navigation("MasterKeyPermissions");

                    b.Navigation("OrderedMaintenances");

                    b.Navigation("ReportedTickets");
                });
#pragma warning restore 612, 618
        }
    }
}
