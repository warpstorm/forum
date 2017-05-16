using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Forum3.Data;
using Forum3.Enums;

namespace Forum3.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20170516131346_boardRelationships")]
    partial class boardRelationships
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Forum3.Models.DataModels.ApplicationUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<DateTime>("Birthday");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<DateTime>("LastOnline");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<DateTime>("Registered");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Board", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CategoryId");

                    b.Property<int>("DisplayOrder");

                    b.Property<int?>("LastMessageId");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<bool>("VettedOnly");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("LastMessageId");

                    b.ToTable("Boards");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("DisplayOrder");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Message", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("DisplayBody")
                        .IsRequired();

                    b.Property<string>("EditedById")
                        .IsRequired();

                    b.Property<string>("EditedByName")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<string>("LastReplyById")
                        .IsRequired();

                    b.Property<string>("LastReplyByName")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<int>("LastReplyId");

                    b.Property<DateTime>("LastReplyPosted");

                    b.Property<string>("LongPreview")
                        .IsRequired();

                    b.Property<string>("OriginalBody")
                        .IsRequired();

                    b.Property<int>("ParentId");

                    b.Property<string>("PostedById")
                        .IsRequired();

                    b.Property<string>("PostedByName")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<int>("Replies");

                    b.Property<int>("ReplyId");

                    b.Property<string>("ShortPreview")
                        .IsRequired();

                    b.Property<DateTime>("TimeEdited");

                    b.Property<DateTime>("TimePosted");

                    b.Property<int>("Views");

                    b.HasKey("Id");

                    b.HasIndex("EditedById");

                    b.HasIndex("LastReplyById");

                    b.HasIndex("PostedById");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.MessageBoard", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BoardId");

                    b.Property<int>("MessageId");

                    b.Property<DateTime>("TimeAdded");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("BoardId");

                    b.HasIndex("MessageId");

                    b.ToTable("MessageBoards");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.MessageThought", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MessageId");

                    b.Property<int>("SmileyId");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("MessageId");

                    b.HasIndex("SmileyId");

                    b.HasIndex("UserId");

                    b.ToTable("MessageThoughts");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Notification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int?>("MessageId");

                    b.Property<string>("TargetUserId")
                        .IsRequired();

                    b.Property<DateTime>("Time");

                    b.Property<int>("Type");

                    b.Property<bool>("Unread");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("MessageId");

                    b.HasIndex("TargetUserId");

                    b.ToTable("Notifications");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Participant", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MessageId");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Participants");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Pin", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("MessageId");

                    b.Property<DateTime>("Time");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Pins");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.SiteSetting", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(64);

                    b.HasKey("Id");

                    b.ToTable("SiteSettings");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Smiley", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Code")
                        .IsRequired();

                    b.Property<decimal?>("DisplayOrder");

                    b.Property<string>("Path")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Smileys");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.ViewLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("LogTime");

                    b.Property<int?>("TargetId");

                    b.Property<int>("TargetType");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("ViewLogs");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("RoleId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Board", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.Category", "Category")
                        .WithMany()
                        .HasForeignKey("CategoryId");

                    b.HasOne("Forum3.Models.DataModels.Message", "LastMessage")
                        .WithMany()
                        .HasForeignKey("LastMessageId");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Message", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.ApplicationUser", "EditedBy")
                        .WithMany()
                        .HasForeignKey("EditedById");

                    b.HasOne("Forum3.Models.DataModels.ApplicationUser", "LastReplyBy")
                        .WithMany()
                        .HasForeignKey("LastReplyById");

                    b.HasOne("Forum3.Models.DataModels.ApplicationUser", "PostedBy")
                        .WithMany()
                        .HasForeignKey("PostedById");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.MessageBoard", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.Board", "Board")
                        .WithMany()
                        .HasForeignKey("BoardId");

                    b.HasOne("Forum3.Models.DataModels.Message", "Message")
                        .WithMany()
                        .HasForeignKey("MessageId");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.MessageThought", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.Message", "Message")
                        .WithMany("Thoughts")
                        .HasForeignKey("MessageId");

                    b.HasOne("Forum3.Models.DataModels.Smiley", "Smiley")
                        .WithMany()
                        .HasForeignKey("SmileyId");

                    b.HasOne("Forum3.Models.DataModels.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("Forum3.Models.DataModels.Notification", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.Message", "Message")
                        .WithMany()
                        .HasForeignKey("MessageId");

                    b.HasOne("Forum3.Models.DataModels.ApplicationUser", "TargetUser")
                        .WithMany()
                        .HasForeignKey("TargetUserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole")
                        .WithMany("Claims")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.ApplicationUser")
                        .WithMany("Claims")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Forum3.Models.DataModels.ApplicationUser")
                        .WithMany("Logins")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Forum3.Models.DataModels.ApplicationUser")
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
