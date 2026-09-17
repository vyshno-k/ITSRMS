# Databricks notebook source
# ============================================================================
# STEP 0: AUTO-DETECT SQL DUMP FILES (EVENT-DRIVEN)
# ============================================================================
 
print("\n" + "="*80)
print("STEP 0: Auto-Detecting SQL Dump Files")
print("="*80)
 
# Define paths
MAIN_DIR = "/Volumes/srms_catalog/srms/new_volume"
ARCHIVE_DIR = f"{MAIN_DIR}/archive"
FAILED_DIR = f"{MAIN_DIR}/failed"
 
# Create directories if needed
for dir_path in [ARCHIVE_DIR, FAILED_DIR]:
    try:
        dbutils.fs.mkdirs(dir_path)
    except:
        pass
 
# Scan for SQL files
print(f"\n🔍 Scanning: {MAIN_DIR}")
 
all_files = dbutils.fs.ls(MAIN_DIR)
sql_files = [f for f in all_files if f.name.endswith('.sql') and not f.isDir()]
 
if not sql_files:
    print("\n✅ No new SQL files to process")
    print("\n📌 Upload ITServiceRequest.db.sql to trigger processing")
    dbutils.notebook.exit("No files to process")
 
print(f"\n📥 Found {len(sql_files)} SQL file(s):\n")
for f in sql_files:
    print(f"   📄 {f.name}  ({f.size/1024:.1f} KB)")
 
# Select file to process
FILE_TO_PROCESS = None
for f in sql_files:
    if f.name == "ITServiceRequest.db.sql":
        FILE_TO_PROCESS = f
        break
 
if not FILE_TO_PROCESS:
    FILE_TO_PROCESS = sql_files[0]
 
# Convert dbfs:// path to /dbfs/ path for Python file operations
SQL_DUMP_FILE = FILE_TO_PROCESS.path.replace('dbfs:', '/dbfs')
PROCESSED_FILENAME = FILE_TO_PROCESS.name
 
# Keep original path for dbutils operations
SQL_DUMP_FILE_DBFS = FILE_TO_PROCESS.path  # For dbutils.fs operations
 
print(f"\n➡️  Processing: {PROCESSED_FILENAME}")
print(f"   Path: {SQL_DUMP_FILE}")
print("\n✅ STEP 0 COMPLETED - Proceeding with sync...\n")
 

# COMMAND ----------

# ============================================================================
# CONFIGURATION
# ============================================================================
 
import sqlite3
import pandas as pd
from pyspark.sql import functions as F
from pyspark.sql.types import *
from datetime import datetime
 
# File paths (SQL_DUMP_FILE is set by Step 0 - Auto-Detect)
# Do not override SQL_DUMP_FILE here - it comes from Step 0
# On serverless, use /Workspace path (not /tmp)
SQLITE_DB_FILE = "/Workspace/.tmp/ITServiceRequest.db"
 
# Delta table configuration
CATALOG = "srms_catalog"
SCHEMA = "srms"
 
# Tables to sync
TABLES_TO_SYNC = [
    "Employees",
    "ServiceRequests",
    "Priorities",
    "Categories",
    "ServiceTypes",
    "SlaConfigurations",
    "TicketAssignments",
    "Resolutions",
    "ServiceRequestHistories",
    "SlaPauseStatuses",
    "SupportEngineers",
    "TicketComments"
]
 
print("✅ Configuration loaded")
try:
    print(f"   SQL Dump: {SQL_DUMP_FILE}")
except NameError:
    print("   SQL Dump: Will be set by Step 0 (Auto-Detect)")
print(f"   Target Catalog: {CATALOG}.{SCHEMA}")
print(f"   Tables to sync: {len(TABLES_TO_SYNC)}")
 

# COMMAND ----------

# ============================================================================
# STEP 1: CREATE SQLITE DATABASE FROM SQL DUMP
# ============================================================================
 
import os
 
print("\n" + "="*80)
print("STEP 1: Creating SQLite Database from SQL Dump")
print("="*80)
 
# Read SQL dump file directly from Volume (no dbutils needed on serverless!)
volume_path = SQL_DUMP_FILE_DBFS.replace('dbfs:', '')
print(f"✅ Reading SQL dump file from Volume: {volume_path}")
 
# Unity Catalog Volumes are directly accessible via Python open()
with open(volume_path, 'r', encoding='utf-8') as f:
    sql_dump = f.read()
 
print(f"✅ Read SQL dump ({len(sql_dump):,} characters)")
 
# Create directory for SQLite DB if needed
sqlite_dir = os.path.dirname(SQLITE_DB_FILE)
if not os.path.exists(sqlite_dir):
    os.makedirs(sqlite_dir)
    print(f"✅ Created directory: {sqlite_dir}")
 
# Remove existing database to start fresh
if os.path.exists(SQLITE_DB_FILE):
    os.remove(SQLITE_DB_FILE)
    print(f"🗑️  Removed existing database")
 
# Create new SQLite database from dump
conn = sqlite3.connect(SQLITE_DB_FILE)
cursor = conn.cursor()
 
try:
    cursor.executescript(sql_dump)
    conn.commit()
    print(f"✅ Created SQLite database: {SQLITE_DB_FILE}")
except Exception as e:
    print(f"❌ Error creating database: {str(e)}")
    raise
finally:
    cursor.close()
 
print("\n✅ STEP 1 COMPLETED: SQLite database ready")
 

# COMMAND ----------



# COMMAND ----------

# DBTITLE 1,STEP 2B: Detect Primary Keys
# ============================================================================
# STEP 2B: DETECT PRIMARY KEYS (FOR SCD TYPE 1 MERGE)
# ============================================================================
 
print("\n" + "="*80)
print("STEP 2B: Detecting Primary Keys")
print("="*80)
 
# Re-open connection to SQLite database
conn = sqlite3.connect(SQLITE_DB_FILE)
cursor = conn.cursor()

# Get list of tables from SQLite database
cursor.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
tables = cursor.fetchall()
print(f"📋 Found {len(tables)} tables in SQLite database\n")

PRIMARY_KEYS = {}
 
for table in tables:
    table_name = table[0]
   
    # Get table schema including primary key info
    cursor.execute(f"PRAGMA table_info({table_name})")
    columns = cursor.fetchall()
   
    # Find primary key columns (pk column = 1 or higher)
    pk_columns = [col[1] for col in columns if col[5] > 0]  # col[5] is pk flag
   
    if pk_columns:
        PRIMARY_KEYS[table_name] = pk_columns
        pk_list = ", ".join(pk_columns)
        print(f"   ✅ {table_name:<30} PK: {pk_list}")
    else:
        print(f"   ⚠️  {table_name:<30} No primary key (will use overwrite)")
 
print(f"\n📊 Found primary keys for {len(PRIMARY_KEYS)} tables")
print("\n✅ STEP 2B COMPLETED")
 
# Close connection
conn.close()
 

# COMMAND ----------

# ============================================================================
# STEP 3: HELPER FUNCTIONS
# ============================================================================
 
def read_sqlite_table_to_spark(table_name):
    """
    Read a table from SQLite database and convert to Spark DataFrame
   
    Args:
        table_name (str): Name of the table to read
   
    Returns:
        pyspark.sql.DataFrame: Spark DataFrame containing the table data
    """
    # Connect to SQLite
    conn = sqlite3.connect(SQLITE_DB_FILE)
   
    # Read into pandas
    query = f"SELECT * FROM {table_name}"
    pdf = pd.read_sql_query(query, conn)
    conn.close()
   
    # Convert pandas to Spark
    if len(pdf) == 0:
        print(f"   ⚠️  Warning: {table_name} is empty")
        return None
   
    sdf = spark.createDataFrame(pdf)
    return sdf
 
def convert_column_names_to_snake_case(df):
    """
    Keep column names as-is (no conversion)
    Preserves PascalCase column names from SQLite
    """
    # No conversion - keep original column names
    return df
 
def map_sqlite_to_delta_table_name(sqlite_table):
    """
    Map SQLite table name to Delta table name
    Example: ServiceRequests -> servicerequests (lowercase, no underscores)
    """
    return sqlite_table.lower()
 
print("✅ Helper functions defined")
 

# COMMAND ----------

# ============================================================================
# STEP 4: SYNC TABLES FROM SQLITE TO DELTA (SCD TYPE 1 MERGE)
# ============================================================================
 
print("\n" + "="*80)
print("STEP 4: Syncing Tables from SQLite to Delta (SCD Type 1)")
print("="*80)
 
sync_summary = []
 
for sqlite_table in TABLES_TO_SYNC:
    print(f"\n📥 Processing: {sqlite_table}")
   
    try:
        # Read from SQLite
        df = read_sqlite_table_to_spark(sqlite_table)
       
        if df is None:
            sync_summary.append({
                "table": sqlite_table,
                "status": "SKIPPED",
                "rows": 0,
                "reason": "Empty table"
            })
            continue
       
        # Convert column names to snake_case
        df = convert_column_names_to_snake_case(df)
       
        # Get Delta table name
        delta_table = map_sqlite_to_delta_table_name(sqlite_table)
        full_table_name = f"{CATALOG}.{SCHEMA}.{delta_table}"
       
        # Count rows
        row_count = df.count()
       
        # Get primary key for this table
        pk_columns = PRIMARY_KEYS.get(sqlite_table, [])
       
        if pk_columns:
            # SCD TYPE 1: Use MERGE INTO for incremental upsert
            print(f"   🔄 Using MERGE (SCD Type 1) with PK: {', '.join(pk_columns)}")
           
            # Keep PK columns as-is (PascalCase)
            pk_snake = pk_columns
           
            # Create temp view for source data
            temp_view = f"temp_{delta_table}_source"
            df.createOrReplaceTempView(temp_view)
           
            # Check if target table exists
            try:
                spark.sql(f"DESCRIBE TABLE {full_table_name}")
                table_exists = True
            except:
                table_exists = False
           
            if not table_exists:
                # First run: Create table with initial data
                df.write.format("delta").mode("overwrite").saveAsTable(full_table_name)
                print(f"   ✅ Created table and inserted {row_count} rows")
            else:
                # Build MERGE statement
                merge_condition = " AND ".join([
                    f"target.{pk} = source.{pk}" for pk in pk_snake
                ])
               
                # Get all columns except PK for UPDATE
                all_cols = [col for col in df.columns if col not in pk_snake]
                update_set = ", ".join([f"target.{col} = source.{col}" for col in all_cols])
                insert_cols = ", ".join(df.columns)
                insert_vals = ", ".join([f"source.{col}" for col in df.columns])
               
                merge_sql = f"""
                MERGE INTO {full_table_name} AS target
                USING {temp_view} AS source
                ON {merge_condition}
                WHEN MATCHED THEN
                    UPDATE SET {update_set}
                WHEN NOT MATCHED THEN
                    INSERT ({insert_cols})
                    VALUES ({insert_vals})
                """
               
                # Execute MERGE
                spark.sql(merge_sql)
                print(f"   ✅ Merged {row_count} rows (upsert completed)")
        else:
            # NO PRIMARY KEY: Fall back to overwrite mode
            print(f"   🔄 Using OVERWRITE (no primary key)")
            df.write \
                .format("delta") \
                .mode("overwrite") \
                .option("overwriteSchema", "true") \
                .saveAsTable(full_table_name)
            print(f"   ✅ Overwrote {row_count} rows")
       
        sync_summary.append({
            "table": sqlite_table,
            "status": "SUCCESS",
            "rows": row_count,
            "delta_table": full_table_name
        })
       
    except Exception as e:
        print(f"   ❌ Error syncing {sqlite_table}: {str(e)}")
        sync_summary.append({
            "table": sqlite_table,
            "status": "FAILED",
            "rows": 0,
            "reason": str(e)
        })
 
print("\n✅ STEP 4 COMPLETED: Table sync finished")
 

# COMMAND ----------

# ============================================================================
# STEP 5: DISPLAY SYNC SUMMARY
# ============================================================================
 
print("\n" + "="*80)
print("SYNC SUMMARY")
print("="*80)
 
summary_df = spark.createDataFrame(sync_summary)
display(summary_df)
 
# Count successes and failures
success_count = sum(1 for s in sync_summary if s["status"] == "SUCCESS")
failed_count = sum(1 for s in sync_summary if s["status"] == "FAILED")
skipped_count = sum(1 for s in sync_summary if s["status"] == "SKIPPED")
total_rows = sum(s["rows"] for s in sync_summary)
 
print(f"\n📊 SYNC STATISTICS:")
print(f"   ✅ Successful: {success_count}")
print(f"   ❌ Failed: {failed_count}")
print(f"   ⚠️  Skipped: {skipped_count}")
print(f"   📦 Total rows synced: {total_rows:,}")
 
if failed_count > 0:
    print("\n⚠️  WARNING: Some tables failed to sync. Review errors above.")
else:
    print("\n✅ ALL TABLES SYNCED SUCCESSFULLY!")
 

# COMMAND ----------

# ============================================================================
# STEP 7: DATA QUALITY CHECKS
# ============================================================================
 
print("\n" + "="*80)
print("STEP 7: Data Quality Checks")
print("="*80)
 
# Check 1: Verify all expected tables exist
print("\n📋 Check 1: Table Existence")
tables_in_catalog = spark.sql(f"SHOW TABLES IN {CATALOG}.{SCHEMA}").collect()
table_names = [row.tableName for row in tables_in_catalog]
 
for sqlite_table in TABLES_TO_SYNC:
    delta_table = map_sqlite_to_delta_table_name(sqlite_table)
    if delta_table in table_names:
        print(f"   ✅ {delta_table}")
    else:
        print(f"   ❌ {delta_table} NOT FOUND")
 
# Check 2: Verify row counts match
print("\n📊 Check 2: Row Count Verification")
conn = sqlite3.connect(SQLITE_DB_FILE)
cursor = conn.cursor()
 
for sqlite_table in TABLES_TO_SYNC:
    delta_table = map_sqlite_to_delta_table_name(sqlite_table)
    full_table_name = f"{CATALOG}.{SCHEMA}.{delta_table}"
   
    # SQLite count
    cursor.execute(f"SELECT COUNT(*) FROM {sqlite_table}")
    sqlite_count = cursor.fetchone()[0]
   
    # Delta count
    try:
        delta_count = spark.table(full_table_name).count()
        match = "✅" if sqlite_count == delta_count else "❌"
        print(f"   {match} {delta_table:<30} SQLite: {sqlite_count:>5}  Delta: {delta_count:>5}")
    except:
        print(f"   ❌ {delta_table:<30} Table not found in Delta")
 
cursor.close()
conn.close()
 
print("\n✅ STEP 7 COMPLETED: Data quality checks finished")
 

# COMMAND ----------

# ============================================================================
# STEP 8: ARCHIVE PROCESSED FILE
# ============================================================================
 
from datetime import datetime
 
print("\n" + "="*80)
print("STEP 8: Archiving Processed File")
print("="*80)
 
# Define paths
ARCHIVE_DIR = "/Volumes/srms_catalog/srms/new_volume/archive"
FAILED_DIR = "/Volumes/srms_catalog/srms/new_volume/failed"
 
# Generate timestamp for archive filename
timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
# Use original filename with timestamp
base_name = PROCESSED_FILENAME.replace('.sql', '')
archive_filename = f"{base_name}_{timestamp}.sql"
 
# Check if sync was successful (no failures)
if failed_count == 0:
    # SUCCESS: Move to archive
    archive_path = f"{ARCHIVE_DIR}/{archive_filename}"
   
    # Copy to archive (use dbfs:// path for dbutils)
    dbutils.fs.cp(SQL_DUMP_FILE_DBFS, archive_path)
    print(f"\n✅ File archived successfully!")
    print(f"   Source: {SQL_DUMP_FILE}")
    print(f"   Archive: {archive_path}")
   
    # Delete original file to keep main folder clean
    dbutils.fs.rm(SQL_DUMP_FILE_DBFS)
    print(f"   🗑️  Removed original file from main folder")
    print(f"\n💡 Next upload will trigger new sync automatically")
   
else:
    # FAILURE: Move to failed folder
    failed_path = f"{FAILED_DIR}/{archive_filename}"
   
    dbutils.fs.cp(SQL_DUMP_FILE_DBFS, failed_path)
    print(f"\n⚠️  Sync had errors - moved to failed folder")
    print(f"   Failed file: {failed_path}")
    print(f"   Errors: {failed_count} table(s) failed to sync")
    print(f"\n📋 Action: Review errors above and fix issues")
   
    # Keep original file for retry
    print(f"   ℹ️  Original file kept for manual retry")
 
print("\n✅ STEP 8 COMPLETED")
 
# Show recent archives
print("\n📦 Recent Archive Files:")
try:
    archive_files = dbutils.fs.ls(ARCHIVE_DIR)
    sorted_files = sorted(archive_files, key=lambda x: x.name, reverse=True)[:5]
    for file in sorted_files:
        size_kb = file.size / 1024
        print(f"   {file.name:<50} {size_kb:>8.1f} KB")
   
    if len(archive_files) > 5:
        print(f"   ... and {len(archive_files) - 5} more archived files")
   
    # Storage info
    total_size = sum(f.size for f in archive_files) / 1024 / 1024
    print(f"\n📊 Archive Storage: {len(archive_files)} files, {total_size:.2f} MB total")
except:
    print("   (No archived files yet)")
 

# COMMAND ----------

# ============================================================================
# COMPLETION SUMMARY
# ============================================================================
 
print("\n" + "="*80)
print("SYNC JOB COMPLETED SUCCESSFULLY")
print("="*80)
 
print("\n✅ All steps completed:")
print("   0. ✅ Auto-detected SQL dump file")
print("   1. ✅ Created SQLite database from SQL dump")
print("   2. ✅ Verified database structure")
print("   3. ✅ Synced tables to Delta")
print("   4. ✅ Performed data quality checks")
print("   5. ✅ Archived processed file")
 
print("\n📅 What Happened:")
if failed_count == 0:
    print("   ✅ File synced successfully to Delta tables")
    print("   ✅ File archived with timestamp")
    print("   ✅ Main folder cleaned")
    print("\n   ➡️  Ready for next upload!")
    print("\n💡 Next Step: Run itsrms-stats-generator manually to update dashboard")
else:
    print("   ❌ Some tables failed to sync")
    print("   ⚠️  File moved to failed folder")
    print("\n   🛠️  Review errors and retry")
 
print("\n💡 EVENT-DRIVEN SETUP:")
print("   Option 1: Schedule this notebook to run every 1-5 minutes")
print("            (It will auto-detect and process new files)")
print("   Option 2: Create workflow with file arrival trigger")
print("            (Triggers immediately when file is uploaded)")
print("\n   Either way: Just upload file → Everything happens automatically!")
 
print("\n" + "="*80)
 

# COMMAND ----------

