using Microsoft.EntityFrameworkCore;
using TodoCenter.API.Models;

namespace TodoCenter.API.Data;

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<TodoTaskEntity> TodoTasks => Set<TodoTaskEntity>();
    public DbSet<AgentSettingEntity> AgentSettings => Set<AgentSettingEntity>();
    public DbSet<TodoTaskLogEntity> TodoTaskLogs => Set<TodoTaskLogEntity>();
    public DbSet<TodoTaskNotifyLogEntity> TodoTaskNotifyLogs => Set<TodoTaskNotifyLogEntity>();
    public DbSet<ProcessedEventEntity> ProcessedEvents => Set<ProcessedEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TodoTaskEntity>(entity =>
        {
            entity.ToTable("todo_task");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskNo).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TaskTypeCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.BusinessSystem).HasMaxLength(64).IsRequired();
            entity.Property(e => e.BusinessType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.BusinessId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ProcessInstanceId).HasMaxLength(128);
            entity.Property(e => e.ProcessNodeId).HasMaxLength(128);
            entity.Property(e => e.AssigneeId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AssigneeName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TaskCategory).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedTime).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime(6)");
            
            entity.HasIndex(e => e.TaskNo).IsUnique();
            entity.HasIndex(e => new { e.AssigneeId, e.Status });
            entity.HasIndex(e => new { e.BusinessId, e.BusinessType });
            entity.HasIndex(e => e.ProcessInstanceId);
        });

        modelBuilder.Entity<AgentSettingEntity>(entity =>
        {
            entity.ToTable("todo_agent_setting");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrincipalUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AgentUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ScopeType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.TaskTypeCode).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.StartTime).HasColumnType("datetime(6)");
            entity.Property(e => e.EndTime).HasColumnType("datetime(6)");
            
            entity.HasIndex(e => e.PrincipalUserId);
        });

        modelBuilder.Entity<TodoTaskLogEntity>(entity =>
        {
            entity.ToTable("todo_task_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(64).IsRequired();
            entity.Property(e => e.OperatorId).HasMaxLength(128);
            entity.Property(e => e.OperatorName).HasMaxLength(128);
            entity.Property(e => e.ActionResult).HasMaxLength(64);
            entity.Property(e => e.BeforeStatus).HasMaxLength(32);
            entity.Property(e => e.AfterStatus).HasMaxLength(32);
            entity.Property(e => e.CreatedTime).HasColumnType("datetime(6)");
            
            entity.HasOne(e => e.Task)
                .WithMany(t => t.Logs)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TodoTaskNotifyLogEntity>(entity =>
        {
            entity.ToTable("todo_task_notify_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NotifyType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ReceiverId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ReceiverName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Channel).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.SentTime).HasColumnType("datetime(6)");
            entity.Property(e => e.CreatedTime).HasColumnType("datetime(6)");
            
            entity.HasOne(e => e.Task)
                .WithMany(t => t.NotifyLogs)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedEventEntity>(entity =>
        {
            entity.ToTable("todo_processed_event");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(128);
            entity.Property(e => e.ProcessedTime).HasColumnType("datetime(6)");
        });
    }
}
