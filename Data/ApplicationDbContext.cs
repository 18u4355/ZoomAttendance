using Microsoft.EntityFrameworkCore;
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
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
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
        }
    }
}
