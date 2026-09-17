# Databricks notebook source
# ============================================================================
# Configuration
# ============================================================================
 
from pyspark.sql.functions import *
from pyspark.sql import SparkSession
from datetime import datetime
import traceback
 
# Catalog and schema configuration
CATALOG = "srms_catalog"
SCHEMA = "srms"
 
# Source tables
SERVICE_REQUESTS_TABLE = f"{CATALOG}.{SCHEMA}.servicerequests"
SUPPORT_ENGINEERS_TABLE = f"{CATALOG}.{SCHEMA}.supportengineers"
PRIORITIES_TABLE = f"{CATALOG}.{SCHEMA}.priorities"
CATEGORIES_TABLE = f"{CATALOG}.{SCHEMA}.categories"
TICKET_ASSIGNMENTS_TABLE = f"{CATALOG}.{SCHEMA}.ticketassignments"
SLA_CONFIGURATIONS_TABLE = f"{CATALOG}.{SCHEMA}.slaconfigurations"
 
# Output table
STATS_TABLE = f"{CATALOG}.{SCHEMA}.dashboard_stats"
 
# Run metadata
RUN_TIMESTAMP = datetime.now()
 
print("✅ Configuration loaded successfully")
print(f"   Run Timestamp: {RUN_TIMESTAMP}")
print(f"   Output Table: {STATS_TABLE}")
 

# COMMAND ----------

# ============================================================================
# Helper Functions
# ============================================================================
 
def create_stats_table_if_not_exists():
    """
    Create the dashboard_stats table if it doesn't exist.
    Uses Delta format for ACID transactions and time travel.
    """
    create_sql = f"""
    CREATE TABLE IF NOT EXISTS {STATS_TABLE} (
        stat_id BIGINT GENERATED ALWAYS AS IDENTITY,
        stat_name STRING NOT NULL COMMENT 'Name of the statistic (e.g., total_requests, open_tickets)',
        stat_value BIGINT NOT NULL COMMENT 'Numeric value of the statistic',
        stat_category STRING COMMENT 'Optional category/priority breakdown (e.g., Hardware, Critical)',
        run_timestamp TIMESTAMP NOT NULL COMMENT 'When this stat was generated',
        created_at TIMESTAMP COMMENT 'Record creation timestamp'
    )
    USING DELTA
    COMMENT 'Dashboard statistics for IT Service Request Management System'
    """
   
    spark.sql(create_sql)
    print(f"✅ Stats table ready: {STATS_TABLE}")
 
def create_stat_record(stat_name, stat_value, stat_category=None):
    """
    Create a single stat record as a DataFrame row.
   
    Args:
        stat_name: Name of the statistic
        stat_value: Numeric value
        stat_category: Optional category/priority name
   
    Returns:
        List containing the stat record
    """
    return [(stat_name, stat_value, stat_category, RUN_TIMESTAMP)]
 
def save_stats_to_delta(stats_df):
    """
    Save statistics DataFrame to Delta table.
    Overwrites existing stats to keep only the latest snapshot.
   
    Args:
        stats_df: DataFrame with columns (stat_name, stat_value, stat_category, run_timestamp)
    """
    # Add created_at timestamp
    stats_df = stats_df.withColumn("created_at", current_timestamp())
   
    stats_df.write \
        .format("delta") \
        .mode("overwrite") \
        .saveAsTable(STATS_TABLE)
   
    record_count = stats_df.count()
    print(f"✅ Saved {record_count} stat records to {STATS_TABLE}")
 
print("✅ Helper functions defined")
 

# COMMAND ----------

# ============================================================================
# Calculate Dashboard Statistics
# ============================================================================
 
print("\n" + "="*80)
print("STARTING STATS GENERATION")
print("="*80)
 
try:
    # Ensure stats table exists
    create_stats_table_if_not_exists()
   
    # Load source data
    print("\n📂 Loading source tables...")
    df_requests = spark.table(SERVICE_REQUESTS_TABLE)
    df_assignments = spark.table(TICKET_ASSIGNMENTS_TABLE)
   
    # Note: .cache() removed for serverless compatibility
    total_records = df_requests.count()
    print(f"   ✓ Loaded {total_records:,} service requests")
   
    # Initialize stats collection
    all_stats = []
   
    # -------------------------------------------------------------------------
    # 1. TOTAL REQUESTS
    # -------------------------------------------------------------------------
    total_requests = df_requests.count()
    all_stats.extend(create_stat_record("total_requests", total_requests))
    print(f"   ✓ Total Requests: {total_requests:,}")
   
    # -------------------------------------------------------------------------
    # 2. TICKETS BY STATUS (Open, In-Progress, Resolved, Closed)
    # -------------------------------------------------------------------------
    print("\n📊 Calculating status breakdowns...")
   
    # New = Open
    open_count = df_requests.filter(col("Status") == "New").count()
    all_stats.extend(create_stat_record("open_tickets", open_count))
    print(f"   ✓ Open (New): {open_count:,}")
   
    # Assigned + In Progress = In-Progress
    inprogress_count = df_requests.filter(
        col("Status").isin(["Assigned", "In Progress"])
    ).count()
    all_stats.extend(create_stat_record("inprogress_tickets", inprogress_count))
    print(f"   ✓ In-Progress (Assigned + In Progress): {inprogress_count:,}")
   
    # Resolved
    resolved_count = df_requests.filter(col("Status") == "Resolved").count()
    all_stats.extend(create_stat_record("resolved_tickets", resolved_count))
    print(f"   ✓ Resolved: {resolved_count:,}")
   
    # Closed
    closed_count = df_requests.filter(col("Status") == "Closed").count()
    all_stats.extend(create_stat_record("closed_tickets", closed_count))
    print(f"   ✓ Closed: {closed_count:,}")
   
    # -------------------------------------------------------------------------
    # 3. SLA BREACHED TICKETS
    # -------------------------------------------------------------------------
    print("\n⏰ Calculating SLA breaches...")
   
    # Load SLA configurations
    df_sla = spark.table(SLA_CONFIGURATIONS_TABLE)
   
    # Join requests with SLA configs
    df_with_sla = df_requests.alias("sr") \
        .join(df_sla.alias("sla"), col("sr.PriorityId") == col("sla.PriorityId"), "left")
   
    # Calculate time elapsed (in minutes) from created_at to now or updated_at
    # Cast string timestamps to timestamp type first
    df_with_sla = df_with_sla.withColumn(
        "elapsed_minutes",
        (unix_timestamp(
            coalesce(to_timestamp(col("sr.UpdatedAt")), current_timestamp())
        ) - unix_timestamp(to_timestamp(col("sr.CreatedAt")))) / 60
    )
   
    # SLA breach: elapsed time > resolution target
    # Only count non-closed tickets as breached
    sla_breached_count = df_with_sla.filter(
        (col("elapsed_minutes") > col("sla.ResolutionTargetMinutes")) &
        (col("sr.Status") != "Closed")
    ).count()
   
    all_stats.extend(create_stat_record("sla_breached_tickets", sla_breached_count))
    print(f"   ✓ SLA Breached: {sla_breached_count:,}")
   
    # -------------------------------------------------------------------------
    # 4. TICKETS BY CATEGORY
    # -------------------------------------------------------------------------
    print("\n📋 Calculating category breakdowns...")
   
    df_categories = spark.table(CATEGORIES_TABLE)
   
    tickets_by_category = df_requests.alias("sr") \
        .join(df_categories.alias("cat"), col("sr.CategoryId") == col("cat.Id"), "left") \
        .groupBy("cat.Name") \
        .agg(count("*").alias("ticket_count")) \
        .collect()
   
    for row in tickets_by_category:
        category_name = row["Name"] if row["Name"] else "Unknown"
        ticket_count = row["ticket_count"]
        all_stats.extend(create_stat_record("tickets_by_category", ticket_count, category_name))
        print(f"   ✓ {category_name}: {ticket_count:,}")
   
    # -------------------------------------------------------------------------
    # 5. TICKETS BY PRIORITY
    # -------------------------------------------------------------------------
    print("\n🔥 Calculating priority breakdowns...")
   
    df_priorities = spark.table(PRIORITIES_TABLE)
   
    tickets_by_priority = df_requests.alias("sr") \
        .join(df_priorities.alias("pri"), col("sr.PriorityId") == col("pri.Id"), "left") \
        .groupBy("pri.Name") \
        .agg(count("*").alias("ticket_count")) \
        .collect()
   
    for row in tickets_by_priority:
        priority_name = row["Name"] if row["Name"] else "Unknown"
        ticket_count = row["ticket_count"]
        all_stats.extend(create_stat_record("tickets_by_priority", ticket_count, priority_name))
        print(f"   ✓ {priority_name}: {ticket_count:,}")
   
    # -------------------------------------------------------------------------
    # 6. ENGINEER WORKLOAD (Current Active Assignments)
    # -------------------------------------------------------------------------
    print("\n👷 Calculating engineer workload...")
   
    df_engineers = spark.table(SUPPORT_ENGINEERS_TABLE)
   
    # Count current assignments per engineer (IsCurrent is bigint: 1=true, 0=false)
    engineer_workload = df_assignments.alias("ta") \
        .filter(col("ta.IsCurrent") == 1) \
        .join(df_engineers.alias("eng"), col("ta.EngineerId") == col("eng.Id"), "left") \
        .groupBy("eng.FullName") \
        .agg(count("*").alias("active_tickets")) \
        .collect()
   
    for row in engineer_workload:
        engineer_name = row["FullName"] if row["FullName"] else "Unknown"
        active_count = row["active_tickets"]
        all_stats.extend(create_stat_record("engineer_workload", active_count, engineer_name))
        print(f"   ✓ {engineer_name}: {active_count:,} active tickets")
   
    # -------------------------------------------------------------------------
    # SAVE ALL STATS TO DELTA TABLE
    # -------------------------------------------------------------------------
    print("\n💾 Saving statistics to Delta table...")
   
    # Create DataFrame from all collected stats
    stats_schema = "stat_name STRING, stat_value INT, stat_category STRING, run_timestamp TIMESTAMP"
    stats_df = spark.createDataFrame(all_stats, schema=stats_schema)
   
    # Save to Delta
    save_stats_to_delta(stats_df)
   
    print("\n" + "="*80)
    print("✅ STATS GENERATION COMPLETED SUCCESSFULLY")
    print("="*80)
    print(f"\n📊 Summary:")
    print(f"   Total stat records generated: {len(all_stats)}")
    print(f"   Run timestamp: {RUN_TIMESTAMP}")
    print(f"   Output table: {STATS_TABLE}")
    print(f"\n💡 Query stats with:")
    print(f"   SELECT * FROM {STATS_TABLE}")
   
except Exception as e:
    print("\n" + "="*80)
    print("❌ ERROR DURING STATS GENERATION")
    print("="*80)
    print(f"\nError: {str(e)}")
    print(f"\nFull traceback:")
    traceback.print_exc()
    raise
 

# COMMAND ----------

# ============================================================================
# Calculate Dashboard Statistics
# ============================================================================
 
print("\n" + "="*80)
print("STARTING STATS GENERATION")
print("="*80)
 
try:
    # Ensure stats table exists
    create_stats_table_if_not_exists()
   
    # Load source data
    print("\n📂 Loading source tables...")
    df_requests = spark.table(SERVICE_REQUESTS_TABLE)
    df_assignments = spark.table(TICKET_ASSIGNMENTS_TABLE)
   
    # Note: .cache() removed for serverless compatibility
    total_records = df_requests.count()
    print(f"   ✓ Loaded {total_records:,} service requests")
   
    # Initialize stats collection
    all_stats = []
   
    # -------------------------------------------------------------------------
    # 1. TOTAL REQUESTS
    # -------------------------------------------------------------------------
    total_requests = df_requests.count()
    all_stats.extend(create_stat_record("total_requests", total_requests))
    print(f"   ✓ Total Requests: {total_requests:,}")
   
    # -------------------------------------------------------------------------
    # 2. TICKETS BY STATUS (Open, In-Progress, Resolved, Closed)
    # -------------------------------------------------------------------------
    print("\n📊 Calculating status breakdowns...")
   
    # New = Open
    open_count = df_requests.filter(col("Status") == "New").count()
    all_stats.extend(create_stat_record("open_tickets", open_count))
    print(f"   ✓ Open (New): {open_count:,}")
   
    # Assigned + In Progress = In-Progress
    inprogress_count = df_requests.filter(
        col("Status").isin(["Assigned", "In Progress"])
    ).count()
    all_stats.extend(create_stat_record("inprogress_tickets", inprogress_count))
    print(f"   ✓ In-Progress (Assigned + In Progress): {inprogress_count:,}")
   
    # Resolved
    resolved_count = df_requests.filter(col("Status") == "Resolved").count()
    all_stats.extend(create_stat_record("resolved_tickets", resolved_count))
    print(f"   ✓ Resolved: {resolved_count:,}")
   
    # Closed
    closed_count = df_requests.filter(col("Status") == "Closed").count()
    all_stats.extend(create_stat_record("closed_tickets", closed_count))
    print(f"   ✓ Closed: {closed_count:,}")
   
    # -------------------------------------------------------------------------
    # 3. SLA BREACHED TICKETS
    # -------------------------------------------------------------------------
    print("\n⏰ Calculating SLA breaches...")
   
    # Load SLA configurations
    df_sla = spark.table(SLA_CONFIGURATIONS_TABLE)
   
    # Join requests with SLA configs
    df_with_sla = df_requests.alias("sr") \
        .join(df_sla.alias("sla"), col("sr.PriorityId") == col("sla.PriorityId"), "left")
   
    # Calculate time elapsed (in minutes) from created_at to now or updated_at
    # Cast string timestamps to timestamp type first
    df_with_sla = df_with_sla.withColumn(
        "elapsed_minutes",
        (unix_timestamp(
            coalesce(to_timestamp(col("sr.UpdatedAt")), current_timestamp())
        ) - unix_timestamp(to_timestamp(col("sr.CreatedAt")))) / 60
    )
   
    # SLA breach: elapsed time > resolution target
    # Only count non-closed tickets as breached
    sla_breached_count = df_with_sla.filter(
        (col("elapsed_minutes") > col("sla.ResolutionTargetMinutes")) &
        (col("sr.Status") != "Closed")
    ).count()
   
    all_stats.extend(create_stat_record("sla_breached_tickets", sla_breached_count))
    print(f"   ✓ SLA Breached: {sla_breached_count:,}")
   
    # -------------------------------------------------------------------------
    # 4. TICKETS BY CATEGORY
    # -------------------------------------------------------------------------
    print("\n📋 Calculating category breakdowns...")
   
    df_categories = spark.table(CATEGORIES_TABLE)
   
    tickets_by_category = df_requests.alias("sr") \
        .join(df_categories.alias("cat"), col("sr.CategoryId") == col("cat.Id"), "left") \
        .groupBy("cat.Name") \
        .agg(count("*").alias("ticket_count")) \
        .collect()
   
    for row in tickets_by_category:
        category_name = row["Name"] if row["Name"] else "Unknown"
        ticket_count = row["ticket_count"]
        all_stats.extend(create_stat_record("tickets_by_category", ticket_count, category_name))
        print(f"   ✓ {category_name}: {ticket_count:,}")
   
    # -------------------------------------------------------------------------
    # 5. TICKETS BY PRIORITY
    # -------------------------------------------------------------------------
    print("\n🔥 Calculating priority breakdowns...")
   
    df_priorities = spark.table(PRIORITIES_TABLE)
   
    tickets_by_priority = df_requests.alias("sr") \
        .join(df_priorities.alias("pri"), col("sr.PriorityId") == col("pri.Id"), "left") \
        .groupBy("pri.Name") \
        .agg(count("*").alias("ticket_count")) \
        .collect()
   
    for row in tickets_by_priority:
        priority_name = row["Name"] if row["Name"] else "Unknown"
        ticket_count = row["ticket_count"]
        all_stats.extend(create_stat_record("tickets_by_priority", ticket_count, priority_name))
        print(f"   ✓ {priority_name}: {ticket_count:,}")
   
    # -------------------------------------------------------------------------
    # 6. ENGINEER WORKLOAD (Current Active Assignments)
    # -------------------------------------------------------------------------
    print("\n👷 Calculating engineer workload...")
   
    df_engineers = spark.table(SUPPORT_ENGINEERS_TABLE)
   
    # Count current assignments per engineer (IsCurrent is bigint: 1=true, 0=false)
    engineer_workload = df_assignments.alias("ta") \
        .filter(col("ta.IsCurrent") == 1) \
        .join(df_engineers.alias("eng"), col("ta.EngineerId") == col("eng.Id"), "left") \
        .groupBy("eng.FullName") \
        .agg(count("*").alias("active_tickets")) \
        .collect()
   
    for row in engineer_workload:
        engineer_name = row["FullName"] if row["FullName"] else "Unknown"
        active_count = row["active_tickets"]
        all_stats.extend(create_stat_record("engineer_workload", active_count, engineer_name))
        print(f"   ✓ {engineer_name}: {active_count:,} active tickets")
   
    # -------------------------------------------------------------------------
    # SAVE ALL STATS TO DELTA TABLE
    # -------------------------------------------------------------------------
    print("\n💾 Saving statistics to Delta table...")
   
    # Create DataFrame from all collected stats
    stats_schema = "stat_name STRING, stat_value INT, stat_category STRING, run_timestamp TIMESTAMP"
    stats_df = spark.createDataFrame(all_stats, schema=stats_schema)
   
    # Save to Delta
    save_stats_to_delta(stats_df)
   
    print("\n" + "="*80)
    print("✅ STATS GENERATION COMPLETED SUCCESSFULLY")
    print("="*80)
    print(f"\n📊 Summary:")
    print(f"   Total stat records generated: {len(all_stats)}")
    print(f"   Run timestamp: {RUN_TIMESTAMP}")
    print(f"   Output table: {STATS_TABLE}")
    print(f"\n💡 Query stats with:")
    print(f"   SELECT * FROM {STATS_TABLE}")
   
except Exception as e:
    print("\n" + "="*80)
    print("❌ ERROR DURING STATS GENERATION")
    print("="*80)
    print(f"\nError: {str(e)}")
    print(f"\nFull traceback:")
    traceback.print_exc()
    raise
 

# COMMAND ----------

# MAGIC %sql
# MAGIC -- ============================================================================
# MAGIC -- Verify Generated Statistics
# MAGIC -- ============================================================================
# MAGIC -- View the current stats snapshot (table is overwritten each run)
# MAGIC  
# MAGIC SELECT
# MAGIC     stat_name,
# MAGIC     stat_value,
# MAGIC     stat_category,
# MAGIC     run_timestamp
# MAGIC FROM srms_catalog.srms.dashboard_stats
# MAGIC ORDER BY
# MAGIC     CASE stat_name
# MAGIC         WHEN 'total_requests' THEN 1
# MAGIC         WHEN 'open_tickets' THEN 2
# MAGIC         WHEN 'inprogress_tickets' THEN 3
# MAGIC         WHEN 'resolved_tickets' THEN 4
# MAGIC         WHEN 'closed_tickets' THEN 5
# MAGIC         WHEN 'sla_breached_tickets' THEN 6
# MAGIC         WHEN 'tickets_by_category' THEN 7
# MAGIC         WHEN 'tickets_by_priority' THEN 8
# MAGIC         WHEN 'engineer_workload' THEN 9
# MAGIC     END,
# MAGIC     stat_category
# MAGIC  

# COMMAND ----------

