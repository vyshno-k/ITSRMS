createtab_stmt

"CREATE TABLE srms_catalog.srms.categories (

  Id BIGINT NOT NULL,

  Name STRING COLLATE UTF8_BINARY NOT NULL,

  IsActive INT NOT NULL)

USING delta

DEFAULT COLLATION UTF8_BINARY

TBLPROPERTIES (

  'delta.checkpoint.writeStatsAsJson' = 'false',

  'delta.checkpoint.writeStatsAsStruct' = 'true',

  'delta.enableDeletionVectors' = 'true',

  'delta.enableRowTracking' = 'true',

  'delta.feature.appendOnly' = 'supported',

  'delta.feature.deletionVectors' = 'supported',

  'delta.feature.domainMetadata' = 'supported',

  'delta.feature.invariants' = 'supported',

  'delta.feature.rowTracking' = 'supported',

  'delta.minReaderVersion' = '3',

  'delta.minWriterVersion' = '7',

  'delta.parquet.compression.codec' = 'zstd',

  'delta.parquet.format.version' = '2.12.0',

  'delta.parquet.format.version.afe.internal' = '2.12.0')

"

CREATE TABLE srms_catalog.srms.dashboard_stats (

  stat_id INT,

  stat_name STRING COLLATE UTF8_BINARY,

  stat_value INT,

  stat_category STRING COLLATE UTF8_BINARY,

  run_timestamp TIMESTAMP,

  created_at TIMESTAMP,

  _rescued_data STRING COLLATE UTF8_BINARY)

USING delta

DEFAULT COLLATION UTF8_BINARY

TBLPROPERTIES (

  'delta.checkpoint.writeStatsAsJson' = 'false',

  'delta.checkpoint.writeStatsAsStruct' = 'true',

  'delta.columnMapping.mode' = 'name',

  'delta.enableDeletionVectors' = 'true',

  'delta.feature.appendOnly' = 'supported',

  'delta.feature.columnMapping' = 'supported',

  'delta.feature.deletionVectors' = 'supported',

  'delta.feature.invariants' = 'supported',

  'delta.minReaderVersion' = '3',

  'delta.minWriterVersion' = '7',

  'delta.parquet.compression.codec' = 'zstd',

  'delta.parquet.format.version' = '2.12.0',

  'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.departments ( Id BIGINT NOT NULL, Name STRING COLLATE UTF8_BINARY NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.employees ( Id BIGINT NOT NULL, FullName STRING COLLATE UTF8_BINARY NOT NULL, Email STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL, EmployeeCode STRING COLLATE UTF8_BINARY NOT NULL, FirstName STRING COLLATE UTF8_BINARY NOT NULL, LastName STRING COLLATE UTF8_BINARY NOT NULL, Phone STRING COLLATE UTF8_BINARY NOT NULL, Department STRING COLLATE UTF8_BINARY NOT NULL, Designation STRING COLLATE UTF8_BINARY NOT NULL, DateOfJoining STRING COLLATE UTF8_BINARY NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.priorities ( Id BIGINT NOT NULL, Name STRING COLLATE UTF8_BINARY NOT NULL, Level INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.resolutions ( Id BIGINT NOT NULL, ServiceRequestId BIGINT NOT NULL, InvestigationNotes STRING COLLATE UTF8_BINARY NOT NULL, ResolutionNotes STRING COLLATE UTF8_BINARY NOT NULL, ResolvedBy BIGINT, ResolvedAt STRING COLLATE UTF8_BINARY NOT NULL, ResolutionDueAt STRING COLLATE UTF8_BINARY, SlaResult STRING COLLATE UTF8_BINARY NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.servicecatalogs ( Id BIGINT NOT NULL, ServiceName STRING COLLATE UTF8_BINARY NOT NULL, Description STRING COLLATE UTF8_BINARY NOT NULL, Category STRING COLLATE UTF8_BINARY NOT NULL, RequestType STRING COLLATE UTF8_BINARY NOT NULL, ServiceOwner STRING COLLATE UTF8_BINARY, EstimatedDeliveryTime STRING COLLATE UTF8_BINARY, DefaultPriority STRING COLLATE UTF8_BINARY NOT NULL, SLA STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.servicerequesthistories ( Id BIGINT NOT NULL, ServiceRequestId BIGINT NOT NULL, Action STRING COLLATE UTF8_BINARY NOT NULL, OldValue STRING COLLATE UTF8_BINARY, NewValue STRING COLLATE UTF8_BINARY, PerformedBy BIGINT, CreatedAt STRING COLLATE UTF8_BINARY NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.servicerequests ( Id BIGINT NOT NULL, TicketNumber STRING COLLATE UTF8_BINARY NOT NULL, EmployeeId BIGINT NOT NULL, CategoryId BIGINT NOT NULL, ServiceTypeId BIGINT NOT NULL, PriorityId BIGINT NOT NULL, Subject STRING COLLATE UTF8_BINARY NOT NULL, Description STRING COLLATE UTF8_BINARY NOT NULL, Status STRING COLLATE UTF8_BINARY NOT NULL, CreatedAt STRING COLLATE UTF8_BINARY NOT NULL, UpdatedAt STRING COLLATE UTF8_BINARY, ResponseDueAt STRING COLLATE UTF8_BINARY, ResolutionDueAt STRING COLLATE UTF8_BINARY, ResolvedAt STRING COLLATE UTF8_BINARY, ClosedAt STRING COLLATE UTF8_BINARY) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.servicetypes ( Id BIGINT NOT NULL, Name STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.slaconfigurations ( Id BIGINT NOT NULL, PriorityId BIGINT NOT NULL, ResponseTargetMinutes INT NOT NULL, ResolutionTargetMinutes INT NOT NULL, ResolutionTargetBusinessDays INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.slapausestatuses ( Id BIGINT NOT NULL, Status STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.supportengineers ( Id BIGINT NOT NULL, FullName STRING COLLATE UTF8_BINARY NOT NULL, Email STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.ticketassignments ( Id BIGINT NOT NULL, ServiceRequestId BIGINT NOT NULL, EngineerId BIGINT NOT NULL, AssignedAt STRING COLLATE UTF8_BINARY NOT NULL, UnassignedAt STRING COLLATE UTF8_BINARY, IsCurrent INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.ticketcomments ( Id BIGINT NOT NULL, ServiceRequestId BIGINT NOT NULL, EmployeeId BIGINT, CommentText STRING COLLATE UTF8_BINARY NOT NULL, CreatedAt STRING COLLATE UTF8_BINARY NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 
 
CREATE TABLE srms_catalog.srms.users ( Id BIGINT NOT NULL, Username STRING COLLATE UTF8_BINARY NOT NULL, FullName STRING COLLATE UTF8_BINARY NOT NULL, Email STRING COLLATE UTF8_BINARY NOT NULL, PasswordHash STRING COLLATE UTF8_BINARY NOT NULL, Department STRING COLLATE UTF8_BINARY NOT NULL, Role STRING COLLATE UTF8_BINARY NOT NULL, IsActive INT NOT NULL) USING delta DEFAULT COLLATION UTF8_BINARY TBLPROPERTIES ( 'delta.checkpoint.writeStatsAsJson' = 'false', 'delta.checkpoint.writeStatsAsStruct' = 'true', 'delta.enableDeletionVectors' = 'true', 'delta.enableRowTracking' = 'true', 'delta.feature.appendOnly' = 'supported', 'delta.feature.deletionVectors' = 'supported', 'delta.feature.domainMetadata' = 'supported', 'delta.feature.invariants' = 'supported', 'delta.feature.rowTracking' = 'supported', 'delta.minReaderVersion' = '3', 'delta.minWriterVersion' = '7', 'delta.parquet.compression.codec' = 'zstd', 'delta.parquet.format.version' = '2.12.0', 'delta.parquet.format.version.afe.internal' = '2.12.0')
 