using Microsoft.EntityFrameworkCore;
using ZoomAttendance.Entities;
using ZoomAttendance.Models.Entities;

namespace ZoomAttendance.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Meeting> Meetings => Set<Meeting>();
        public DbSet<MeetingAttendance> Attendance => Set<MeetingAttendance>();
        public DbSet<Staff> Staff { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //USERS
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.StaffName).HasColumnName("staff_name").IsRequired();
                entity.Property(e => e.Email).HasColumnName("email").IsRequired();
                entity.Property(e => e.Role).HasColumnName("role").IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            });

            //MEETINGS
            modelBuilder.Entity<Meeting>(entity =>
            {
                entity.ToTable("meetings");
                entity.HasKey(e => e.MeetingId);
                entity.Property(e => e.MeetingId).HasColumnName("meeting_id");
                entity.Property(e => e.Title).HasColumnName("title").IsRequired();
                entity.Property(e => e.ZoomUrl).HasColumnName("zoom_url").IsRequired();
                entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
                entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.IsClosed).HasColumnName("IsClosed");

            }); 

            // ATTENDANCE
            modelBuilder.Entity<MeetingAttendance>(entity =>
            {
                entity.ToTable("attendance");
                entity.HasKey(e => e.AttendanceId);
                entity.Property(e => e.AttendanceId).HasColumnName("attendance_id");

                entity.Property(e => e.MeetingId)
                      .HasColumnName("meeting_id");


                entity.Property(e => e.StaffEmail)
                      .HasColumnName("staff_email");


                entity.Property(e => e.StaffName)
                      .HasColumnName("staff_name");


                entity.Property(e => e.JoinToken)
                      .HasColumnName("join_token");
                     

                entity.Property(e => e.JoinTime)
                      .HasColumnName("join_time");

                entity.Property(e => e.ConfirmAttendance)
                      .HasColumnName("confirm_attendance");

                entity.Property(e => e.ConfirmationTime)
                      .HasColumnName("confirmation_time");

                entity.Property(e => e.ConfirmationToken)
                      .HasColumnName("confirmation_token");

                entity.Property(e => e.ConfirmationExpiresAt)
                      .HasColumnName("confirmation_expires_at");

               entity.Property(e => e.ClosedAt)
                      .HasColumnName("closed_at");
                entity.Property(e => e.CreatedAt)
                     .HasColumnName("created_at");


                entity.HasOne<Meeting>()
                      .WithMany()
                      .HasForeignKey(e => e.MeetingId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Meeting)
      .WithMany()
      .HasForeignKey(a => a.MeetingId)
      .OnDelete(DeleteBehavior.Cascade);

            });
        
        // ── Staff ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Staff>()
                .HasIndex(s => s.BarcodeToken)
                .IsUnique();

        modelBuilder.Entity<Staff>()
                .HasIndex(s => s.Email)
                .IsUnique();

        modelBuilder.Entity<Staff>()
                .HasIndex(s => s.FullName); // supports name-based dropdown lookup

        // ── Meeting ───────────────────────────────────────────────────────
        // Existing table — EF reads only, no migrations run against this table
        modelBuilder.Entity<Meeting>()
                .HasKey(m => m.MeetingId);

        // ── AttendanceLog ─────────────────────────────────────────────────
        modelBuilder.Entity<AttendanceLog>()
                .HasIndex(a => new { a.StaffId, a.MeetingId
    })
                .IsUnique(); // prevents double scan

    modelBuilder.Entity<AttendanceLog>()
                .HasOne(a => a.Staff)
                .WithMany(s => s.AttendanceLogs)
                .HasForeignKey(a => a.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<AttendanceLog>()
                .HasOne(a => a.Meeting)
                .WithMany(m => m.AttendanceLogs)
                .HasForeignKey(a => a.MeetingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}