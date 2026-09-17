# Databricks notebook source
from pyspark.sql import functions as F
import json

def widget(name, default=""):
    dbutils.widgets.text(name, default)
    return dbutils.widgets.get(name)

employees_path=widget("employees_path"); departments_path=widget("departments_path"); service_categories_path=widget("service_categories_path"); service_types_path=widget("service_types_path"); priorities_path=widget("priorities_path"); sla_configurations_path=widget("sla_configurations_path"); request_path=widget("request_path"); result_path=widget("result_path")
required={"employees_path":employees_path,"departments_path":departments_path,"service_categories_path":service_categories_path,"service_types_path":service_types_path,"priorities_path":priorities_path,"sla_configurations_path":sla_configurations_path,"request_path":request_path,"result_path":result_path}
missing=[k for k,v in required.items() if not v]
if missing: raise ValueError("Missing job parameters: "+", ".join(missing))
CATALOG="srms_catalog"; SCHEMA="srms"

def read_json(path): return spark.read.option("multiLine","true").json(path)

def refresh_delta(path, table_name):
    df=read_json(path)
    df.write.format("delta").mode("overwrite").option("overwriteSchema","true").saveAsTable(f"{CATALOG}.{SCHEMA}.{table_name}")
    return spark.table(f"{CATALOG}.{SCHEMA}.{table_name}")

employees=refresh_delta(employees_path,"employees")
departments=refresh_delta(departments_path,"departments")
service_categories=refresh_delta(service_categories_path,"service_categories")
service_types=refresh_delta(service_types_path,"service_types")
priorities=refresh_delta(priorities_path,"priorities")
sla_configurations=refresh_delta(sla_configurations_path,"sla_configurations")

request_rows=read_json(request_path).limit(1).collect()
request=request_rows[0].asDict(recursive=True) if request_rows else {}
for key in ["employeeId","categoryId","serviceTypeId"]:
    if request.get(key) in (None,"","null"): request[key]=None
    else: request[key]=int(request[key])
search=(request.get("search") or "").strip().lower()

matched_employees=employees.filter(F.col("isActive")==True)
if request.get("employeeId") is not None: matched_employees=matched_employees.filter(F.col("id")==request["employeeId"])
if search: matched_employees=matched_employees.filter(F.lower(F.coalesce(F.col("fullName"),F.lit(""))).contains(search)|F.lower(F.coalesce(F.col("email"),F.lit(""))).contains(search)|F.lower(F.coalesce(F.col("employeeCode"),F.lit(""))).contains(search))
matched_categories=service_categories.filter(F.col("isActive")==True)
if request.get("categoryId") is not None: matched_categories=matched_categories.filter(F.col("id")==request["categoryId"])
if search: matched_categories=matched_categories.filter(F.lower(F.coalesce(F.col("name"),F.lit(""))).contains(search))
matched_types=service_types.filter(F.col("isActive")==True)
if request.get("serviceTypeId") is not None: matched_types=matched_types.filter(F.col("id")==request["serviceTypeId"])
if search: matched_types=matched_types.filter(F.lower(F.coalesce(F.col("name"),F.lit(""))).contains(search))

def row_dict(row): return row.asDict(recursive=True)
result={
 "processedBy":"Databricks PySpark",
 "request":request,
 "deltaTables":{"employees":f"{CATALOG}.{SCHEMA}.employees","departments":f"{CATALOG}.{SCHEMA}.departments","serviceCategories":f"{CATALOG}.{SCHEMA}.service_categories","serviceTypes":f"{CATALOG}.{SCHEMA}.service_types","priorities":f"{CATALOG}.{SCHEMA}.priorities","slaConfigurations":f"{CATALOG}.{SCHEMA}.sla_configurations"},
 "counts":{"employees":matched_employees.count(),"departments":departments.count(),"serviceCategories":matched_categories.count(),"serviceTypes":matched_types.count(),"priorities":priorities.count(),"slaConfigurations":sla_configurations.count()},
 "employees":[row_dict(x) for x in matched_employees.orderBy("fullName").limit(100).collect()],
 "departments":[row_dict(x) for x in departments.orderBy("name").limit(100).collect()],
 "serviceCategories":[row_dict(x) for x in matched_categories.orderBy("id").limit(100).collect()],
 "serviceTypes":[row_dict(x) for x in matched_types.orderBy("id").limit(100).collect()],
 "priorities":[row_dict(x) for x in priorities.orderBy("level").limit(20).collect()],
 "slaConfigurations":[row_dict(x) for x in sla_configurations.orderBy("priorityId").limit(20).collect()]
}
result_json=json.dumps(result,default=str)
dbutils.fs.put(result_path,result_json,overwrite=True)
dbutils.notebook.exit(result_json)
