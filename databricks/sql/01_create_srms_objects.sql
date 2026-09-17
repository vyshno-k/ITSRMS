-- Run once in Databricks SQL Editor.
CREATE CATALOG IF NOT EXISTS srms_catalog;
CREATE SCHEMA IF NOT EXISTS srms_catalog.srms;
CREATE VOLUME IF NOT EXISTS srms_catalog.srms.integration;
-- Delta tables are created/refreshed by the PySpark job.
