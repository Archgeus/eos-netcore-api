using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EOS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("EOS.Models.Token", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime(6)");

                b.Property<string>("TokenValue")
                    .IsRequired()
                    .HasColumnType("longtext");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.Property<string>("Username")
                    .IsRequired()
                    .HasColumnType("longtext");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("Tokens");
            });

            modelBuilder.Entity("EOS.Models.PurchaseTransaction", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime(6)");

                b.Property<int>("ItemId")
                    .HasColumnType("int");

                b.Property<string>("ItemName")
                    .IsRequired()
                    .HasColumnType("longtext");

                b.Property<string>("RequestId")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<int>("TransactionId")
                    .HasColumnType("int");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.Property<string>("UserIp")
                    .HasColumnType("varchar(45)");

                b.Property<int>("Price")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("PurchaseTransactions");
            });

            modelBuilder.Entity("EOS.Models.FundsTransaction", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<int>("Amount")
                    .HasColumnType("int");

               

            modelBuilder.Entity("EOS.Models.FundsTransaction", b =>
            {
                b.HasOne("EOS.Models.User", "User")
                    .WithMany("FundsTransactions")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            }); b.Property<int>("BalanceAfter")
                    .HasColumnType("int");

                b.Property<int>("BalanceBefore")
                    .HasColumnType("int");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime(6)");

                b.Property<string>("Operation")
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                b.Property<string>("TransactionId")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<int>("UserId")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("UserId");

                b.ToTable("FundsTransactions");
            });

            modelBuilder.Entity("EOS.Models.PurchaseTransaction", b =>
            {
                b.HasOne("EOS.Models.User", "User")
                    .WithMany("PurchaseTransactions")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("EOS.Models.User", b =>
            {
                b.Navigation("PurchaseTransactions");
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<int>("Balance")
                    .HasColumnType("int");

                b.Property<DateTime>("JoinDate")
                    .HasColumnType("datetime(6)");

                b.Property<DateTime?>("LastLogin")
                    .HasColumnType("datetime(6)");

                b.Property<string>("Password")
                    .IsRequired()
                    .HasColumnType("longtext");

                b.Property<string>("Username")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.HasKey("Id");

                b.HasIndex("Username")
                    .IsUnique();

                b.ToTable("Users");
            });

            modelBuilder.Entity("EOS.Models.Token", b =>
            {
                b.HasOne("EOS.Models.User", "User")
                    .WithMany("Tokens")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

            modelBuilder.Entity("EOS.Models.User", b =>
            {
                b.Navigation("Tokens");
                b.Navigation("PurchaseTransactions");
                b.Navigation("FundsTransactions");
            });
#pragma warning restore 612, 618
        }
    }
}
