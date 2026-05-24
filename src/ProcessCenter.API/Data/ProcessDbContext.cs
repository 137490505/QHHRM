using Microsoft.EntityFrameworkCore;
using ProcessCenter.API.Models;

namespace ProcessCenter.API.Data;

public class ProcessDbContext : DbContext
{
    public ProcessDbContext(DbContextOptions<ProcessDbContext> options) : base(options) { }

    public DbSet<ProcessDefinitionEntity> ProcessDefinitions => Set<ProcessDefinitionEntity>();
    public DbSet<ProcessDefinitionNodeEntity> ProcessDefinitionNodes => Set<ProcessDefinitionNodeEntity>();
    public DbSet<ProcessNodeConditionEntity> ProcessNodeConditions => Set<ProcessNodeConditionEntity>();
    public DbSet<ProcessInstanceEntity> ProcessInstances => Set<ProcessInstanceEntity>();
    public DbSet<ProcessNodeHistoryEntity> ProcessNodeHistories => Set<ProcessNodeHistoryEntity>();
    public DbSet<ProcessedEventEntity> ProcessedEvents => Set<ProcessedEventEntity>();
    public DbSet<ProcessCallbackLogEntity> ProcessCallbackLogs => Set<ProcessCallbackLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProcessDefinitionEntity>(entity =>
        {
            entity.ToTable("proc_definition");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProcessName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.BusinessType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CallbackConfig).HasMaxLength(2000);
            entity.Property(e => e.CreatedTime).HasColumnType("datetime(6)");
            entity.Property(e => e.UpdatedTime).HasColumnType("datetime(6)");
            
            entity.HasIndex(e => new { e.ProcessCode, e.VersionNo }).IsUnique();
        });

        modelBuilder.Entity<ProcessDefinitionNodeEntity>(entity =>
        {
            entity.ToTable("proc_definition_node");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NodeId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.NodeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NodeType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.AssigneeType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.AssigneeId).HasMaxLength(500).IsRequired();
            entity.Property(e => e.AssigneeName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MultiPersonType).HasMaxLength(32);
            entity.Property(e => e.NextNodeId).HasMaxLength(128);
            
            entity.HasOne<ProcessDefinitionEntity>()
                .WithMany(p => p.Nodes)
                .HasForeignKey(e => e.ProcessDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessNodeConditionEntity>(entity =>
        {
            entity.ToTable("proc_node_condition");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConditionExpression).HasMaxLength(500);
            entity.Property(e => e.TargetNodeId).HasMaxLength(128).IsRequired();
            
            entity.HasOne<ProcessDefinitionNodeEntity>()
                .WithMany(n => n.Conditions)
                .HasForeignKey(e => e.ProcessDefinitionNodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessInstanceEntity>(entity =>
        {
            entity.ToTable("proc_instance");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InstanceNo).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProcessCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.BusinessSystem).HasMaxLength(64).IsRequired();
            entity.Property(e => e.BusinessType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.BusinessId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.CurrentNodeId).HasMaxLength(128);
            entity.Property(e => e.CurrentNodeName).HasMaxLength(200);
            entity.Property(e => e.CurrentAssigneeId).HasMaxLength(500);
            entity.Property(e => e.CurrentAssigneeName).HasMaxLength(500);
            entity.Property(e => e.StarterId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.StarterName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.StartedTime).HasColumnType("datetime(6)");
            entity.Property(e => e.FinishedTime).HasColumnType("datetime(6)");
            
            entity.HasIndex(e => e.InstanceNo).IsUnique();
            entity.HasIndex(e => new { e.BusinessId, e.BusinessType });
            entity.HasIndex(e => e.ProcessCode);
        });

        modelBuilder.Entity<ProcessNodeHistoryEntity>(entity =>
        {
            entity.ToTable("proc_node_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NodeId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.NodeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NodeType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.HandlerId).HasMaxLength(128);
            entity.Property(e => e.HandlerName).HasMaxLength(128);
            entity.Property(e => e.Action).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ActionResult).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ArrivedTime).HasColumnType("datetime(6)");
            entity.Property(e => e.HandledTime).HasColumnType("datetime(6)");
            
            entity.HasOne(e => e.ProcessInstance)
                .WithMany(p => p.Histories)
                .HasForeignKey(e => e.ProcessInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedEventEntity>(entity =>
        {
            entity.ToTable("proc_processed_event");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(128);
            entity.Property(e => e.ProcessedTime).HasColumnType("datetime(6)");
        });

        modelBuilder.Entity<ProcessCallbackLogEntity>(entity =>
        {
            entity.ToTable("proc_callback_log");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CallbackUrl).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(32).IsRequired();
            entity.Property(e => e.StatusCode);
            entity.Property(e => e.CreatedTime).HasColumnType("datetime(6)");
            
            entity.HasOne(e => e.ProcessInstance)
                .WithMany()
                .HasForeignKey(e => e.ProcessInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
